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

public class CloudLayer : MonoBehaviour {

    public float sideLength;
    private Transform tideFlowTransform;
    private Vector2 realPosition = Vector2.zero;
    private SpriteRenderer rend;
	private EtherSampler etherSampler;
    private float[] hues;
    private float hueCycle = 0;
    private float flowCycle = 0;

    // Start is called before the first frame update
    void Awake() {
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
        tideFlowTransform = GameObject.FindGameObjectWithTag("TideFlowTransform").transform;
        rend = GetComponent<SpriteRenderer>();
        hues = new float[] {
            EtherSampler.GetHueFromColor(etherSampler.playerHue),
            EtherSampler.GetHueFromColor(etherSampler.targetHue),
            EtherSampler.GetHueFromColor(etherSampler.predatorHue),
            EtherSampler.GetHueFromColor(etherSampler.freighterHue)
        };
    }

    // Update is called once per frame
    void Update() {
        float scale = etherSampler.ascendedScale / etherSampler.playerShip.altitudeProfile.moveScale;
        transform.localScale = new Vector3(scale, scale, scale);
        Vector2 normDirection = etherSampler.greaterTideDirection.normalized;
        float originalMagnitude = etherSampler.greaterTideDirection.magnitude;
        float magDirection = originalMagnitude / 20f;
        Vector2 direction = normDirection * magDirection * Time.deltaTime;
        Vector2 playerPos = (Vector2)etherSampler.playerTransform.position;

        float realSpan = 4 * sideLength * 360 / 10; // Cloud length times map length
        float halfSpan = realSpan / 2f;

        realPosition = new Vector2(
            Mathf.Repeat(realPosition.x + direction.x + halfSpan, realSpan) - halfSpan,
            Mathf.Repeat(realPosition.y + direction.y + halfSpan, realSpan) - halfSpan
        );
        
        // Wrapping alternative, but if the numbers check out, it won't actually work right.
        // Saving it in a comment, just in case.
        //realPosition += direction;
        /*float halfSpan = realSpan / 2;
        float longestAxis = Mathf.Max(Mathf.Abs(realPosition.x), Mathf.Abs(realPosition.y));
        while (longestAxis > halfSpan) {
            realPosition -= ((realPosition / longestAxis) * halfSpan);
        }*/

        Vector2 diff = realPosition - playerPos;
        float nextX = Mathf.Repeat(diff.x, sideLength) * scale;
        float nextY = Mathf.Repeat(diff.y, sideLength) * scale;
        transform.position = new Vector2(playerPos.x + nextX, playerPos.y + nextY);

        flowCycle = Mathf.Repeat(flowCycle + (originalMagnitude * Time.deltaTime / 32f), 360f);
        Quaternion flowRotation = Quaternion.Euler(0, 0, Mathf.Atan2(normDirection.y, normDirection.x) * Mathf.Rad2Deg);
        tideFlowTransform.localRotation = flowRotation;
        tideFlowTransform.localPosition = flowRotation * Vector2.right * flowCycle;

        // Make colors flash faster and slower, as though the Ether is breathing.
        float acceleration = (Mathf.Sin(Time.time * 2f) * 2f) + 5f;
        hueCycle = Mathf.Repeat(hueCycle + (acceleration * Time.deltaTime), 5f);
        Color chosenColor = Color.white;
        if (etherSampler.playerShip.altitudeProfile.hasEasyListening) {
            int chosenChannel = Mathf.Clamp(Mathf.FloorToInt(hueCycle), 0, 4);
            if (chosenChannel < 4) {
                SignalTrace channelTrace = etherSampler.store.ascendedCell.channelSignals[chosenChannel * 2];
                if (channelTrace.strength > 0.5f) {
                    chosenColor = Color.HSVToRGB(hues[chosenChannel], 1f, 1f);
                }
            }
        }

        chosenColor = Color.Lerp(rend.color, chosenColor, 10 * Time.deltaTime);

        //float chosenAlpha = altitudeAlpha.Evaluate(Mathf.Clamp(etherSampler.playerShip.currentAltitude, 0f, 3f));
        float chosenAlpha = etherSampler.playerShip.altitudeProfile.cloudAlpha;
        rend.color = new Color(chosenColor.r, chosenColor.g, chosenColor.b, chosenAlpha);
    }
}
