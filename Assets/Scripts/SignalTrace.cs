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
    public PingDirection lastDirection = PingDirection.Surrounding;

    public void ApplyPing(PingDirection direction, PingStrength strength) {
        if (strength != PingStrength.Silent) {
            this.lastDirection = direction;
            _hasBlink = true;
            switch(strength) {
                case PingStrength.Weak:
                default:
                    this.strength = 0.5f;
                    break;
                case PingStrength.Good:
                    this.strength = 0.75f;
                    break;
                case PingStrength.Strong:
                    this.strength = 1f;
                    break;
            }
        }
    }

    public void Clk(float dt) {
        strength = Mathf.MoveTowards(strength, 0, dt / 10f);
        _hasBlink |= (Random.Range(0, 100) < 4); //FIXME: It's bad practice to do random on every frame in a release version
    }
}
