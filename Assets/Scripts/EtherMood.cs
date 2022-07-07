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

public struct EtherMood {

    public float tradeVsPiracy { private set; get; }
    public float hospitalityVsPredation { private set; get; }
    public float proTrade {
        get {
            return Mathf.Clamp01(tradeVsPiracy) * proHospitality;
        }
    }
    public float proPiracy {
        get {
            return Mathf.Clamp01(-tradeVsPiracy) * proHospitality;
        }
    }
    public float proHospitality {
        get {
            return Mathf.Clamp01(hospitalityVsPredation);
        }
    }
    public float proPredation {
        get {
            return Mathf.Clamp01(-hospitalityVsPredation);
        }
    }
    public float neutrality {
        get {
            return Mathf.Clamp01(1f - Mathf.Max(proTrade, proPiracy, proPredation));
        }
    }

    public EtherMood(float tradeVsPiracy, float hospitalityVsPredation) {
        this.tradeVsPiracy = tradeVsPiracy;
        this.hospitalityVsPredation = hospitalityVsPredation;
    }

    public EtherMood BiasForNeutrality(float factor) {
        return BiasForNeutrality(factor, Clone());
    }

    private EtherMood BiasForNeutrality(float factor, EtherMood clone) {
        clone.tradeVsPiracy = Mathf.Clamp(clone.tradeVsPiracy - (factor * Mathf.Sign(tradeVsPiracy)), -1f, 1f);
        clone.hospitalityVsPredation = Mathf.Clamp(clone.hospitalityVsPredation - (factor * Mathf.Sign(hospitalityVsPredation)), -1f, 1f);
        return clone;
    }

    public EtherMood BiasForChannel(PingChannelID id, float factor) {
        EtherMood clone = Clone();
        switch (id) {
            default:
                return BiasForNeutrality(factor, clone);
            case PingChannelID.Player_WasSeen:
            case PingChannelID.Player_WasAsked:
            case PingChannelID.Freighter_WasSeen:
            case PingChannelID.Freighter_WasAsked:
                // Favor more trade
                clone.tradeVsPiracy = Mathf.Clamp(clone.tradeVsPiracy + factor, -1, 1);
                // Favor more hospitality
                clone.hospitalityVsPredation = Mathf.Clamp(clone.hospitalityVsPredation + factor, -1, 1);
                break;
            case PingChannelID.Target_WasSeen:
            case PingChannelID.Target_WasAsked:
                // Favor more piracy
                clone.tradeVsPiracy = Mathf.Clamp(clone.tradeVsPiracy - factor, -1, 1);
                // Favor more hospitality
                clone.hospitalityVsPredation = Mathf.Clamp(clone.hospitalityVsPredation + factor, -1, 1);
                break;
            case PingChannelID.Predator_WasSeen:
            case PingChannelID.Predator_WasAsked:
                // Favor less trade and less piracy
                clone.tradeVsPiracy = Mathf.Clamp(clone.tradeVsPiracy - (factor * Mathf.Sign(tradeVsPiracy)), -1f, 1f);
                // Favor more predation
                clone.hospitalityVsPredation = Mathf.Clamp(clone.hospitalityVsPredation - factor, -1, 1);
                break;
        }
        return clone;
    }

    private EtherMood Clone() {
        EtherMood clone = new EtherMood();
        clone.tradeVsPiracy = this.tradeVsPiracy;
        clone.hospitalityVsPredation = this.hospitalityVsPredation;
        return clone;
    }

    public Color GetColor(EtherSampler etherSampler, Color defaultColor) {
        Color proTradeColor = etherSampler.playerHue;
        Color proPiracyColor = etherSampler.targetHue;
        Color proPredationColor = etherSampler.predatorHue;
        
        Color tradeVsPiracyColor = Color.Lerp(proPiracyColor, proTradeColor, (tradeVsPiracy + 1f) / 2f);
        Color hospitalityVsPredationColor = Color.Lerp(proPredationColor, tradeVsPiracyColor, (hospitalityVsPredation + 1f) / 2f);

        return Color.Lerp(hospitalityVsPredationColor, defaultColor, neutrality);
    }
}
