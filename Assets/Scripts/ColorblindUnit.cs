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

/*
I put a high priority on colorblind accessibility in everything I make.
Because color is a core part of reading the environment, I implement
a unit in the cockpit for separating out color data according to signal
channel, allowing players of any color capability to distinguish between
signals in the environment.
*/
public class ColorblindUnit : MonoBehaviour {

    private static float COMPASS_RADIUS = 48;

	public PingChannelID channelID;
    [Space]
    public UnitLED above;
    public UnitLED level;
    public Image compassDot;

    private EtherSampler etherSampler;
    private float[] hues;

    void Awake() {
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
        hues = new float[] {
            EtherSampler.GetHueFromColor(etherSampler.playerHue),
            EtherSampler.GetHueFromColor(etherSampler.targetHue),
            EtherSampler.GetHueFromColor(etherSampler.predatorHue),
            EtherSampler.GetHueFromColor(etherSampler.freighterHue)
        };
    }

    // Start is called before the first frame update
    void Start() {
        float hue = hues[(int)channelID / 2];
        compassDot.color = Color.HSVToRGB(hue, 1f, 1f);
        above.hue = hue;
        level.hue = hue;
    }

    // Update is called once per frame
    void Update() {
        SignalTrace seenTrace = etherSampler.playerShip.sampleCell.channelSignals[(int)channelID];
        SignalTrace askTrace = etherSampler.playerShip.sampleCell.channelSignals[(int)channelID + 1];
        SignalTrace levelTrace = SignalTrace.zero;

        bool isPlayerBuried = etherSampler.playerShip.altitudeProfile.hasBurialHazard;
        above.buriedMode = isPlayerBuried;
        if (isPlayerBuried) {
            ClkLED(seenTrace, askTrace, above);
        }
        else {
            ClkLED(seenTrace, askTrace, level);
            levelTrace = seenTrace.strength > askTrace.strength ? seenTrace : askTrace;
        }

        float angle;
        float angleNoise = 120;
        switch (levelTrace.lastDirection) {
            default:
            case PingDirection.Surrounding:
                angle = Random.Range(0, 360f);
                break;
            case PingDirection.East:
                angle = Random.Range(-angleNoise, angleNoise);
                break;
            case PingDirection.North:
                angle = 90 + Random.Range(-angleNoise, angleNoise);
                break;
            case PingDirection.West:
                angle = 180 + Random.Range(-angleNoise, angleNoise);
                break;
            case PingDirection.South:
                angle = 270 + Random.Range(-angleNoise, angleNoise);
                break;
        }
        float magnitude = Random.Range(0.75f, 1.25f) * levelTrace.strength;

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * magnitude;
        float y = Mathf.Sin(angle * Mathf.Deg2Rad) * magnitude;
        Vector2 compassPos = (new Vector2(x, y) + (Random.insideUnitCircle * 0.1f));
        if (compassPos.sqrMagnitude > 1f) compassPos = compassPos.normalized;
        compassPos *= COMPASS_RADIUS;
        Vector2 currentPos = compassDot.rectTransform.anchoredPosition;
        compassDot.rectTransform.anchoredPosition = Vector2.Lerp(currentPos, compassPos, 10 * Time.deltaTime);
    }

    private void ClkLED(SignalTrace seenTrace, SignalTrace askTrace, UnitLED led) {
        if (seenTrace.hasBlink || askTrace.hasBlink) {
            led.Blink(Mathf.Max(seenTrace.strength, askTrace.strength));
        }
    }
}
