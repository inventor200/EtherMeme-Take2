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

public class ShipInEther : EtherAgent {

    public static KeyCode KEY_NORTH = KeyCode.W;
    public static KeyCode KEY_WEST = KeyCode.A;
    public static KeyCode KEY_SOUTH = KeyCode.S;
    public static KeyCode KEY_EAST = KeyCode.D;
    public static KeyCode KEY_AHEAD_FULL_MOD = KeyCode.LeftShift;
    public static KeyCode KEY_STEALTH = KeyCode.H;
    public static KeyCode KEY_ASCEND = KeyCode.R;
    public static KeyCode KEY_DESCEND = KeyCode.F;

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

    private float pingHue = 0;
    private float pingMagnitude = 0;
    private float[] hues;

    private float arrowSize = 1f;

    private bool hasLanded = false;
    private bool tetherStabilityBroken = false;

    void Awake() {
        AgentAwake();
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
        AgentStart();
        terminalScreen.WriteLine("Mission start; diving to safe depth...");
    }

    // Update is called once per frame
    // TODO: Refactor this class into player ship, and map grid
    void Update() {
        pingMagnitude = Mathf.Clamp01(pingMagnitude - (Time.deltaTime / 2f));
        Color haloColor = Color.HSVToRGB(pingHue, 1f, pingMagnitude);
        
        if (Input.GetKeyDown(KeyCode.Space) && hasLanded && altitudeProfile.allowControlInput && altitudeProfile.allowSonarPing) {
            SendPing(PingChannelID.Player_WasSeen);
        }

        ShipSpeed nextSpeed = ShipSpeed.Halted;
        Vector2 nextDirection = Vector2.zero;

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
                nextDirection += Vector2.up;
            }
            if (Input.GetKey(KEY_WEST)) {
                nextSpeed = ShipSpeed.Cruise;
                nextDirection += Vector2.left;
            }
            if (Input.GetKey(KEY_SOUTH)) {
                nextSpeed = ShipSpeed.Cruise;
                nextDirection += Vector2.down;
            }
            if (Input.GetKey(KEY_EAST)) {
                nextSpeed = ShipSpeed.Cruise;
                nextDirection += Vector2.right;
            }

            if (nextSpeed == ShipSpeed.Cruise && Input.GetKey(KEY_AHEAD_FULL_MOD)) {
                nextSpeed = ShipSpeed.AheadFull;
            }

            if (Input.GetKeyDown(KEY_ASCEND)) {
                Ascend(1);
            }
            else if (Input.GetKeyDown(KEY_DESCEND)) {
                Descend(1);
            }

            if (Input.GetKeyDown(KEY_STEALTH)) {
                nextSpeed = ShipSpeed.Stealth;
            }
        }

        AgentUpdate(nextSpeed, nextDirection);

        if (signalAvailabilityChanged) {
            if (altitudeProfile.hasTetherSignal) {
                terminalScreen.WriteGreenLine("Tether signal aquired; recalibrating...");
            }
            else if (hasLanded) {
                terminalScreen.WriteWarningLine("Tether signal lost");
            }
        }

        ambientSounds.engineOn = currentSpeed != ShipSpeed.Stealth;

        if ((int)currentSpeed > (int)ShipSpeed.Halted) {
            float nextAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            float currentAngle = arrow.localRotation.eulerAngles.z;

            arrow.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowardsAngle(currentAngle, nextAngle, 360f * Time.deltaTime));
        }

        // Adjust visual grid
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

        // Control vignette intensity with altitude
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
        AgentFixedUpdate();
    }
    
    protected override void HandlePingEffect(PingChannelID id) {
        ambientSounds.pingSound.Play();
        pingMagnitude = 1f;
        pingHue = hues[(int)id / 2];
    }
}
