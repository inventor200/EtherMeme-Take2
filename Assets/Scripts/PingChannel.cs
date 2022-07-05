using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingChannel {

    public PingChannelID id { private set; get; }
    public bool askChannel { private set; get; }
    public float strength {
        get {
            float pingAmount = Mathf.Clamp01(1f - Mathf.Abs(_strength - 1f));
            return pingAmount * pingAmount;
        }
    }
    private float _strength;
    private int sparkleStage;
    public bool isSparkly {
        get {
            return sparkleStage == 1;
        }
    }
    public float hue { private set; get; }

    public PingChannel(PingChannelID id, bool askChannel, float hue) {
        this.id = id;
        this.askChannel = askChannel;
        this._strength = 0;
        this.sparkleStage = 2;
        this.hue = hue;
    }

    public void ResetPing() {
        _strength = 0;
        sparkleStage = 2;
    }

    public void Ping(float distanceOffset) {
        _strength = 1f + distanceOffset;
        sparkleStage = 0;
    }

    public void Sink(float dx) {
        _strength = Mathf.Clamp(_strength - dx, 0, 8);
        if (_strength <= 1f) {
            if (sparkleStage < 2) {
                sparkleStage++;
            }
        }
    }
}
