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

public class EtherCell {

    // Long-term memory of the ether cell
    public EtherFootprint[] footprints { private set; get; }
    // Short-term response of the ping/conversation
    public SignalTrace[] channelSignals { private set; get; }
    public int x { protected set; get; }
    public int y { protected set; get; }
    public EtherMood mood;

    public EtherCell(int x, int y) {
        this.x = x;
        this.y = y;

        footprints = new EtherFootprint[(int)PingChannelID.Count];
        for (int i = 0; i < (int)PingChannelID.Count; i++) {
            footprints[i] = new EtherFootprint((PingChannelID)i);
        }

        channelSignals = new SignalTrace[(int)PingChannelID.Count];
        for (int i = 0; i < channelSignals.Length; i++) {
            channelSignals[i] = new SignalTrace();
        }

        mood = new EtherMood();
    }

    public void Clk(float dt) {
        // Long-term memory fades half as fast for mood-biased tides
        float neutralityFactor = 1f / (2f - mood.neutrality);
        for (int i = 0; i < footprints.Length; i++) {
            footprints[i].strength = Mathf.Clamp(footprints[i].strength - (dt * neutralityFactor), 0f, 60f);
        }

        for (int i = 0; i < channelSignals.Length; i++) {
            channelSignals[i].Clk(dt);
        }
    }

    public void CopyTo(EtherCell other) {
        other.x = this.x;
        other.y = this.y;

        for (int i = 0; i < footprints.Length; i++) {
            other.footprints[i].strength = this.footprints[i].strength;
        }

        for (int i = 0; i < channelSignals.Length; i++) {
            this.channelSignals[i].CopyTo(other.channelSignals[i]);
        }

        other.mood = mood;
    }

    public void CollectFromSample(EtherCell[,] sampleArea, SignalTrace[,] traceArea, float[,] mixFactors) {
        this.x = sampleArea[1, 1].x;
        this.y = sampleArea[1, 1].y;

        float totalMixAmount = 0;
        for (int y = 0; y < 3; y++) {
            for (int x = 0; x < 3; x++) {
                totalMixAmount += mixFactors[x, y];
            }
        }

        if (totalMixAmount < float.Epsilon) {
            Debug.LogError("Total mix amount for sample is zero!");
            return;
        }

        for (int i = 0; i < footprints.Length; i++) {
            float totalStrength = 0;
            for (int y = 0; y < 3; y++) {
                for (int x = 0; x < 3; x++) {
                    totalStrength += sampleArea[x, y].footprints[i].strength * mixFactors[x, y];
                }
            }
            this.footprints[i].strength = totalStrength / totalMixAmount;
        }

        for (int i = 0; i < channelSignals.Length; i++) {
            for (int y = 0; y < 3; y++) {
                for (int x = 0; x < 3; x++) {
                    traceArea[x, y] = sampleArea[x, y].channelSignals[i];
                }
            }
            this.channelSignals[i].CollectFromSample(traceArea, mixFactors, totalMixAmount);
        }

        float totalTrade = 0;
        float totalHospitality = 0;
        for (int y = 0; y < 3; y++) {
            for (int x = 0; x < 3; x++) {
                totalTrade += sampleArea[x, y].mood.tradeVsPiracy * mixFactors[x, y];
                totalHospitality += sampleArea[x, y].mood.hospitalityVsPredation * mixFactors[x, y];
            }
        }

        this.mood = new EtherMood(totalTrade / totalMixAmount, totalHospitality / totalMixAmount);
    }
}
