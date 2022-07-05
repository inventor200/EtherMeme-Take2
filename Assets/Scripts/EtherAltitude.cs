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

[System.Serializable]
public class EtherAltitude {

    public string name;
	public float minAltitude;
    public float mainAltitude;
    public float maxAltitude;
    public bool hasTetherSignal;
    public bool hasEasyListening;
    public bool hasRestrictedVision;
    public bool canSeeUp;
    public bool canSeeDown;
    public bool forcePassThrough;
    public bool hasInstabilityHazard;
    public bool hasBurialHazard;
    public bool allowSonarPing;
    public bool allowLateralMovement;
    public bool allowTideInteraction;
    public bool allowControlInput;
    public bool hasExclusiveEntry;
    public TideMode tideMode;
    public float cloudAlpha;
    public float tideAlpha;
    public float mapScale;
    public float moveScale;
    public float mapAlpha;
    public float vignetteFactor;
    public float diagramYOffset;
    public float flowVisibility;

    public static float Lerp(float higherAltitude, float levelAltitude, float lowerAltitude,
        float currentAltitude, float higherValue, float mainValue, float lowerValue) {

        float highDelta = higherAltitude - levelAltitude;
        float lowDelta = lowerAltitude - levelAltitude;
        float mainDelta = currentAltitude - levelAltitude;

        float higherFactor = Mathf.Abs(highDelta) < float.Epsilon ? 0 : Mathf.Clamp01(mainDelta / highDelta);
        float lowerFactor = Mathf.Abs(lowDelta) < float.Epsilon ? 0 : Mathf.Clamp01(mainDelta / lowDelta);
        float levelFactor = 1f - Mathf.Max(lowerFactor, higherFactor);

        return (higherValue * higherFactor) + (mainValue * levelFactor) + (lowerValue * lowerFactor);
    }

    // Returns the registered ether altitude that best-matches the floating-point altitude.
    public static EtherAltitude UpdateAltitudeProfileAndGetBucket(float currentAltitude, EtherAltitude altitudeProfile, EtherAltitude[] altitudes) {
        EtherAltitude above = altitudes[altitudes.Length - 1];
        EtherAltitude level = above;
        EtherAltitude below = altitudes[altitudes.Length - 2];
        for (int i = 1; i < altitudes.Length - 1; i++) {
            if (currentAltitude >= altitudes[i].minAltitude && currentAltitude < altitudes[i].maxAltitude) {
                above = altitudes[i + 1];
                level = altitudes[i];
                below = altitudes[i - 1];
                break;
            }
        }
        if (currentAltitude < altitudes[0].maxAltitude) {
            above = altitudes[1];
            level = altitudes[0];
            below = altitudes[0];
        }

        altitudeProfile.hasTetherSignal = level.hasTetherSignal;
        altitudeProfile.hasEasyListening = level.hasEasyListening;
        altitudeProfile.hasRestrictedVision = level.hasRestrictedVision;
        altitudeProfile.canSeeUp = level.canSeeUp;
        altitudeProfile.canSeeDown = level.canSeeDown;
        altitudeProfile.forcePassThrough = level.forcePassThrough;
        altitudeProfile.hasInstabilityHazard = level.hasInstabilityHazard;
        altitudeProfile.hasBurialHazard = level.hasBurialHazard;
        altitudeProfile.allowSonarPing = level.allowSonarPing;
        altitudeProfile.allowLateralMovement = level.allowLateralMovement;
        altitudeProfile.allowTideInteraction = level.allowTideInteraction;
        altitudeProfile.allowControlInput = level.allowControlInput;
        altitudeProfile.hasExclusiveEntry = level.hasExclusiveEntry;

        altitudeProfile.tideMode = level.tideMode;

        altitudeProfile.cloudAlpha = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.cloudAlpha, level.cloudAlpha, below.cloudAlpha
        );

        altitudeProfile.tideAlpha = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.tideAlpha, level.tideAlpha, below.tideAlpha
        );

        altitudeProfile.mapScale = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.mapScale, level.mapScale, below.mapScale
        );

        altitudeProfile.moveScale = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.moveScale, level.moveScale, below.moveScale
        );

        altitudeProfile.mapAlpha = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.mapAlpha, level.mapAlpha, below.mapAlpha
        );

        altitudeProfile.vignetteFactor = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.vignetteFactor, level.vignetteFactor, below.vignetteFactor
        );

        altitudeProfile.diagramYOffset = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.diagramYOffset, level.diagramYOffset, below.diagramYOffset
        );

        altitudeProfile.flowVisibility = Lerp(
            above.mainAltitude, level.mainAltitude, below.mainAltitude,
            currentAltitude,
            above.flowVisibility, level.flowVisibility, below.flowVisibility
        );

        return level;
    }
}
