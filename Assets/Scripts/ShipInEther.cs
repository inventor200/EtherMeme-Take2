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

    public static float STANDARD_DEPTH_TOLERANCE = 0.5f;

    public static float[][] SPEED_MODES = new float [][] {
        new float[] {100f, 1.2f},
        new float[] {200f, 2.5f}
    };
	public SpriteRenderer halo;
    public Transform arrow;
    public PostProcessVolume postProcessor;
    private Vignette vignette;
    [Space]
    public AmbientSounds ambientSounds;
    public RectTransform rootGrid;
    public AnimationCurve rootScaleCurve;
    public TopDownGrid standardGrid;
    public TopDownGrid ascendedGrid;
    public AnimationCurve vignetteStrengths;
    public TMPro.TextMeshProUGUI positionText;
    public CoordClock xClock;
    public CoordClock yClock;
    public float currentAltitude { private set; get; } = 2;
    private int goalAltitude = 1;

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
    private bool wasInteractingWithTides = false;
    private bool interactingWithTides = true;
    public bool isAscended {
        get {
            return currentAltitude > 1f + STANDARD_DEPTH_TOLERANCE;
        }
    }
    public bool isBuried {
        get {
            return currentAltitude < (1f - STANDARD_DEPTH_TOLERANCE);
        }
    }
    public bool isAtStandardDepth {
        get {
            return (currentAltitude >= (1f - STANDARD_DEPTH_TOLERANCE))
                && (currentAltitude <= 1f + STANDARD_DEPTH_TOLERANCE);
        }
    }

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
    }

    // Start is called before the first frame update
    void Start() {
        //
    }

    // Update is called once per frame
    // TODO: Engine is silent when in stealth
    // TODO: Refactor this class
    void Update() {
        pingMagnitude = Mathf.Clamp01(pingMagnitude - (Time.deltaTime / 2f));
        Color haloColor = Color.HSVToRGB(pingHue, 1f, pingMagnitude);
        
        if (Input.GetKeyDown(KeyCode.Space)) {
            SendPing(PingChannelID.Player_WasSeen);
        }

        ShipSpeed nextSpeed = ShipSpeed.Halted;
        Vector2 nextMoveDir = moveDir;

        if (currentSpeed == ShipSpeed.Stealth) {
            nextSpeed = ShipSpeed.Stealth;

            if (Input.GetKeyDown(KEY_STEALTH)) {
                nextSpeed = ShipSpeed.Halted;
            }

            float blendInColor = 0.15f;
            haloColor = new Color(blendInColor, blendInColor, blendInColor, 1f);
        }
        else {
            nextSpeed = ShipSpeed.Halted;

            if (!isBuried) {
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
            }

            if (Input.GetKeyDown(KEY_ASCEND) && goalAltitude < 2) {
                goalAltitude++;
            }
            else if (Input.GetKeyDown(KEY_DESCEND) && goalAltitude > 0) {
                goalAltitude--;
            }

            if (Input.GetKeyDown(KEY_STEALTH)) {
                nextSpeed = ShipSpeed.Stealth;
                //nextMoveDir = Vector2.zero; // expectAcceleration will slow us down
            }
        }

        currentAltitude = Mathf.MoveTowards(currentAltitude, goalAltitude, 0.25f * Time.deltaTime);

        currentSpeed = nextSpeed;
        moveDir = nextMoveDir.normalized;

        float speedScale = 0;
        float expectedSpeed = 0;

        if ((int)currentSpeed > (int)ShipSpeed.Halted) {
            float nextAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            float currentAngle = arrow.localRotation.eulerAngles.z;

            arrow.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowardsAngle(currentAngle, nextAngle, 360f * Time.deltaTime));
            speedScale = ri.velocity.magnitude / (SPEED_MODES[1][1] * etherSampler.altitudeScale);

            expectedAcceleration = Mathf.MoveTowards(expectedAcceleration, 1f, Time.deltaTime);
            expectedSpeed = SPEED_MODES[(int)currentSpeed][1];
        }
        else {
            expectedAcceleration = Mathf.MoveTowards(expectedAcceleration, 0f, Time.deltaTime);
            expectedSpeed = SPEED_MODES[0][1]; // We're actually slowing down from this
        }

        expectedPosition += moveDir * expectedSpeed * expectedAcceleration * Time.deltaTime;
        if (isAscended) {
            expectedPosition = (Vector2)transform.position; // Recalibrate when ascended
        }

        // Ensure wrapping
        expectedPosition = new Vector2(
            Mathf.Repeat(expectedPosition.x, 360f),
            Mathf.Repeat(expectedPosition.y, 360f)
        );

        string stealthInd = currentSpeed == ShipSpeed.Stealth ? "\n[Stealth]" : "";
        positionText.text = string.Format("{0,4}, {1,4}", Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)) + stealthInd;

        // Adjust visual grid
        float totalAltitudeScale = rootScaleCurve.Evaluate(Mathf.Clamp01(currentAltitude / 2f));
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
        interactingWithTides = isAtStandardDepth;

        // Control vignette intensity with altitude
        vignette.intensity.value = Mathf.Clamp01(vignetteStrengths.Evaluate(currentAltitude / 2f));

        xClock.value = expectedPosition.x;
        yClock.value = expectedPosition.y;
    }

    void FixedUpdate() {
        if (wasInteractingWithTides != interactingWithTides) {
            wasInteractingWithTides = interactingWithTides;
            gameObject.layer = LayerMask.NameToLayer(interactingWithTides ? "Standard" : "NonStandard");
        }
        
        float nonStandardMass = 100000;
        float nonStandardDrag = 100f;
        if (currentSpeed == ShipSpeed.Stealth) {
            ri.mass = interactingWithTides ? 100 : nonStandardMass;
            ri.drag = interactingWithTides ? 0f : nonStandardDrag;
        }
        else {
            ri.mass = interactingWithTides ? 500 : nonStandardMass;
            ri.drag = interactingWithTides ? 10f : nonStandardDrag;
            if (currentSpeed != ShipSpeed.Halted) {
                float dragAdjustment = ri.drag * 0.75f;
                float speed = SPEED_MODES[(int)currentSpeed][0] * etherSampler.altitudeScale;
                ri.AddForce(moveDir * ri.mass * speed * dragAdjustment * Time.deltaTime);
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

public enum ShipSpeed {
    Stealth = -2,
    Halted = -1,
    Cruise = 0,
    AheadFull = 1
}
