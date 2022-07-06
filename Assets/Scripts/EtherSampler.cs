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

    public static float GREATER_TIDE_STRENGTH = 200;

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
    [Space]
    public AmbientSounds ambientSounds;
    [Space]
    public EtherAltitude[] altitudes;
    private float lastSparkleVolume = 0;
    public Vector2 greaterTideDirection { private set; get; }
    private Vector2 greaterTideSeed;
    public ShipInEther playerShip { private set; get; }
    public EtherStore store { private set; get; }
    public List<EtherAgent> agents { private set; get; } = new List<EtherAgent>();

    // Sampling
    private EtherCell[,] sampleArea;
    private SignalTrace[,] traceArea;
    private float[,] mixFactors;

    // Cached boids
    private TideEngine tideEngine;
    private Transform[] trArray;
    private Rigidbody2D[] riArray;
    private VisualTide[] cmArray;
    // For sorting
    private SonarSortable[,] srArray;
    private bool pingRequiresSort = true;
    private float agentTimer = 0;
    private int agentIndex = 0;

    void Awake() { //TODO: Create a debug UI screen for reading samples and any cell around the map
        store = new EtherStore(36);
        playerShip = playerTransform.GetComponent<ShipInEther>();
        sampleArea = new EtherCell[3, 3];
        traceArea = new SignalTrace[3, 3];
        mixFactors = new float[3, 3];
    }

    // Start is called before the first frame update
    void Start() {
        greaterTideSeed = Random.insideUnitCircle * 100;
        trArray = new Transform[startingTideCount];
        riArray = new Rigidbody2D[startingTideCount];
        cmArray = new VisualTide[startingTideCount];
        srArray = new SonarSortable[(int)PingDirection.Count,startingTideCount];
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
        // Handle sound volumes
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

        // Update 20 FPS agent handler
        bool handlingAgents = agentIndex < agents.Count;
        if (!handlingAgents) {
            agentTimer += Time.deltaTime;
            if (agentTimer >= 1f / 20f) { // Try handling agents
                agentIndex = 0;
                agentTimer = 0;
                handlingAgents = agentIndex < agents.Count;
            }
        }

        // Update ascended cell
        for (int i = 0; i < agents.Count; i++) {
            EtherAgent agent = agents[i];
            store.ascendedCell.channelSignals[(int)agent.cruiseChannel].strength = agent.altitudeProfile.hasEasyListening ? 1f : 0f;
        }

        // Clk grid
        store.Clk(Time.deltaTime);

        if (handlingAgents) {
            EtherAgent agent = agents[agentIndex];

            // Handle agent pings
            pingRequiresSort = true;
            while (agent.pingRequests.Count > 0) {
                PingForAngle(agent, agent.pingRequests.Dequeue());
            }

            // Collect agent sensor sample
            if (agent.altitudeProfile.hasEasyListening) {
                store.ascendedCell.CopyTo(agent.sampleCell);
            }
            else if (agent.altitudeProfile.collectsSamples) {
                PrepareSample(agent.transform.position);
                agent.sampleCell.CollectFromSample(sampleArea, traceArea, mixFactors);
            }

            agentIndex++;
            //Debug.Log("" + Time.frameCount + ": Handled agent " + agent.transform.name);
        }
    }

    void FixedUpdate() {
        float greaterTideAngle = Mathf.PerlinNoise(greaterTideSeed.x + (Time.fixedTime / 32f), greaterTideSeed.y) * Mathf.PI * 2;
        float basicHeave = Mathf.PerlinNoise(greaterTideSeed.y + (Time.fixedTime / 16f), greaterTideSeed.x);
        float minHeave = 0.2f;
        float greaterTideHeave = ((basicHeave * (1f - minHeave)) + minHeave) * GREATER_TIDE_STRENGTH;
        float greaterTideX = Mathf.Cos(greaterTideAngle);
        float greaterTideY = Mathf.Sin(greaterTideAngle);
        greaterTideDirection = new Vector2(greaterTideX, greaterTideY) * greaterTideHeave;

        // Do boids
        // We're avoiding operations that affect garbage collection as much as possible
        TideMode tideMode = playerShip.altitudeProfile.tideMode;

        tideEngine.Setup(tideMode != TideMode.Standard);
        for (int i = 0; i < startingTideCount; i++) {
            tideEngine.StartIndex(i);
            switch (tideMode) {
                case TideMode.Crowding:
                case TideMode.Standard:
                    tideEngine.CacheNextNoiseForce();
                    tideEngine.CalculatePeerPressure();
                    break;
            }
            tideEngine.StartPlayerPressure();
            switch (tideMode) {
                case TideMode.Crowding:
                    tideEngine.CalculatePlayerCrowding();
                    break;
                case TideMode.Standard:
                    tideEngine.CalculatePlayerPressure();
                    break;
            }
            tideEngine.DoWrapping();
            switch (tideMode) {
                case TideMode.Standard:
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

    // Cache a 3x3 area of cells and their gradients, relative to a world position
    private void PrepareSample(Vector2 realPosition) {
        Vector2 gradientPos = GetGradientPosFromWorldPos(realPosition);
        Vector2Int cellPos = GetCellCoordFromGradientPos(gradientPos);
        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                int cellIndexX = GetSafeCellCoord(cellPos.x + x);
                int cellIndexY = GetSafeCellCoord(cellPos.y + y);
                EtherCell selectedCell = store.cells[cellIndexX, cellIndexY];
                sampleArea[x + 1, y + 1] = selectedCell;

                Vector2Int cellPosition = new Vector2Int(cellIndexX, cellIndexY);
                float gradientDelta = GetCoordGradientDelta(
                    gradientPos, cellPosition
                );
                mixFactors[x + 1, y + 1] = 1f - Mathf.Clamp01(gradientDelta);
            }
        }
    }

    private float GetCoordGradientDelta(float fromGradientCoord, int toCellCoord) {
        // Get shortest delta position to cell
        return Mathf.Min(
            Mathf.Abs((float)toCellCoord - fromGradientCoord),
            Mathf.Abs(((float)toCellCoord + (float)store.sideLength) - fromGradientCoord),
            Mathf.Abs(((float)toCellCoord - (float)store.sideLength) - fromGradientCoord)
        );
    }

    private float GetCoordGradientDelta(Vector2 fromGradientPosition, Vector2Int toCellPosition) {
        float dx = GetCoordGradientDelta(fromGradientPosition.x, toCellPosition.x);
        float dy = GetCoordGradientDelta(fromGradientPosition.y, toCellPosition.y);
        return (new Vector2(dx, dy)).magnitude;
    }

    private Vector2 GetGradientPosFromWorldPos(Vector2 realPosition) {
        float gradientX = (realPosition.x / 360f) * store.sideLength;
        float gradientY = (realPosition.y / 360f) * store.sideLength;
        return new Vector2(gradientX, gradientY);
    }

    private Vector2Int GetCellCoordFromGradientPos(Vector2 gradientPosition) {
        int x = GetSafeCellCoord(Mathf.RoundToInt(gradientPosition.x));
        int y = GetSafeCellCoord(Mathf.RoundToInt(gradientPosition.y));
        return new Vector2Int(x, y);
    }

    private Vector2Int GetCellCoordFromWorldPos(Vector2 realPosition) {
        return GetCellCoordFromGradientPos(GetGradientPosFromWorldPos(realPosition));
    }

    private int GetSafeCellCoord(int coord) {
        while (coord >= store.sideLength) coord -= store.sideLength;
        while (coord < 0) coord += store.sideLength;
        return coord;
    }

    public void RequestPing(EtherAgent agent, PingChannelID id) {
        //TODO: Calculate ping responses from footprint trace of sample area
        ApplyPing(agent, PingDirection.East, PingStrength.Strong, id);
    }

    private void ApplyPing(EtherAgent agent, PingDirection direction, PingStrength strength, PingChannelID id) {
        //TODO: Confirm other agents can ping?

        // Do not handle rendering of agents outside of view distance
        if (Vector2.Distance(agent.transform.position, playerTransform.position) <= 25f) {
            agent.pingRequests.Enqueue(new SonarPing(direction, strength, id));
        }
        PrepareSample(agent.transform.position);
        for (int y = 0; y < 3; y++) {
            for (int x = 0; x < 3; x++) {
                sampleArea[x, y].channelSignals[(int)id].ApplyPing(direction, strength, mixFactors[x, y]);
            }
        }
    }

    private void PreparePings(EtherAgent agent) {
        // Cache direction and distance sorts for multiple possible pings
        // Also, avoid garbage collection while doing so
        float agentX = agent.transform.position.x;
        float agentY = agent.transform.position.y;
        for (int i = 0; i < startingTideCount; i++) {
            float tidePosX = trArray[i].position.x;
            float tidePosY = trArray[i].position.y;
            float tideDirectionX = tidePosX - agentX;
            float tideDirectionY = tidePosY - agentY;
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

    private void PingForAngle(EtherAgent agent, SonarPing pingRequest) {
        if ((int)pingRequest.strength <= 0) return;
        
        if (pingRequiresSort) {
            PreparePings(agent);
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
