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

    public PostProcessVolume postProcessor;
    private Vignette vignette;
    [Space]
    public AmbientSounds ambientSounds;
    public MapDisplay mapDisplay;
    public CoordClock xClock;
    public CoordClock yClock;
    public DepthDiagram depthDiagram;
    public TerminalScreen terminalScreen;

    private bool hasLanded = false;
    private bool tetherStabilityBroken = false;

    void Awake() {
        AgentAwake();
        postProcessor.profile.TryGetSettings<Vignette>(out vignette);
    }

    // Start is called before the first frame update
    void Start() {
        AgentStart();
        terminalScreen.WriteLine("Mission start; diving to safe depth...");
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space) && hasLanded && altitudeProfile.allowControlInput && altitudeProfile.allowSonarPing) {
            PingForPlayer();
        }

        ShipSpeed nextSpeed = ShipSpeed.Halted;
        Vector2 nextDirection = Vector2.zero;

        if (currentSpeed == ShipSpeed.Stealth) {
            nextSpeed = ShipSpeed.Stealth;

            if (Input.GetKeyDown(KEY_STEALTH) && hasLanded) {
                nextSpeed = ShipSpeed.Halted;
            }
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

        mapDisplay.UpdateMap(currentSpeed, currentDirection, altitudeProfile.mapScale, expectedPosition, currentAltitude);

        // Sounds get higher with speed
        ambientSounds.velocityMix = speedScale;

        // Control vignette intensity with altitude
        vignette.intensity.value = altitudeProfile.vignetteFactor;

        // Update expected positions on coordinate clocks
        xClock.value = expectedPosition.x;
        xClock.errorValue = expectedError;
        yClock.value = expectedPosition.y;
        yClock.errorValue = expectedError;

        // Extra intro diving messages, timed against WIP diving sound
        // TODO: Separate the sound out into stems, for easier re-timing and pause compatibility
        if (!hasLanded) {
            if (currentAltitude <= 2f && !tetherStabilityBroken) {
                terminalScreen.WriteErrorLine("Tether stability system near limit!");
                tetherStabilityBroken = true;
            }
            else if (altitudeProfile.allowSonarPing) {
                hasLanded = true;
                terminalScreen.WriteWarningLine("Tether stability system disconnected");
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
    }
}
