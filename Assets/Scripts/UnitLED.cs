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

public class UnitLED : MonoBehaviour {

	private Image led;
    [HideInInspector]
    public float hue;
    [HideInInspector]
    public bool buriedMode = false;

    private float flash;
    private float brightness;

    // Start is called before the first frame update
    void Awake() {
        led = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update() {
        flash = Mathf.MoveTowards(flash, 0, Time.deltaTime * 8);
        brightness = Mathf.MoveTowards(brightness, 0, Time.deltaTime / 3f);
        float curvedFlash = flash * 1.5f;
        // When buried, only the strongest signals get through, and the dropoff is faster
        float totalBrightness = Mathf.Clamp01((Mathf.Max(curvedFlash, brightness) * 1.5f) + (buriedMode ? -0.85f : 0f));
        led.color = Color.HSVToRGB(hue, 1f, (totalBrightness * 0.9f) + 0.1f);
    }

    public void Blink(float strength) {
        flash = Mathf.Clamp01(strength);
        brightness = Mathf.Max(flash * 0.85f, brightness);
    }
}
