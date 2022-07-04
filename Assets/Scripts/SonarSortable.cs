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

public class SonarSortable {
    public int index;
    public float dotPr;
    public float dist;
    public float lastScore { private set; get; }
    public float distanceOffset {
        get {
            return lastScore / 4f;
        }
    }

    public SonarSortable(int index, float dotPr, float dist) {
        this.index = index;
        this.dotPr = dotPr;
        this.dist = dist;
        this.lastScore = 0;
    }

    public void CacheScore() {
        float dotScore = (2.5f - (dotPr + 1f)) * 2;
        lastScore = dotScore * dist;
    }
}
