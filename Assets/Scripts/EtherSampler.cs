/*
MIT License

Copyright (c) 2022 Joseph Cramsey

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EtherSampler : MonoBehaviour {

    private static float MAX_TIDE_FORCE = 10f;

    public Transform playerTransform;
	public Transform tidePrefab;
    public Camera etherCam;
    [Space]
    public float sampleWidth = 10;
    public int startingTideCount = 100;
    public float crowdDistance = 5f;
    public float maxPeerDistance = 4f;
    public int highHitsPerPass = 64;
    public int midHitsPerPass = 32;
    public int lowHitsPerPass = 16;
    [Space]
    public Color playerHue;
    public Color targetHue;
    public Color predatorHue;
    public Color freighterHue;
    public float ascendedScale = 10f;
    public float cameraScale {
        get {
            return etherCam.orthographicSize * 2f;
        }
    }
    public float altitudeScale {
        get {
            return 1f + (Mathf.Clamp01(playerShip.currentAltitude - 1f) * (ascendedScale - 1f));
        }
    }
    [Space]
    public AmbientSounds ambientSounds;
    
    private Transform[] trArray;
    private Rigidbody2D[] riArray;
    private VisualTide[] cmArray;
    // For sorting
    private SonarSortable[,] srArray;
    private Queue<SonarPing> pingRequests;
    private bool pingRequiresSort = true;
    private float lastSparkleVolume = 0;
    //public float altitude { private set; get; }
    public Vector2 greaterTideDirection { private set; get; }
    private Vector2 greaterTideSeed;
    public ShipInEther playerShip { private set; get; }
    public bool[] channelsAscended { private set; get; }

    void Awake() {
        channelsAscended = new bool[4];
        playerShip = playerTransform.GetComponent<ShipInEther>();
    }

    // Start is called before the first frame update
    void Start() {
        greaterTideSeed = Random.insideUnitCircle * 100;
        //altitude = 1;
        trArray = new Transform[startingTideCount];
        riArray = new Rigidbody2D[startingTideCount];
        cmArray = new VisualTide[startingTideCount];
        srArray = new SonarSortable[(int)PingDirection.Count,startingTideCount];
        pingRequests = new Queue<SonarPing>();
        float sampleRadius = sampleWidth / 2f;
        float minRadius = 0.5f;
        for (int i = 0; i < startingTideCount; i++) {
            bool validSpot = false;
            float x = 0;
            float y = 0;
            do {
                validSpot = true;
                x = Random.Range(-sampleRadius, sampleRadius);
                y = Random.Range(-sampleRadius, sampleRadius);
                if (x > -minRadius && x < minRadius && y > -minRadius && y < minRadius) {
                    validSpot = false;
                }
            } while (!validSpot);
            Transform clone = Instantiate(tidePrefab, new Vector3(x, y, 0), Quaternion.identity);
            trArray[i] = clone;
            riArray[i] = clone.GetComponent<Rigidbody2D>();
            cmArray[i] = clone.GetComponent<VisualTide>();
            for (int d = 0; d < (int)PingDirection.Count; d++) {
                srArray[d,i] = new SonarSortable(i, 0, 0);
            }
        }
    }

    // Update is called once per frame
    void Update() {
        pingRequiresSort = true;
        while (pingRequests.Count > 0) {
            PingForAngle(pingRequests.Dequeue());
        }
        float sparkleVolume = 0;
        for (int i = 0; i < startingTideCount; i++) {
            float tideVolume = cmArray[i].sparkleVolume;
            float playerDiffX = playerTransform.position.x - trArray[i].position.x;
            float playerDiffY = playerTransform.position.y - trArray[i].position.y;
            float playerDist = (playerDiffX * playerDiffX) + (playerDiffY * playerDiffY);
            tideVolume = Mathf.Clamp01((tideVolume * 4f) / playerDist);
            sparkleVolume = Mathf.Max(sparkleVolume, tideVolume);
        }
        ambientSounds.altitudeMix = Mathf.Clamp(playerShip.currentAltitude, 0, 2);
        lastSparkleVolume = Mathf.Lerp(lastSparkleVolume, sparkleVolume, 8 * Time.deltaTime);
        ambientSounds.sparkleVolume = lastSparkleVolume * 0.25f;

        // TODO: channelAscended has true bits according to what teams are in ascended layers
        channelsAscended[(int)PingChannelID.Player_WasSeen / 2] = playerShip.isAscended;
    }

    void FixedUpdate() {
        float greaterTideAngle = Mathf.PerlinNoise(greaterTideSeed.x + (Time.fixedTime / 32f), greaterTideSeed.y) * Mathf.PI * 2;
        float greaterTideHeave = Mathf.PerlinNoise(greaterTideSeed.y + Time.fixedTime, greaterTideSeed.x) * 200f;
        float greaterTideX = Mathf.Cos(greaterTideAngle);
        float greaterTideY = Mathf.Sin(greaterTideAngle);
        greaterTideDirection = new Vector2(greaterTideX, greaterTideY) * greaterTideHeave;

        // Do boids
        // We're avoiding operations that affect garbage collection as much as possible
        if (playerShip.currentAltitude >= 1.9f) {
            DoAscendedBoids();
        }
        else if (playerShip.isBuried) {
            DoBuriedBoids();
        }
        else {
            DoStandardBoids(greaterTideX, greaterTideY, greaterTideHeave);
        }
    }

    private void DoAscendedBoids() { // Tides follow player with minimal calculation
        float wrapRadius = (sampleWidth / 2f) * 1.5f;
        float sqrCrowdDist = wrapRadius * wrapRadius;

        for (int i = 0; i < startingTideCount; i++) {
            Vector2 posA = trArray[i].position;
            float playerDiffX = playerTransform.position.x - posA.x;
            float playerDiffY = playerTransform.position.y - posA.y;
            float playerDist = (playerDiffX * playerDiffX) + (playerDiffY * playerDiffY);
            // Check to avoid sqrt
            if (playerDist >= sqrCrowdDist) {
                float cachedPlayerDist = Mathf.Sqrt(playerDist);
                // Wrap positions
                if (cachedPlayerDist > wrapRadius && cmArray[i].canTeleport) {
                    trArray[i].position = new Vector3(
                        playerTransform.position.x + playerDiffX,
                        playerTransform.position.y + playerDiffY,
                        0);
                    cmArray[i].FinishTeleport();
                }
            }
        }
    }

    private void DoStandardBoids(float greaterTideX, float greaterTideY, float greaterTideHeave) { // Tides scatter around player
        float sqrCrowdDist = crowdDistance * crowdDistance;
        float sqrMaxPeerDist = maxPeerDistance * maxPeerDistance;

        for (int i = 0; i < startingTideCount; i++) {
            Vector2 nextForce = Random.insideUnitCircle * 100f;
            Vector2 posA = trArray[i].position;
            float peerPressureX = 0;
            float peerPressureY = 0;
            for (int j = 0; j < startingTideCount; j++) {
                if (i == j) continue;
                Vector2 posB = trArray[j].position;

                float diffX = posB.x - posA.x;
                float diffY = posB.y - posA.y;
                float dist = (diffX * diffX) + (diffY * diffY);
                // Do a check before performing a sqrt operation
                if (dist > sqrMaxPeerDist) continue;
                float cachedDist = Mathf.Sqrt(dist);
                float cachedForce = 50f / (dist * dist);
                float repelX = (-diffX / cachedDist) * cachedForce;
                float repelY = (-diffY / cachedDist) * cachedForce;
                peerPressureX = Mathf.Clamp(peerPressureX + (repelX / startingTideCount), -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
                peerPressureY = Mathf.Clamp(peerPressureY + (repelY / startingTideCount), -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
            }

            float playerPressureX = 0;
            float playerPressureY = 0;
            float playerDiffX = playerTransform.position.x - posA.x;
            float playerDiffY = playerTransform.position.y - posA.y;
            float playerDist = (playerDiffX * playerDiffX) + (playerDiffY * playerDiffY);
            // Pull tides in when they're too far away
            if (playerDist > sqrCrowdDist) {
                float cachedPlayerDist = Mathf.Sqrt(playerDist);
                float playerForce = playerDist * 0.25f;
                playerPressureX = Mathf.Clamp((playerDiffX / cachedPlayerDist) * playerForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
                playerPressureY = Mathf.Clamp((playerDiffY / cachedPlayerDist) * playerForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
                // Wrap positions
                if (cachedPlayerDist > (sampleWidth / 2f) * 1.5f && cmArray[i].canTeleport) {
                    trArray[i].position = new Vector3(
                        playerTransform.position.x + playerDiffX,
                        playerTransform.position.y + playerDiffY,
                        0);
                    cmArray[i].FinishTeleport();
                }
            }
            float postMult = riArray[i].mass * 7f * Time.fixedDeltaTime;
            
            riArray[i].AddForce(new Vector2(
                ((nextForce.x + peerPressureX + playerPressureX) * postMult)
                + (greaterTideX * greaterTideHeave),
                ((nextForce.y + peerPressureY + playerPressureY) * postMult)
                + (greaterTideY * greaterTideHeave)
            ));
        }
    }

    private void DoBuriedBoids() { // Tides crowd around player
        float sqrMaxPeerDist = maxPeerDistance * maxPeerDistance;

        for (int i = 0; i < startingTideCount; i++) {
            Vector2 nextForce = Random.insideUnitCircle * 100f;
            Vector2 posA = trArray[i].position;
            float peerPressureX = 0;
            float peerPressureY = 0;
            for (int j = 0; j < startingTideCount; j++) {
                if (i == j) continue;
                Vector2 posB = trArray[j].position;

                float diffX = posB.x - posA.x;
                float diffY = posB.y - posA.y;
                float dist = (diffX * diffX) + (diffY * diffY);
                // Do a check before performing a sqrt operation
                if (dist > sqrMaxPeerDist) continue;
                float cachedDist = Mathf.Sqrt(dist);
                float cachedForce = 50f / (dist * dist);
                float repelX = (-diffX / cachedDist) * cachedForce;
                float repelY = (-diffY / cachedDist) * cachedForce;
                peerPressureX = Mathf.Clamp(peerPressureX + (repelX / startingTideCount), -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
                peerPressureY = Mathf.Clamp(peerPressureY + (repelY / startingTideCount), -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
            }

            float playerPressureX = 0;
            float playerPressureY = 0;
            float playerDiffX = playerTransform.position.x - posA.x;
            float playerDiffY = playerTransform.position.y - posA.y;
            //float playerDist = (playerDiffX * playerDiffX) + (playerDiffY * playerDiffY);
            // Tides crowd the player when buried
            //float cachedPlayerDist = Mathf.Sqrt(playerDist);
            //float playerForce = playerDist * 0.25f;
            float crowdForce = 1.5f;
            playerPressureX = Mathf.Clamp(playerDiffX * crowdForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
            playerPressureY = Mathf.Clamp(playerDiffY * crowdForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
            // The player does not move, so wrapping is not necessary
            /*if (cachedPlayerDist > (sampleWidth / 2f) * 1.5f && cmArray[i].canTeleport) {
                trArray[i].position = new Vector3(
                    playerTransform.position.x + playerDiffX,
                    playerTransform.position.y + playerDiffY,
                    0);
                cmArray[i].FinishTeleport();
            }*/
            float postMult = riArray[i].mass * 7f * Time.fixedDeltaTime;
            
            riArray[i].AddForce(new Vector2(
                ((nextForce.x + peerPressureX + playerPressureX) * postMult),
                ((nextForce.y + peerPressureY + playerPressureY) * postMult)
            ));
        }
    }

    public static float GetHueFromColor(Color sampledColor) {
        float hue = 0;
        float trash0 = 0;
        float trash1 = 0;
        Color.RGBToHSV(sampledColor, out hue, out trash0, out trash1);
        return hue;
    }

    public void RequestPing(PingChannelID id) {
        //TODO: Calculate ping responses from environment
        pingRequests.Enqueue(new SonarPing(PingDirection.East, PingStrength.Good, id));
    }

    private void PreparePings() {
        // Cache direction and distance sorts for multiple possible pings
        // Also, avoid garbage collection while doing so
        float playerX = playerTransform.position.x;
        float playerY = playerTransform.position.y;
        for (int i = 0; i < startingTideCount; i++) {
            float tidePosX = trArray[i].position.x;
            float tidePosY = trArray[i].position.y;
            float tideDirectionX = tidePosX - playerX;
            float tideDirectionY = tidePosY - playerY;
            float cachedDist = Mathf.Sqrt(
                (tideDirectionX * tideDirectionX) +
                (tideDirectionY * tideDirectionY)
            );
            for (int d = 0; d < (int)PingDirection.Count; d++) {
                SonarSortable sr = srArray[d,i];
                sr.index = i; // Align indices
                sr.dist = cachedDist;
                if (d == (int)PingDirection.Surrounding) {
                    sr.dotPr = 1f;
                }
                else {
                    Vector2 vecDir;
                    Vector2 tideDir = new Vector2(tideDirectionX / cachedDist, tideDirectionY / cachedDist);
                    switch ((PingDirection)d) {
                        default:
                        case PingDirection.North:
                            vecDir = Vector2.up;
                            break;
                        case PingDirection.East:
                            vecDir = Vector2.right;
                            break;
                        case PingDirection.South:
                            vecDir = Vector2.down;
                            break;
                        case PingDirection.West:
                            vecDir = Vector2.left;
                            break;
                    }
                    sr.dotPr = Vector2.Dot(vecDir, tideDir);
                }
                sr.CacheScore();
            }
        }

        // We're doing insertion sort here, as it is simple to do, our array is small, and
        // we want a minimal memory impact.
        for (int d = 0; d < (int)PingDirection.Count; d++) {
            for (int i = 1; i < startingTideCount; i++) {
                SonarSortable x = srArray[d,i];
                int j = i - 1;
                while (j >= 0 && srArray[d,j].lastScore > x.lastScore) {
                    srArray[d,j + 1] = srArray[d,j];
                    j--;
                }
                srArray[d,j + 1] = x;
            }
        }

        pingRequiresSort = false;
    }

    private void PingForAngle(SonarPing pingRequest) {
        if ((int)pingRequest.strength <= 0) return;
        
        if (pingRequiresSort) {
            PreparePings();
        }

        int d = (int)pingRequest.direction;

        // Dish out pings, with more hits closer to the front of the array
        int maxHitsPerPass;
        switch (pingRequest.strength)
        {
            default:
                maxHitsPerPass = highHitsPerPass;
                break;
            case PingStrength.Good:
                maxHitsPerPass = midHitsPerPass;
                break;
            case PingStrength.Weak:
                maxHitsPerPass = lowHitsPerPass;
                break;
        }
        for (int m = startingTideCount; m >= maxHitsPerPass; m /= 2) {
            for (int i = 0; i < maxHitsPerPass; i++) {
                int pick = Random.Range(0, m);
                if (pick < maxHitsPerPass) continue; // We will be hitting these later
                int index = srArray[d,pick].index;
                cmArray[index].Ping((srArray[d,i].distanceOffset * 2) + 1, pingRequest.id);
            }
        }
        for (int i = 0; i < maxHitsPerPass; i++) {
            int index = srArray[d,i].index;
            cmArray[index].Ping(srArray[d,i].distanceOffset, pingRequest.id);
        }
    }
}

public class SonarPing {

    public PingDirection direction { private set; get; }
    public PingStrength strength { private set; get; }
    public PingChannelID id { private set; get; }

    public SonarPing(PingDirection direction, PingStrength strength, PingChannelID id) {
        this.direction = direction;
        this.strength = strength;
        this.id = id;
    }
}

public class SonarSortable {
    public int index;
    public float dotPr;
    public float dist;
    public float lastScore { private set; get; }
    public float distanceOffset {
        get {
            return lastScore / 4f;
        }
    }

    public SonarSortable(int index, float dotPr, float dist) {
        this.index = index;
        this.dotPr = dotPr;
        this.dist = dist;
        this.lastScore = 0;
    }

    public void CacheScore() {
        float dotScore = (2.5f - (dotPr + 1f)) * 2;
        lastScore = dotScore * dist;
    }
}

public enum PingStrength {
    Strong = 3,
    Good = 2,
    Weak = 1,
    Silent = 0
}

public enum PingDirection {
    Surrounding = 0,
    North = 1,
    East = 2,
    South = 3,
    West = 4,
    Count = 5
}
