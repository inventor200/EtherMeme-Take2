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

public class SignalTrace {

    public static SignalTrace zero = new SignalTrace() {
        strength = 0,
        _hasBlink = false,
        lastDirection = PingDirection.Surrounding
    };

    public float strength = 0;
    public bool hasBlink {
        get {
            bool res = _hasBlink;
            _hasBlink = false;
            return res;
        }
        set {
            _hasBlink = value;
        }
    }
    private bool _hasBlink = false;
    private float blinkTimer = -1;
    public PingDirection lastDirection = PingDirection.Surrounding;

    public void ApplyPing(PingDirection direction, PingStrength strength, float mixFactor) {
        if (strength != PingStrength.Silent) {
            this.lastDirection = direction;
            _hasBlink = true;
            float newStrength;
            switch(strength) {
                case PingStrength.Weak:
                default:
                    newStrength = 0.5f * mixFactor;
                    break;
                case PingStrength.Good:
                    newStrength = 0.75f * mixFactor;
                    break;
                case PingStrength.Strong:
                    newStrength = 1f * mixFactor;
                    break;
            }
            this.strength = Mathf.Max(this.strength, newStrength);
        }
    }

    public void Clk(float dt) {
        blinkTimer -= dt;
        if (blinkTimer <= 0) {
            blinkTimer = Random.Range(0.1f, 0.75f);
            _hasBlink = true;
        }
        strength = Mathf.MoveTowards(strength, 0, dt / 10f);
    }

    public void CopyTo(SignalTrace other) {
        other.strength = this.strength;
        //other._hasBlink = this._hasBlink;
        other.lastDirection = this.lastDirection;
    }

    public void CollectFromSample(SignalTrace[,] sampleArea, float[,] mixAmounts, float totalMixAmount) {
        this.lastDirection = sampleArea[1, 1].lastDirection;

        float totalStrength = 0;
        //float totalBlink = 0;
        for (int y = 0; y < 3; y++) {
            for (int x = 0; x < 3; x++) {
                totalStrength += sampleArea[x, y].strength * mixAmounts[x, y];
                //totalBlink += sampleArea[x, y]._hasBlink ? mixAmounts[x, y] : 0f;
            }
        }

        this.strength = totalStrength / totalMixAmount;
        //this._hasBlink = totalBlink / totalMixAmount > 0.5f;
    }
}
