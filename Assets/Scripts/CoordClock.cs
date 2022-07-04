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

public class CoordClock : MonoBehaviour {

	public RectTransform hundredsHand;
    public RectTransform lowTensHand;
    public RectTransform highTensHand;

    [HideInInspector]
    public float value = 0;
    [HideInInspector]
    public float errorValue = 0;

    private ClockHand hundredsClockHand;
    private ClockHand lowTensClockHand;
    private ClockHand highTensClockHand;

    // Start is called before the first frame update
    void Awake() {
        hundredsClockHand = new ClockHand(hundredsHand, 360);
        lowTensClockHand = new ClockHand(lowTensHand, 90);
        highTensClockHand = new ClockHand(highTensHand, 90);
    }

    // Update is called once per frame
    void Update() {
        hundredsClockHand.value = value;
        lowTensClockHand.value = Mathf.Repeat(value - (errorValue / 2f), 90f);
        highTensClockHand.value = Mathf.Repeat(value + (errorValue / 2f), 90f);
        hundredsClockHand.Clk(Time.deltaTime);
        lowTensClockHand.Clk(Time.deltaTime);
        highTensClockHand.Clk(Time.deltaTime);
    }
}
