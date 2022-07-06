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

public class VisualTide : MonoBehaviour {

    public ParticleSystem sparkles;
    public int sparkleChance = 25;
    public Vector2 sparkleTimeRange = new Vector2(5f, 30f);
    public float moodySparkMultiplier = 4f;
    public AnimationCurve sparkleVolumeCurve;

    public bool canTeleport {
        get {
            return teleportCooldown <= 0;
        }
    }
    public float sparkleVolume {
        get {
            return Mathf.Clamp01(Mathf.Max(sparkleVolumeCurve.Evaluate(Mathf.Clamp01(sparkleVolumeTime)), glowVolume - 0.5f) - 0.1f);
        }
    }

	private SpriteRenderer rend;
    private EtherSampler etherSampler;
    private Transform tideFlowTransform;
    private Vector2 perlinSeed;
    private float teleportCooldown = 0;
    private PingChannel[] channels;
    private float casualSparkleTimer;
    [HideInInspector]
    public EtherMood mood; //TODO: Get from sample
    private float sparkleVolumeTime = 1f;
    private float glowVolume = 0f;

    // Start is called before the first frame update
    void Awake() {
        ResetSparkles();
        casualSparkleTimer *= Random.Range(0f, 1f);
        perlinSeed = Random.insideUnitCircle * 1000;
        rend = GetComponent<SpriteRenderer>();
        rend.color = Color.black;
        channels = new PingChannel[(int)PingChannelID.Count];
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
        tideFlowTransform = GameObject.FindGameObjectWithTag("TideFlowTransform").transform;
        for (int i = 0; i < channels.Length; i++) {
            int colorIndex = i / 2;
            Color sampledColor;
            switch (colorIndex) {
                default:
                case 0:
                    sampledColor = etherSampler.playerHue;
                    break;
                case 1:
                    sampledColor = etherSampler.targetHue;
                    break;
                case 2:
                    sampledColor = etherSampler.predatorHue;
                    break;
                case 3:
                    sampledColor = etherSampler.freighterHue;
                    break;
            }
            float hue = EtherSampler.GetHueFromColor(sampledColor);
            channels[i] = new PingChannel((PingChannelID)i, i % 2 == 1, hue);
        }
    }

    void Update() {
        float perlin = Mathf.PerlinNoise(perlinSeed.x + Time.time, perlinSeed.y) * 0.5f;
        float delta = (perlin * perlin) + 0.25f;

        float pingValue = 0;
        float pingHue = 0;
        float totalHue = 0;

        for (int i = 0; i < channels.Length; i++) {
            PingChannel channel = channels[i];
            float strength = channel.strength;
            if (channel.askChannel) {
                float waveTime = Mathf.Repeat(perlinSeed.x + Time.time * 12, Mathf.PI * 2);
                strength *= Mathf.Sin(waveTime);
            }
            pingHue += channel.hue * strength;
            totalHue += strength;
            pingValue = Mathf.Max(pingValue, strength);
        }

        pingHue /= totalHue;

        float flowVisibility = etherSampler.playerShip.altitudeProfile.flowVisibility;
        float greaterTideHeave = etherSampler.greaterTideDirection.magnitude;
        Vector2 positionRelativeToGreaterTide = (Vector2)tideFlowTransform.InverseTransformPoint(transform.position);
        float yFactor = Mathf.Sin(positionRelativeToGreaterTide.y * Mathf.PI * 2) / 4f;
        float xFactor = (positionRelativeToGreaterTide.x + yFactor) * Mathf.PI * 2;
        float minFlow = 0.35f;
        float flowIntensity = Mathf.Clamp01(((greaterTideHeave / EtherSampler.GREATER_TIDE_STRENGTH) - minFlow)) / (1f - minFlow);
        float flowFactor = Mathf.Sin(xFactor) * flowIntensity * flowVisibility;
        float minValue = 0.5f;
        float standardValue = (flowFactor + minValue + (flowVisibility * (1f - minValue))) / 2f;

        float masterValue = Mathf.Clamp01((standardValue * delta) + (pingValue * 0.75f));
        glowVolume = masterValue;
        masterValue = Mathf.Clamp01(masterValue * etherSampler.playerShip.altitudeProfile.tideAlpha);
        
        bool renderAtAll = masterValue > 0.01f;
        if (rend.enabled != renderAtAll) {
            rend.enabled = renderAtAll;
        }

        if (renderAtAll) {
            rend.color = Color.HSVToRGB(pingHue, pingValue, masterValue);
        }
        if (!canTeleport) {
            teleportCooldown -= Time.deltaTime;
        }
        
        bool makeSparkles = false;
        for (int i = 0; i < channels.Length; i++) {
            channels[i].Sink(Time.deltaTime);
            if (channels[i].isSparkly && renderAtAll) {
                makeSparkles |= Random.Range(0, 100) < sparkleChance;
            }
        }

        float sparkleSpeedMult = Mathf.Lerp(1f, moodySparkMultiplier, Mathf.Clamp01(Mathf.Abs(1f - mood.neutrality)));
        casualSparkleTimer -= Time.deltaTime * sparkleSpeedMult;
        if (casualSparkleTimer <= 0) {
            makeSparkles = true;
        }

        if (makeSparkles) {
            if (renderAtAll) {
                ParticleSystem.MainModule particleMain = sparkles.main;

                Color sparkleColor = Color.HSVToRGB(pingHue, pingValue, 1f);
                Color proTradeColor = Color.HSVToRGB(channels[0].hue, 1f, 1f);
                Color proPiracyColor = Color.HSVToRGB(channels[1].hue, 1f, 1f);
                Color proPredationColor = Color.HSVToRGB(channels[2].hue, 1f, 1f);
                
                Color tradeVsPiracyColor = Color.Lerp(proPiracyColor, proTradeColor, (mood.tradeVsPiracy + 1f) / 2f);
                Color hospitalityVsPredationColor = Color.Lerp(proPredationColor, tradeVsPiracyColor, (mood.hospitalityVsPredation + 1f) / 2f);

                particleMain.startColor = Color.Lerp(hospitalityVsPredationColor, sparkleColor, mood.neutrality);
                sparkles.Play();
            }
            ResetSparkles();
            sparkleVolumeTime = 0f;
        }

        if (renderAtAll) {
            sparkleVolumeTime = Mathf.Clamp01(sparkleVolumeTime + Time.deltaTime);
        }
    }

    private void ResetSparkles() {
        casualSparkleTimer = Random.Range(sparkleTimeRange.x, sparkleTimeRange.y);
        sparkleVolumeTime = 1f;
    }

    public void FinishTeleport() {
        teleportCooldown = 0.5f;
        for (int i = 0; i < channels.Length; i++) {
            channels[i].ResetPing();
        }
        sparkles.Stop();
        sparkleVolumeTime = 1f;
    }

    public void Ping(float distanceOffset, PingChannelID id) {
        channels[(int)id].Ping(distanceOffset);
    }

    void OnCollisionEnter2D(Collision2D col) {
        if (col.transform.tag == "EtherAgent") {
            EtherAgent etherAgent = col.gameObject.GetComponent<EtherAgent>();
            if (etherAgent != null) {
                if (etherAgent.currentSpeed == ShipSpeed.Cruise) {
                    Ping(0, etherAgent.cruiseChannel);
                }
                else if (etherAgent.currentSpeed == ShipSpeed.AheadFull) {
                    Ping(0, PingChannelID.Predator_WasSeen);
                }
            }
        }
    }
}
