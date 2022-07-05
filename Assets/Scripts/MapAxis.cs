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

public class MapAxis : MonoBehaviour {

	public TMPro.TextMeshProUGUI positiveLabel;
    public TMPro.TextMeshProUGUI negativeLabel;
    public float mapWidth = 10;
    public float gridWidth = 512;
    public bool yAxis;
    private RectTransform rect;

    void Awake() {
        rect = GetComponent<RectTransform>();
    }
    
    public void SetAxis(float value, Vector2 offset) {
        float position;
        if (yAxis) {
            position = Mathf.Repeat((mapWidth / 2f) + (value - offset.y), mapWidth) * gridWidth / mapWidth;
            rect.anchoredPosition = new Vector2(0, position);
        }
        else {
            position = Mathf.Repeat((mapWidth / 2f) + (value - offset.x), mapWidth) * gridWidth / mapWidth;
            rect.anchoredPosition = new Vector2(position, 0);
        }

        bool needsNegative = position > 400;
        positiveLabel.enabled = !needsNegative;
        negativeLabel.enabled = needsNegative;
        string label = "" + Mathf.RoundToInt(value) + "\u00b0";
        if (needsNegative) {
            negativeLabel.text = label;
        }
        else {
            positiveLabel.text = label;
        }
    }
}
