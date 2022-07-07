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

public class DebugPreviewGrid : MonoBehaviour {

	public DebugHoverCell cellPrefab;
    public Transform overlayGroup;
    public RectTransform playerMarker;
    public RectTransform sampleMarker;
    private EtherSampler etherSampler;
    private ShipInEther playerShip;
    private EtherStore store;
    private DebugHoverCell[,] grid;

    // Start is called before the first frame update
    void Start() {
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
        playerShip = etherSampler.playerShip;
        store = etherSampler.store;
        grid = new DebugHoverCell[store.sideLength, store.sideLength];
        for (int y = 0; y < store.sideLength; y++) {
            for (int x = 0; x < store.sideLength; x++) {
                DebugHoverCell clone;
                if (x == 0 && y == 0) {
                    clone = cellPrefab;
                }
                else {
                    clone = (Instantiate(cellPrefab.transform, transform) as Transform).GetComponent<DebugHoverCell>();
                }
                grid[x, y] = clone;
                clone.cell = store.cells[x, y];
            }
        }
        overlayGroup.SetAsLastSibling();
        sampleMarker.transform.SetAsLastSibling();
    }

    // Update is called once per frame
    void Update() {
        playerMarker.anchoredPosition = GradientPosToDebugPos(
            etherSampler.GetGradientPosFromWorldPos(playerShip.transform.position)
        );

        sampleMarker.anchoredPosition = GradientPosToDebugPos(
            etherSampler.GetCellCoordFromWorldPos(playerShip.transform.position)
        );
    }

    private Vector2 GradientPosToDebugPos(Vector2 gradientPosition) {
        float sampleX = (gradientPosition.x * 14) + 5;
        float sampleY = -(gradientPosition.y * 14) - 5;
        return new Vector2(sampleX, sampleY);
    }
}
