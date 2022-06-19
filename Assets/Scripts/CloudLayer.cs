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
    public AnimationCurve altitudeAlpha;
    private Vector2 realPosition = Vector2.zero;
    private SpriteRenderer rend;
	private EtherSampler etherSampler;
    private float[] hues;
    private float hueCycle = 0;

    // Start is called before the first frame update
    void Awake() {
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
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
        float scale = etherSampler.ascendedScale / etherSampler.altitudeScale;
        transform.localScale = new Vector3(scale, scale, scale);
        Vector2 normDirection = etherSampler.greaterTideDirection.normalized;
        float magDirection = (etherSampler.greaterTideDirection.magnitude / 100f) + 10f;
        Vector2 direction = normDirection * magDirection * Time.deltaTime;
        Vector2 playerPos = (Vector2)etherSampler.playerTransform.position;
        realPosition = new Vector2(
            Mathf.Repeat(realPosition.x + direction.x, 14400f),
            Mathf.Repeat(realPosition.y + direction.y, 14400f)
        );
        //float fullLength = sideLength * scale;
        Vector2 diff = realPosition - playerPos;
        float nextX = Mathf.Repeat(diff.x, sideLength) * scale;
        float nextY = Mathf.Repeat(diff.y, sideLength) * scale;
        transform.position = new Vector2(playerPos.x + nextX, playerPos.y + nextY);

        // Make colors flash faster and slower, as though the Ether is breathing.
        float acceleration = (Mathf.Sin(Time.time * 2f) * 2f) + 5f;
        hueCycle = Mathf.Repeat(hueCycle + (acceleration * Time.deltaTime), 5f);
        int chosenChannel = Mathf.Clamp(Mathf.FloorToInt(hueCycle), 0, 4);
        Color chosenColor = Color.white;
        if (chosenChannel < 4) {
            if (etherSampler.channelsAscended[chosenChannel]) {
                chosenColor = Color.HSVToRGB(hues[chosenChannel], 1f, 1f);
            }
        }

        chosenColor = Color.Lerp(rend.color, chosenColor, 10 * Time.deltaTime);

        float chosenAlpha = altitudeAlpha.Evaluate(Mathf.Clamp(etherSampler.playerShip.currentAltitude, 0f, 3f));
        rend.color = new Color(chosenColor.r, chosenColor.g, chosenColor.b, chosenAlpha);
    }
}
