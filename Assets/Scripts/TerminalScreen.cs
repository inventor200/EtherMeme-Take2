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

public class TerminalScreen : MonoBehaviour {

	private static int LINE_COUNT = 6;

    public TMPro.TextMeshProUGUI lineText;
    public Image flash;

    private string[] lines = new string[LINE_COUNT];
    private float flashCount = 0;

    // Update is called once per frame
    void Update() {
        flashCount = Mathf.Clamp01(flashCount - Time.deltaTime);
        flash.color = new Color(1f, 1f, 1f, flashCount);
    }

    public void WriteLine(string line) {
        for (int i = LINE_COUNT - 1; i > 0; i--) {
            lines[i] = lines[i - 1];
        }
        lines[0] = line;

        string total = lines[LINE_COUNT - 1];
        for (int i = LINE_COUNT - 2; i >= 0; i--) {
            total += "\n" + lines[i];
        }

        lineText.text = total;
        flashCount = 1f;
    }

    public void WriteWarningLine(string line) {
        WriteLine("<color=#FFFF00>" + line + "</color>");
    }

    public void WriteErrorLine(string line) {
        WriteLine("<color=#FF0000>" + line + "</color>");
    }

    public void WriteGreenLine(string line) {
        WriteLine("<color=#00FF00>" + line + "</color>");
    }
}
