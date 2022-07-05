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

public class PingChannel {

    public PingChannelID id { private set; get; }
    public bool askChannel { private set; get; }
    public float strength {
        get {
            float pingAmount = Mathf.Clamp01(1f - Mathf.Abs(_strength - 1f));
            return pingAmount * pingAmount;
        }
    }
    private float _strength;
    private int sparkleStage;
    public bool isSparkly {
        get {
            return sparkleStage == 1;
        }
    }
    public float hue { private set; get; }

    public PingChannel(PingChannelID id, bool askChannel, float hue) {
        this.id = id;
        this.askChannel = askChannel;
        this._strength = 0;
        this.sparkleStage = 2;
        this.hue = hue;
    }

    public void ResetPing() {
        _strength = 0;
        sparkleStage = 2;
    }

    public void Ping(float distanceOffset) {
        _strength = 1f + distanceOffset;
        sparkleStage = 0;
    }

    public void Sink(float dx) {
        _strength = Mathf.Clamp(_strength - dx, 0, 8);
        if (_strength <= 1f) {
            if (sparkleStage < 2) {
                sparkleStage++;
            }
        }
    }
}
