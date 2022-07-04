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
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using UnityEngine;

public class ShipInEther : MonoBehaviour {

    public static KeyCode KEY_NORTH = KeyCode.W;
    public static KeyCode KEY_WEST = KeyCode.A;
    public static KeyCode KEY_SOUTH = KeyCode.S;
    public static KeyCode KEY_EAST = KeyCode.D;
    public static KeyCode KEY_AHEAD_FULL_MOD = KeyCode.LeftShift;
    public static KeyCode KEY_STEALTH = KeyCode.H;
    public static KeyCode KEY_ASCEND = KeyCode.R;
    public static KeyCode KEY_DESCEND = KeyCode.F;

    private static float DEPTH_CHANGE_TIME = 10f;

    // Force, avg m/s, avg error per meter
    private static float[][] SPEED_MODES = new float [][] {
        new float[] {100f, 1.2f, 0.02f},
        new float[] {200f, 2.5f, 0.03f}
    };

    private static float ERROR_PER_SEC_IDLE_STANDARD = 0.036f;
    private static float ERROR_PER_SEC_STEALTH_STANDARD = 0.738f;
    private static float ERROR_PER_SEC_BURIED = 0.000042f;

	public SpriteRenderer halo;
    public Transform arrow;
    public PostProcessVolume postProcessor;
    private Vignette vignette;
    [Space]
    public AmbientSounds ambientSounds;
    public RectTransform rootGrid;
    public TopDownGrid standardGrid;
    public TopDownGrid ascendedGrid;
    public CoordClock xClock;
    public CoordClock yClock;
    public DepthDiagram depthDiagram;
    public TerminalScreen terminalScreen;
    public float currentAltitude { private set; get; }
    private int goalAltitudeIndex = 1;
    public EtherAltitude altitudeProfile { private set; get; }
    public EtherAltitude altitudeBucket { private set; get; }

    private EtherSampler etherSampler;
    private Rigidbody2D ri;
    private float pingHue = 0;
    private float pingMagnitude = 0;
    private float[] hues;

    public ShipSpeed currentSpeed { private set; get; } = ShipSpeed.Halted;
    private Vector2 moveDir = Vector2.zero;
    private float arrowSize = 1f;
    public Vector2 expectedPosition { private set; get; }
    private float expectedAcceleration = 0;
    private float expectedError = 0;
    private bool wasInteractingWithTides = false;
    /*private bool interactingWithTides = true;
    public bool isEntering {
        get {
            return currentAltitude >= ENTRY_ALTITUDE;
        }
    }
    public bool isAscended {
        get {
            return currentAltitude > 1f + STANDARD_DEPTH_TOLERANCE && currentAltitude < ENTRY_ALTITUDE;
        }
    }
    public bool hasSignal {
        get {
            return currentAltitude > 2f - STANDARD_DEPTH_TOLERANCE;
        }
    }
    public bool isBuried {
        get {
            return currentAltitude < 1f - STANDARD_DEPTH_TOLERANCE;
        }
    }
    public bool isAtStandardDepth {
        get {
            return (currentAltitude >= (1f - STANDARD_DEPTH_TOLERANCE))
                && (currentAltitude <= 1f + STANDARD_DEPTH_TOLERANCE);
        }
    }*/

    private bool hasLanded = false;
    private float decoderSampleCountdown = 1f;
    private float decoderSampleNextDelay = 1f;
    private bool tetherStabilityBroken = false;

    void Awake() {
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
        ri = GetComponent<Rigidbody2D>();
        hues = new float[] {
            EtherSampler.GetHueFromColor(etherSampler.playerHue),
            EtherSampler.GetHueFromColor(etherSampler.targetHue),
            EtherSampler.GetHueFromColor(etherSampler.predatorHue),
            EtherSampler.GetHueFromColor(etherSampler.freighterHue)
        };
        postProcessor.profile.TryGetSettings<Vignette>(out vignette);
        altitudeProfile = new EtherAltitude();
        //altitudeBucket = etherSampler.altitudes[etherSampler.altitudes.Length - 1];
        currentAltitude = etherSampler.altitudes[etherSampler.altitudes.Length - 1].mainAltitude;
        altitudeBucket = EtherAltitude.UpdateAltitudeProfileAndGetBucket(currentAltitude, altitudeProfile, etherSampler.altitudes);
    }

    // Start is called before the first frame update
    void Start() {
        expectedPosition = (Vector2)transform.position;
        terminalScreen.WriteLine("Mission start; diving to safe depth...");
    }

    // Update is called once per frame
    // TODO: Refactor this class into player ship, general ship/agent, and map grid
    void Update() {
        pingMagnitude = Mathf.Clamp01(pingMagnitude - (Time.deltaTime / 2f));
        Color haloColor = Color.HSVToRGB(pingHue, 1f, pingMagnitude);
        
        if (Input.GetKeyDown(KeyCode.Space) && hasLanded && altitudeProfile.allowControlInput && altitudeProfile.allowSonarPing) {
            SendPing(PingChannelID.Player_WasSeen);
        }

        ShipSpeed nextSpeed = ShipSpeed.Halted;
        Vector2 nextMoveDir = moveDir;

        if (currentSpeed == ShipSpeed.Stealth) {
            nextSpeed = ShipSpeed.Stealth;

            if (Input.GetKeyDown(KEY_STEALTH) && hasLanded) {
                nextSpeed = ShipSpeed.Halted;
            }

            float blendInColor = 0.15f;
            haloColor = new Color(blendInColor, blendInColor, blendInColor, 1f);
        }
        else if (hasLanded && altitudeProfile.allowControlInput) {
            if (Input.GetKey(KEY_NORTH)) {
                nextSpeed = ShipSpeed.Cruise;
                nextMoveDir += Vector2.up;
            }
            if (Input.GetKey(KEY_WEST)) {
                nextSpeed = ShipSpeed.Cruise;
                nextMoveDir += Vector2.left;
            }
            if (Input.GetKey(KEY_SOUTH)) {
                nextSpeed = ShipSpeed.Cruise;
                nextMoveDir += Vector2.down;
            }
            if (Input.GetKey(KEY_EAST)) {
                nextSpeed = ShipSpeed.Cruise;
                nextMoveDir += Vector2.right;
            }

            if (nextSpeed == ShipSpeed.Cruise && Input.GetKey(KEY_AHEAD_FULL_MOD)) {
                nextSpeed = ShipSpeed.AheadFull;
            }

            int altitudeChange = 0;

            if (Input.GetKeyDown(KEY_ASCEND) && goalAltitudeIndex < etherSampler.altitudes.Length - 1) {
                altitudeChange++;
                if (etherSampler.altitudes[goalAltitudeIndex + altitudeChange].forcePassThrough) {
                    altitudeChange++; // Pass through
                }
            }
            else if (Input.GetKeyDown(KEY_DESCEND) && goalAltitudeIndex > 0) {
                altitudeChange--;
                if (etherSampler.altitudes[goalAltitudeIndex + altitudeChange].forcePassThrough) {
                    altitudeChange--; // Pass through
                }
            }

            if (etherSampler.altitudes[goalAltitudeIndex + altitudeChange].hasExclusiveEntry) {
                altitudeChange = 0; // Cancel change
            }

            goalAltitudeIndex += altitudeChange;

            if (Input.GetKeyDown(KEY_STEALTH)) {
                nextSpeed = ShipSpeed.Stealth;
            }
        }

        if (!altitudeProfile.allowLateralMovement) {
            nextSpeed = ShipSpeed.Halted;
        }

        bool didHaveSignal = altitudeProfile.hasTetherSignal;
        float goalAltitude = etherSampler.altitudes[goalAltitudeIndex].mainAltitude;
        currentAltitude = Mathf.MoveTowards(currentAltitude, goalAltitude, Time.deltaTime / DEPTH_CHANGE_TIME);
        altitudeBucket = EtherAltitude.UpdateAltitudeProfileAndGetBucket(currentAltitude, altitudeProfile, etherSampler.altitudes);
        bool nowHasSignal = altitudeProfile.hasTetherSignal;
        if (didHaveSignal != nowHasSignal) {
            if (nowHasSignal) {
                terminalScreen.WriteGreenLine("Tether signal aquired; recalibrating...");
            }
            else if (hasLanded) {
                terminalScreen.WriteWarningLine("Tether signal lost");
            }
        }

        currentSpeed = nextSpeed;
        moveDir = nextMoveDir.normalized;

        ambientSounds.engineOn = currentSpeed != ShipSpeed.Stealth;

        float speedScale = 0;
        float expectedSpeed = 0;

        if ((int)currentSpeed > (int)ShipSpeed.Halted) {
            float nextAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            float currentAngle = arrow.localRotation.eulerAngles.z;

            arrow.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowardsAngle(currentAngle, nextAngle, 360f * Time.deltaTime));
            speedScale = ri.velocity.magnitude / (SPEED_MODES[1][1] * altitudeProfile.moveScale);

            expectedAcceleration = Mathf.MoveTowards(expectedAcceleration, 1f, Time.deltaTime);
            expectedSpeed = SPEED_MODES[(int)currentSpeed][1];

        }
        else {
            expectedAcceleration = Mathf.MoveTowards(expectedAcceleration, 0f, Time.deltaTime);
            expectedSpeed = SPEED_MODES[0][1]; // We're actually slowing down from this
        }

        expectedPosition += moveDir * expectedSpeed * expectedAcceleration * Time.deltaTime;
        if (altitudeProfile.hasTetherSignal) {
            expectedPosition = (Vector2)transform.position; // Recalibrate when ascended
            expectedError = 0;
        }

        // Ensure wrapping
        expectedPosition = new Vector2(
            Mathf.Repeat(expectedPosition.x, 360f),
            Mathf.Repeat(expectedPosition.y, 360f)
        );

        string stealthInd = currentSpeed == ShipSpeed.Stealth ? "\n[Stealth]" : "";

        // Adjust visual grid
        //float totalAltitudeScale = rootScaleCurve.Evaluate(Mathf.Clamp01(currentAltitude / 2f));
        float totalAltitudeScale = altitudeProfile.mapScale;
        rootGrid.localScale = new Vector3(totalAltitudeScale, totalAltitudeScale, totalAltitudeScale);
        standardGrid.AdjustTo(expectedPosition, currentAltitude, etherSampler.cameraScale);
        ascendedGrid.AdjustTo(expectedPosition, currentAltitude, etherSampler.cameraScale);

        // Adjust arrow
        float nextSize = currentSpeed == ShipSpeed.Stealth ? 0f : 1f;
        arrowSize = Mathf.Lerp(arrowSize, nextSize, 5 * Time.deltaTime);
        arrow.localScale = new Vector3(arrowSize, arrowSize, arrowSize);
        halo.transform.localScale = new Vector3(1 + arrowSize, 1 + arrowSize, 1 + arrowSize);
        halo.color = haloColor;

        // Sounds get higher with speed
        ambientSounds.velocityMix = speedScale;

        // Prepare to change collision layer, if necessary
        //interactingWithTides = altitudeProfile.allowTideInteraction;

        // Control vignette intensity with altitude
        //vignette.intensity.value = Mathf.Clamp01(vignetteStrengths.Evaluate(currentAltitude / 2f));
        vignette.intensity.value = altitudeProfile.vignetteFactor;

        xClock.value = expectedPosition.x;
        xClock.errorValue = expectedError;
        yClock.value = expectedPosition.y;
        yClock.errorValue = expectedError;

        if (!hasLanded) {
            if (currentAltitude <= 2.25f && !tetherStabilityBroken) {
                terminalScreen.WriteErrorLine("Tether stability system: 10s left");
                tetherStabilityBroken = true;
            }
            else if (altitudeProfile.allowSonarPing) {
                hasLanded = true;
                terminalScreen.WriteWarningLine("Tether signal lost");
            }
        }

        // Instability and Depth Diagram
        depthDiagram.yOffset = altitudeProfile.diagramYOffset;
        if (altitudeProfile.hasInstabilityHazard) {
            if (depthDiagram.instabilityLevel == 0) {
                terminalScreen.WriteErrorLine("Unstable tides; dive to a safer depth!");
                if (!hasLanded) {
                    terminalScreen.WriteWarningLine("  (Tether is providing stability for now)");
                }
            }
            depthDiagram.instabilityLevel = 2;
        }
        else if (altitudeProfile.hasBurialHazard) {
            if (depthDiagram.instabilityLevel == 0) {
                terminalScreen.WriteWarningLine("Swallowing hazard; do not dive deeper!");
            }
            depthDiagram.instabilityLevel = 1;
        }
        else {
            if (depthDiagram.instabilityLevel > 0) {
                terminalScreen.WriteGreenLine("Stable depth reached");
            }
            depthDiagram.instabilityLevel = 0;
        }
    }

    void FixedUpdate() {
        if (wasInteractingWithTides != altitudeProfile.allowTideInteraction) {
            wasInteractingWithTides = altitudeProfile.allowTideInteraction;
            gameObject.layer = LayerMask.NameToLayer(altitudeProfile.allowTideInteraction ? "Standard" : "NonStandard");
        }
        
        float nonStandardMass = 100000;
        float nonStandardDrag = 100f;
        if (currentSpeed == ShipSpeed.Stealth) {
            ri.mass = altitudeProfile.allowTideInteraction ? 100 : nonStandardMass;
            ri.drag = altitudeProfile.allowTideInteraction ? 0f : nonStandardDrag;
            if (altitudeProfile.tideMode == TideMode.Standard) {
                expectedError += ERROR_PER_SEC_STEALTH_STANDARD * Time.fixedDeltaTime;
            }
            else if (altitudeProfile.tideMode == TideMode.Crowding) {
                expectedError += ERROR_PER_SEC_BURIED * Time.fixedDeltaTime;
            }
        }
        else {
            ri.mass = altitudeProfile.allowTideInteraction ? 500 : nonStandardMass;
            ri.drag = altitudeProfile.allowTideInteraction ? 10f : nonStandardDrag;

            if (altitudeProfile.tideMode == TideMode.Standard) {
                expectedError += ERROR_PER_SEC_IDLE_STANDARD * Time.fixedDeltaTime;
            }
            else if (altitudeProfile.tideMode == TideMode.Crowding) {
                expectedError += ERROR_PER_SEC_BURIED * Time.fixedDeltaTime;
            }
            
            if (currentSpeed != ShipSpeed.Halted && altitudeProfile.allowLateralMovement) {
                float dragAdjustment = ri.drag * 0.75f;
                float speed = SPEED_MODES[(int)currentSpeed][0] * altitudeProfile.moveScale;
                expectedError += SPEED_MODES[(int)currentSpeed][1] * altitudeProfile.moveScale
                    * SPEED_MODES[(int)currentSpeed][2] * Time.fixedDeltaTime;
                ri.AddForce(moveDir * ri.mass * speed * dragAdjustment * Time.fixedDeltaTime);
            }
        }

        // Wrap player position
        ri.position = new Vector2(
            Mathf.Repeat(ri.position.x, 360f),
            Mathf.Repeat(ri.position.y, 360f)
        );
    }

    public void SendPing(PingChannelID id) {
        ambientSounds.pingSound.Play();
        pingMagnitude = 1f;
        pingHue = hues[(int)id / 2];
        etherSampler.RequestPing(id);
    }
}
