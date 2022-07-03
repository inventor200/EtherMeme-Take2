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
    
    private TideEngine tideEngine;
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
    //public bool[] channelsAscended { private set; get; }
    public SignalTrace[,] channelSignals { private set; get; }

    void Awake() {
        //channelsAscended = new bool[(int)PingChannelID.Count / 2];
        int channelCount = (int)PingChannelID.Count / 2;
        channelSignals = new SignalTrace[3, channelCount];
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < channelCount; j++) {
                channelSignals[i, j] = new SignalTrace();
            }
        }
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
            Transform clone = Instantiate(tidePrefab, new Vector3(x, y, 0) + playerTransform.position, Quaternion.identity);
            trArray[i] = clone;
            riArray[i] = clone.GetComponent<Rigidbody2D>();
            cmArray[i] = clone.GetComponent<VisualTide>();
            for (int d = 0; d < (int)PingDirection.Count; d++) {
                srArray[d,i] = new SonarSortable(i, 0, 0);
            }
        }
        tideEngine = new TideEngine(playerTransform.GetComponent<Rigidbody2D>(), this, trArray, riArray, cmArray);
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

        // TODO: Add signal to channelSignals according to altitude and pings

        int channelCount = (int)PingChannelID.Count / 2;
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < channelCount; j++) {
                channelSignals[i, j].Clk(Time.deltaTime);
            }
        }
    }

    void FixedUpdate() {
        float greaterTideAngle = Mathf.PerlinNoise(greaterTideSeed.x + (Time.fixedTime / 32f), greaterTideSeed.y) * Mathf.PI * 2;
        float greaterTideHeave = Mathf.PerlinNoise(greaterTideSeed.y + Time.fixedTime, greaterTideSeed.x) * 200f;
        float greaterTideX = Mathf.Cos(greaterTideAngle);
        float greaterTideY = Mathf.Sin(greaterTideAngle);
        greaterTideDirection = new Vector2(greaterTideX, greaterTideY) * greaterTideHeave;

        // Do boids
        // We're avoiding operations that affect garbage collection as much as possible
        int tideMode;
        if (playerShip.currentAltitude >= 1.9f) {
            tideMode = 2;
        }
        else if (playerShip.isBuried) {
            tideMode = 0;
        }
        else {
            tideMode = 1;
        }

        tideEngine.Setup(tideMode != 1);
        for (int i = 0; i < startingTideCount; i++) {
            tideEngine.StartIndex(i);
            switch (tideMode) {
                case 0:
                case 1:
                    tideEngine.CacheNextNoiseForce();
                    tideEngine.CalculatePeerPressure();
                    break;
            }
            tideEngine.StartPlayerPressure();
            switch (tideMode) {
                case 0:
                    tideEngine.CalculatePlayerCrowding();
                    break;
                case 1:
                    tideEngine.CalculatePlayerPressure();
                    break;
            }
            tideEngine.DoWrapping();
            switch (tideMode) {
                case 1:
                    tideEngine.ApplyForces(greaterTideX, greaterTideY, greaterTideHeave);
                    break;
                default:
                    tideEngine.ApplyForces(0, 0, 0);
                    break;
            }
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
        ApplyPing(PingDirection.East, PingStrength.Good, id);
    }

    private void ApplyPing(PingDirection direction, PingStrength strength, PingChannelID id) {
        pingRequests.Enqueue(new SonarPing(direction, strength, id));
        if (playerShip.isAtStandardDepth) {
            channelSignals[1, (int)id / 2].ApplyPing(direction, strength);
        }
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

public class SignalTrace {

    public float strength = 0;
    public bool hasBlink {
        get {
            bool res = _hasBlink;
            _hasBlink = false;
            return res;
        }
        set {
            _hasBlink = value;
        }
    }
    private bool _hasBlink = false;
    public PingDirection lastDirection = PingDirection.Surrounding;

    public void ApplyPing(PingDirection direction, PingStrength strength) {
        if (strength != PingStrength.Silent) {
            this.lastDirection = direction;
            _hasBlink = true;
            switch(strength) {
                case PingStrength.Weak:
                default:
                    this.strength = 0.5f;
                    break;
                case PingStrength.Good:
                    this.strength = 0.75f;
                    break;
                case PingStrength.Strong:
                    this.strength = 1f;
                    break;
            }
        }
    }

    public void Clk(float dt) {
        strength = Mathf.MoveTowards(strength, 0, dt / 10f);
        _hasBlink |= (Random.Range(0, 100) < 4);
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
