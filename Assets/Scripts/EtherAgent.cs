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

public class EtherAgent : MonoBehaviour {

    protected static float DEPTH_CHANGE_TIME = 6f;

    // Force, avg m/s, avg error per meter
    protected static float[][] SPEED_MODES = new float [][] {
        new float[] {100f, 1.2f, 0.02f},
        new float[] {200f, 2.5f, 0.03f}
    };

    protected static float ERROR_PER_SEC_IDLE_STANDARD = 0.036f;
    protected static float ERROR_PER_SEC_STEALTH_STANDARD = 0.738f;
    protected static float ERROR_PER_SEC_BURIED = 0.000042f;

	public SpriteRenderer halo;
    public PingChannelID cruiseChannel;
    [Space]
    public bool skipsPhysics;
    public float currentAltitude { private set; get; }
    protected int goalAltitudeIndex = 1;
    public EtherAltitude altitudeProfile { private set; get; }
    public EtherAltitude altitudeBucket { private set; get; }
    public EtherCell sampleCell { private set; get; }
    protected EtherSampler etherSampler { private set; get; }
    protected Rigidbody2D ri { private set; get; }
    public ShipSpeed currentSpeed { private set; get; } = ShipSpeed.Halted;
    protected Vector2 currentDirection { private set; get; } = Vector2.zero;
    public Vector2 expectedPosition { private set; get; } = Vector2.zero;
    protected float expectedAcceleration { private set; get; }
    protected float expectedError { private set; get; }
    protected float speedScale { private set; get; }
    private bool wasInteractingWithTides;
    protected bool signalAvailabilityChanged { private set; get; }
    protected bool allowDrawing { private set; get; }
    //protected bool isMovingIntoStandard { private set; get; }
    protected float[] hues { private set; get; }
    private float haloSize = 2f;
    [HideInInspector]
    public Queue<SonarPing> pingRequests;
    private float pingHue = 0;
    private float pingMagnitude = 0;

    protected void AgentAwake() {
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
        ri = GetComponent<Rigidbody2D>();
        altitudeProfile = new EtherAltitude();
        currentAltitude = etherSampler.altitudes[etherSampler.altitudes.Length - 1].mainAltitude;
        altitudeBucket = EtherAltitude.UpdateAltitudeProfileAndGetBucket(currentAltitude, altitudeProfile, etherSampler.altitudes);
        hues = new float[] {
            EtherSampler.GetHueFromColor(etherSampler.playerHue),
            EtherSampler.GetHueFromColor(etherSampler.targetHue),
            EtherSampler.GetHueFromColor(etherSampler.predatorHue),
            EtherSampler.GetHueFromColor(etherSampler.freighterHue)
        };
        sampleCell = new EtherCell(0, 0);
        etherSampler.agents.Add(this);
        pingRequests = new Queue<SonarPing>();
    }

    protected void AgentStart() {
        expectedPosition = (Vector2)transform.position;
        gameObject.layer = LayerMask.NameToLayer(skipsPhysics ? "IgnoreTides" : (altitudeProfile.allowTideInteraction ? "Standard" : "NonStandard"));
    }

    protected void AgentUpdate(ShipSpeed nextSpeed, Vector2 nextDirection) {
        // Handle altitude changes
        bool didHaveSignal = altitudeProfile.hasTetherSignal;
        float goalAltitude = etherSampler.altitudes[goalAltitudeIndex].mainAltitude;
        currentAltitude = Mathf.MoveTowards(currentAltitude, goalAltitude, Time.deltaTime / DEPTH_CHANGE_TIME);
        altitudeBucket = EtherAltitude.UpdateAltitudeProfileAndGetBucket(currentAltitude, altitudeProfile, etherSampler.altitudes);
        bool nowHasSignal = altitudeProfile.hasTetherSignal;
        signalAvailabilityChanged = didHaveSignal != nowHasSignal;

        if (!altitudeProfile.allowLateralMovement && nextSpeed != ShipSpeed.Stealth) {
            nextSpeed = ShipSpeed.Halted;
        }

        currentSpeed = nextSpeed;
        currentDirection = nextDirection;

        // Handle speed and movement knowledge
        speedScale = 0;
        float expectedSpeed = 0;

        if ((int)currentSpeed > (int)ShipSpeed.Halted) {
            speedScale = ri.velocity.magnitude / (SPEED_MODES[1][1] * altitudeProfile.moveScale);
            expectedAcceleration = Mathf.MoveTowards(expectedAcceleration, 1f, Time.deltaTime);
            expectedSpeed = SPEED_MODES[(int)currentSpeed][1];
        }
        else {
            expectedAcceleration = Mathf.MoveTowards(expectedAcceleration, 0f, Time.deltaTime);
            expectedSpeed = SPEED_MODES[0][1]; // We're actually slowing down from this
        }

        expectedPosition += currentDirection * expectedSpeed * expectedAcceleration * Time.deltaTime;

        if (altitudeProfile.hasTetherSignal) {
            expectedPosition = (Vector2)transform.position; // Recalibrate when ascended
            expectedError = 0;
        }

        // Ensure wrapping
        expectedPosition = new Vector2(
            Mathf.Repeat(expectedPosition.x, 360f),
            Mathf.Repeat(expectedPosition.y, 360f)
        );

        sampleCell.Clk(Time.deltaTime); // Fake updates between 20FPS sampler updates

        // Decide rendering
        allowDrawing = skipsPhysics ? (
            etherSampler.playerShip.altitudeBucket == altitudeBucket
            && !(currentSpeed == ShipSpeed.Stealth && haloSize < 1.1f)
            && altitudeProfile.renderNPCs
        ) : true;

        // Handle rendering
        halo.enabled = allowDrawing;
        if (allowDrawing) {
            // Adjust halo
            pingMagnitude = Mathf.Clamp01(pingMagnitude - (Time.deltaTime / 2f));
            Color haloColor = Color.HSVToRGB(pingHue, 1f, pingMagnitude);
            if (currentSpeed == ShipSpeed.Stealth) {
                float blendInColor = 0.15f;
                haloColor = new Color(blendInColor, blendInColor, blendInColor, 1f);
            }

            float nextSize = currentSpeed == ShipSpeed.Stealth ? 1f : 2f;
            haloSize = Mathf.Lerp(haloSize, nextSize, 5 * Time.deltaTime);
            halo.transform.localScale = new Vector3(haloSize, haloSize, haloSize);

            halo.color = haloColor;
        }

        // If Agent Collision Is Implemented:
        // If this agent is not in an altitude that allows for tide interaction, but
        // is moving into one that IS, then turn on this flag.
        // Also make sure we are in a situation to botch the agent's position without
        // altering the expected position.
        /*
        EtherAltitude goalAltitudeObject = etherSampler.altitudes[goalAltitudeIndex];
        isMovingIntoStandard = (goalAltitudeObject.allowTideInteraction
            && !altitudeProfile.allowTideInteraction
            && !altitudeProfile.hasTetherSignal
            && altitudeBucket != goalAltitudeObject);
        */
    }

    protected void AgentFixedUpdate() {
        //TODO: Impart footprint trace on a sample area when moving
        //TODO: Affect mood of sample area while moving, based on speed
        // These would be best-done by caching the total net affect, and then the cache
        // will be read and applied when pings are handled for this agent.

        if (wasInteractingWithTides != altitudeProfile.allowTideInteraction && !skipsPhysics) {
            wasInteractingWithTides = altitudeProfile.allowTideInteraction;
            gameObject.layer = LayerMask.NameToLayer(altitudeProfile.allowTideInteraction ? "Standard" : "NonStandard");
        }

        /*
        if (isMovingIntoStandard) {
            // If Agent Collision Is Implemented:
            // Do a circle cast upon the Standard layer.
            // If we are about to collide with another Standard-layer object,
            // then forcefully alter our position to prevent collision and overlap.
            //
            // Otherwise, it would make it much easier to handle logic and future
            // netcode if each player agent is the only thing that collides with
            // tides, and everything else just pretends to and doesn't collide
            // at all.
        }
        */
        
        float nonStandardMass = 100000;
        float nonStandardDrag = 100f;
        float physicsModeFactor = skipsPhysics ? 0.75f : 1f;
        if (currentSpeed == ShipSpeed.Stealth) {
            ri.mass = altitudeProfile.allowTideInteraction ? 100 : nonStandardMass;
            ri.drag = altitudeProfile.allowTideInteraction ? 0f : nonStandardDrag;
            if (altitudeProfile.allowTideInteraction) {
                expectedError += ERROR_PER_SEC_STEALTH_STANDARD * Time.fixedDeltaTime;
            }
            else if (altitudeProfile.hasBurialHazard) {
                expectedError += ERROR_PER_SEC_BURIED * Time.fixedDeltaTime;
            }
        }
        else {
            ri.mass = altitudeProfile.allowTideInteraction ? 500 : nonStandardMass;
            ri.drag = altitudeProfile.allowTideInteraction ? 10f : nonStandardDrag;

            if (altitudeProfile.allowTideInteraction) {
                expectedError += ERROR_PER_SEC_IDLE_STANDARD * Time.fixedDeltaTime;
            }
            else if (altitudeProfile.hasBurialHazard) {
                expectedError += ERROR_PER_SEC_BURIED * Time.fixedDeltaTime;
            }
            
            if (currentSpeed != ShipSpeed.Halted && altitudeProfile.allowLateralMovement) {
                float dragAdjustment = ri.drag * 0.75f;
                float speed = SPEED_MODES[(int)currentSpeed][0] * altitudeProfile.moveScale;
                expectedError += SPEED_MODES[(int)currentSpeed][1] * altitudeProfile.moveScale
                    * SPEED_MODES[(int)currentSpeed][2] * Time.fixedDeltaTime;
                ri.AddForce(currentDirection * ri.mass * speed * dragAdjustment * physicsModeFactor * Time.fixedDeltaTime);
            }
        }

        // Wrap player position
        ri.position = new Vector2(
            Mathf.Repeat(ri.position.x, 360f),
            Mathf.Repeat(ri.position.y, 360f)
        );
    }

    protected virtual void HandlePingEffect(PingChannelID id) {
        //
    }

    protected void PingForPlayer() {
        SendPing(PingChannelID.Player_WasSeen);
    }

    protected void PingForTarget() {
        SendPing(PingChannelID.Target_WasSeen);
    }

    protected void PingForPredator() {
        SendPing(PingChannelID.Predator_WasSeen);
    }

    protected void PingForFreighter() {
        SendPing(PingChannelID.Freighter_WasSeen);
    }

    private void SendPing(PingChannelID id) {
        if (id == cruiseChannel) {
            id++; // An agent will never ask about itself, but rather ask if anyone else has.
        }
        if (allowDrawing) {
            HandlePingEffect(id);
            pingMagnitude = 1f;
            pingHue = hues[(int)id / 2];
        }
        etherSampler.RequestPing(this, id);
    }

    protected void GoToAltitude(int index) {
        int delta = index - goalAltitudeIndex;
        if (delta > 0) {
            Ascend(delta);
        }
        else if (delta < 0) {
            Descend(-delta);
        }
    }

    protected void Ascend(int levels) {
        for (int i = 0; i < levels && goalAltitudeIndex < etherSampler.altitudes.Length - 1; i++) {
            ChangeVerticalGoal(1);
        }
    }

    protected void Descend(int levels) {
        for (int i = 0; i < levels && goalAltitudeIndex > 0; i++) {
            ChangeVerticalGoal(-1);
        }
    }

    private void ChangeVerticalGoal(int direction) {
        int altitudeChange = 0;

        altitudeChange += direction;
        while (etherSampler.altitudes[goalAltitudeIndex + altitudeChange].forcePassThrough) {
            altitudeChange += direction; // Pass through
        }

        if (etherSampler.altitudes[goalAltitudeIndex + altitudeChange].hasExclusiveEntry) {
            altitudeChange = 0; // Cancel change
        }

        goalAltitudeIndex += altitudeChange;
    }
}
