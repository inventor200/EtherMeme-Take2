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

public class CloudLayer : MonoBehaviour {

	public EtherSampler sampler;
    public float sideLength;
    public float ascensionScale = 10;
    public float standardAlpha = 0.1f;
    private Vector2 realPosition = Vector2.zero;
    private SpriteRenderer rend;

    // Start is called before the first frame update
    void Awake() {
        rend = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update() {
        float scale = 1f + (Mathf.Clamp01(2f - sampler.altitude) * (ascensionScale - 1f));
        transform.localScale = new Vector3(scale, scale, scale);
        Vector2 direction = sampler.greaterTideDirection / 100f;
        Vector2 playerPos = (Vector2)sampler.playerTransform.position;
        realPosition += direction * Time.deltaTime;
        float fullLength = sideLength * scale;
        //float halfLength = fullLength / 2f;
        Vector2 diff = (realPosition * scale) - playerPos;
        float nextX = Mathf.Repeat(diff.x, fullLength);
        float nextY = Mathf.Repeat(diff.y, fullLength);
        transform.position = new Vector2(playerPos.x + nextX, playerPos.y + nextY);

        float chosenAlpha = Mathf.Clamp01(sampler.altitude) * standardAlpha;
        rend.color = new Color(1f, 1f, 1f, chosenAlpha);
    }
}
