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

public class AmbientSounds : MonoBehaviour {

    [HideInInspector]
	public float sparkleVolume = 0;
    [HideInInspector]
    public float altitudeMix = 1;
    [HideInInspector]
    public float velocityMix = 0;
    public AudioSource pingSound;
    public AudioSource sparkleAudio;
    public AudioSource layer1Audio;
    public AudioSource layer2Audio;
    public AudioSource layer3Audio;
    public AudioSource layer4Audio;

    // Start is called before the first frame update
    void Start() {
        //altitudeMix = 1;
    }

    // Update is called once per frame
    void Update() {
        //velocityMix = (Mathf.Sin(Time.time / 6) * 0.5f) + 0.5f;

        float _topMult = Mathf.Clamp01(altitudeMix - 1f);
        float topMult = _topMult * _topMult * _topMult * _topMult;
        float bottomMult = Mathf.Clamp01(1f - altitudeMix);
        float midMult = 1f - Mathf.Max(topMult, bottomMult);
        float lowerMult = Mathf.Clamp01(2f - altitudeMix);

        sparkleAudio.volume = sparkleVolume * lowerMult;
        sparkleAudio.pitch = (sparkleVolume * 0.2f) + Random.Range(1.8f, 2.2f) + (midMult);

        layer1Audio.volume = topMult;
        layer2Audio.volume = topMult;
        layer2Audio.pitch = 1f + (velocityMix * 0.01f);
        layer3Audio.volume = midMult;
        layer4Audio.volume = lowerMult;
        layer4Audio.pitch = 0.125f - (0.025f * midMult) + (velocityMix * 0.1f);
        //Debug.Log(velocityMix);
    }
}
