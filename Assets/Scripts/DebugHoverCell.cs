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

public class DebugHoverCell : MonoBehaviour {

    private static Color NORMAL_COLOR = new Color(0.5f, 0.5f, 0.5f);

    public TMPro.TMP_Dropdown dataMode;
    [HideInInspector]
	public EtherCell cell;
    [HideInInspector]
    private EtherSampler etherSampler;
    private Image img;

    void Awake() {
        img = GetComponent<Image>();
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
    }

    // Update is called once per frame
    void Update() {
        Color chosenColor = NORMAL_COLOR;

        int dataIndex = dataMode.value;
        if (dataIndex >= cell.footprints.Length) {
            dataIndex -= cell.footprints.Length;

            if (dataIndex >= cell.channelSignals.Length) {
                dataIndex -= cell.footprints.Length;
                switch (dataIndex) {
                    default:
                        chosenColor = NORMAL_COLOR;
                        break;
                    case 0: // Mood
                        chosenColor = cell.mood.GetColor(etherSampler, NORMAL_COLOR);
                        break;
                }
            }
            else {
                chosenColor = Color.Lerp(Color.black, GetThemeColor(dataIndex), Mathf.Clamp01(cell.channelSignals[dataIndex].strength));
            }
        }
        else {
            chosenColor = Color.Lerp(Color.black, GetThemeColor(dataIndex), Mathf.Clamp01(cell.footprints[dataIndex].strength));
        }

        img.color = chosenColor;
    }

    private Color GetThemeColor(int dataIndex) {
        switch (dataIndex / 2) {
            default:
            case 0:
                return etherSampler.playerHue;
            case 1:
                return etherSampler.targetHue;
            case 2:
                return etherSampler.predatorHue;
            case 3:
                return etherSampler.freighterHue;
        }
    }
}
