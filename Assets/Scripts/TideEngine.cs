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

using UnityEngine;

/*
The purpose of the TideEngine is to centralize all functional boid code for easier
maintenance, without sacrificing efficiency for the garbage collector.
*/
public class TideEngine {

    // Primary statics and pointers:
    private static float MAX_TIDE_FORCE = 10f;

    private Rigidbody2D playerRi;
    private EtherSampler etherSampler;
	private Transform[] trArray;
    private Rigidbody2D[] riArray;
    private VisualTide[] cmArray;

    // Variable bank:
    private float sqrCrowdDist;
    private float sqrMaxPeerDist;
    private int i;
    private Vector2 nextForce;
    private float posAX;
    private float posAY;
    private float peerPressureX;
    private float peerPressureY;
    private float playerDiffX;
    private float playerDiffY;
    private float playerPressureX;
    private float playerPressureY;
    private float playerDist;
    private float cachedPlayerDist;
    private bool outsideOfCrowdDist;

    // Begin logic:
    public TideEngine(Rigidbody2D playerRi, EtherSampler etherSampler, Transform[] trArray, Rigidbody2D[] riArray, VisualTide[] cmArray) {
        this.playerRi = playerRi;
        this.etherSampler = etherSampler;
        this.trArray = trArray;
        this.riArray = riArray;
        this.cmArray = cmArray;
    }

    public void Setup(bool useFakeCrowdDistance) {
        sqrMaxPeerDist = etherSampler.maxPeerDistance * etherSampler.maxPeerDistance;
        float wrapRadius = (etherSampler.sampleWidth / 2f) * 1.5f;
        sqrCrowdDist = useFakeCrowdDistance ? (wrapRadius * wrapRadius) : (etherSampler.crowdDistance * etherSampler.crowdDistance);
    }

    public void StartIndex(int i) {
        this.i = i;
        posAX = riArray[i].position.x;
        posAY = riArray[i].position.y;
        peerPressureX = 0;
        peerPressureY = 0;
        playerPressureX = 0;
        playerPressureY = 0;
        
        // Catch a player world-wrapping ahead of time.
        // If a tide is father than 180 on an axis, then the player
        // has probably has their position wrapped to the other side
        // of the Ether.
        CachePlayerPositionDifference();

        bool adjustmentMade = false;
        if (playerDiffX > 180f) {
            posAX += 360f;
            adjustmentMade = true;
        }
        if (playerDiffX < -180f) {
            posAX -= 360f;
            adjustmentMade = true;
        }
        if (playerDiffY > 180f) {
            posAY += 360f;
            adjustmentMade = true;
        }
        if (playerDiffY < -180f) {
            posAY -= 360f;
            adjustmentMade = true;
        }

        if (adjustmentMade) {
            CachePlayerPositionDifference();
            riArray[i].position = new Vector2(posAX, posAY);
        }
        // End of catch
    }

    private void CachePlayerPositionDifference() {
        playerDiffX = playerRi.position.x - posAX;
        playerDiffY = playerRi.position.y - posAY;
    }

    public void CacheNextNoiseForce() {
        nextForce = Random.insideUnitCircle * 100f;
    }

    public void CalculatePeerPressure() {
        for (int j = 0; j < etherSampler.startingTideCount; j++) {
            if (i == j) continue;
            float posBX = riArray[j].position.x;
            float posBY = riArray[j].position.y;

            float diffX = posBX - posAY;
            float diffY = posBY - posAY;
            float dist = (diffX * diffX) + (diffY * diffY);
            // Do a check before performing a sqrt operation
            if (dist > sqrMaxPeerDist) continue;
            float cachedDist = Mathf.Sqrt(dist);
            float cachedForce = 50f / (dist * dist);
            float repelX = (-diffX / cachedDist) * cachedForce;
            float repelY = (-diffY / cachedDist) * cachedForce;
            peerPressureX = Mathf.Clamp(peerPressureX + (repelX / etherSampler.startingTideCount), -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
            peerPressureY = Mathf.Clamp(peerPressureY + (repelY / etherSampler.startingTideCount), -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
        }
    }

    public void StartPlayerPressure() {
        playerDist = (playerDiffX * playerDiffX) + (playerDiffY * playerDiffY);
        outsideOfCrowdDist = playerDist > sqrCrowdDist;
        if (outsideOfCrowdDist) {
            cachedPlayerDist = Mathf.Sqrt(playerDist);
        }
    }

    public void CalculatePlayerPressure() {
        if (outsideOfCrowdDist) {
            float playerForce = playerDist * 0.25f;
            playerPressureX = Mathf.Clamp((playerDiffX / cachedPlayerDist) * playerForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
            playerPressureY = Mathf.Clamp((playerDiffY / cachedPlayerDist) * playerForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
        }
    }

    public void CalculatePlayerCrowding() {
        float crowdForce = 1.5f;
        playerPressureX = Mathf.Clamp(playerDiffX * crowdForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
        playerPressureY = Mathf.Clamp(playerDiffY * crowdForce, -MAX_TIDE_FORCE, MAX_TIDE_FORCE);
    }

    public void DoWrapping() {
        if (outsideOfCrowdDist) {
            if (cachedPlayerDist > etherSampler.sampleWidth * 0.75f && cmArray[i].canTeleport) {
                /*
                This type of wrapping handles the illusion of distant tides being reused and placed
                ahead of the player, making it seem like there are a lot more tides than actually
                placed in the world.

                For handling of player position wrapping, see the catch in StartIndex(int i).
                */
                riArray[i].position = new Vector2(
                    playerRi.position.x + playerDiffX,
                    playerRi.position.y + playerDiffY
                );
                cmArray[i].FinishTeleport();
            }
        }
    }

    public void ApplyForces(float greaterTideX, float greaterTideY, float greaterTideHeave) {
        float postMult = riArray[i].mass * 7f * Time.fixedDeltaTime;
            
        riArray[i].AddForce(new Vector2(
            ((nextForce.x + peerPressureX + playerPressureX) * postMult)
            + (greaterTideX * greaterTideHeave),
            ((nextForce.y + peerPressureY + playerPressureY) * postMult)
            + (greaterTideY * greaterTideHeave)
        ));
    }
}
