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
using UnityEngine.UI;
using UnityEngine;

public class DepthDiagram : MonoBehaviour {

	public Image playerDot;
    public RectTransform playerLabelLeft;
    public RectTransform playerLabelRight;
    public AnimationCurve altitudeToDepth;
    public ShipInEther playerShip;
    public Image instabilityWarning; //TODO: Animate
    public Image hackerWarning; //TODO: Animate

    private bool wasShallow = false;

    // Start is called before the first frame update
    void Start() {
        //
    }

    // Update is called once per frame
    void Update() {
        bool isShallow = playerShip.currentAltitude > 1.9f;

        if (isShallow != wasShallow) {
            wasShallow = isShallow;
            playerLabelLeft.gameObject.SetActive(isShallow);
            playerLabelRight.gameObject.SetActive(!isShallow);
        }

        playerDot.color = Color.HSVToRGB(0f, 0f, (Mathf.Repeat(Time.time * 2f, 1f) > 0.5f) ? 0f : 1f);
        playerDot.rectTransform.anchoredPosition = new Vector2(0, altitudeToDepth.Evaluate(Mathf.Clamp(playerShip.currentAltitude, 0f, 3f)));
    }
}