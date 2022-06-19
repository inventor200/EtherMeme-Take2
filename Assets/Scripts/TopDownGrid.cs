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

public class TopDownGrid : MonoBehaviour {

    public Image img;
    public float standardAlpha = 0.1f;
    public float screenResolution = 512;
    public float unitsPerWrap = 10;
    public bool isAscended;
    private RectTransform offsetTransform;

    //private EtherSampler etherSampler;

    void Awake() {
        offsetTransform = GetComponent<RectTransform>();
    }

    public void AdjustTo(Vector2 expectedPosition, float altitude, float cameraScale) {
        float pixelsPerUnit = -screenResolution / cameraScale;
        float wrapWidth = unitsPerWrap * -pixelsPerUnit;
        float expectedX = Mathf.Repeat(expectedPosition.x * pixelsPerUnit, wrapWidth);
        float expectedY = Mathf.Repeat(expectedPosition.y * pixelsPerUnit, wrapWidth);
        float buriedFactor = 1f - Mathf.Clamp01(altitude);
        offsetTransform.anchoredPosition = new Vector2(expectedX, expectedY);
        float ascendedFactor = Mathf.Clamp01(altitude - 1f);
        float entryFactor = 1f - Mathf.Clamp01(altitude - 2f);
        if (isAscended) {
            img.color = new Color(1f, 1f, 1f, standardAlpha * ascendedFactor * entryFactor);
        }
        else {
            img.color = new Color(1f, 1f, 1f, standardAlpha * (1f - ascendedFactor) * (1f - buriedFactor) * entryFactor);
        }
    }
}
