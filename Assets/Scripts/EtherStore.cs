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

public class EtherStore {

	public EtherCell[,] cells { private set; get; }
    public EtherCell ascendedCell { private set; get; }
    public int sideLength { private set; get; }

    public EtherStore(int sideLength) {
        this.sideLength = sideLength;
        cells = new EtherCell[sideLength, sideLength];
        for (int y = 0; y < sideLength; y++) {
            for (int x = 0; x < sideLength; x++) {
                cells[x, y] = new EtherCell(x, y);
            }
        }
        ascendedCell = new EtherCell(0, 0);
    }

    public void Clk(float dt) {
        for (int y = 0; y < sideLength; y++) {
            for (int x = 0; x < sideLength; x++) {
                cells[x, y].Clk(dt);
            }
        }
    }
}
