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

public class MapDisplay : MonoBehaviour {

	public Transform arrow;
    public RectTransform rootGrid;
    public TopDownGrid standardGrid;
    public TopDownGrid ascendedGrid;
    public MapAxis xAxis;
    public MapAxis yAxis;
    private float arrowSize = 1f;
    private EtherSampler etherSampler;

    void Awake() {
        etherSampler = GameObject.FindGameObjectWithTag("EtherSampler").GetComponent<EtherSampler>();
    }

    public void UpdateMap(ShipSpeed currentSpeed, Vector2 currentDirection, float totalAltitudeScale, Vector2 expectedPosition, float currentAltitude) {
        if ((int)currentSpeed > (int)ShipSpeed.Halted) {
            float nextAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            float currentAngle = arrow.localRotation.eulerAngles.z;

            arrow.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowardsAngle(currentAngle, nextAngle, 360f * Time.deltaTime));
        }

        // Adjust visual grid
        rootGrid.localScale = new Vector3(totalAltitudeScale, totalAltitudeScale, totalAltitudeScale);
        standardGrid.AdjustTo(expectedPosition, currentAltitude, etherSampler.cameraScale);
        ascendedGrid.AdjustTo(expectedPosition, currentAltitude, etherSampler.cameraScale);

        // Adjust arrow
        float nextSize = currentSpeed == ShipSpeed.Stealth ? 0f : 1f;
        arrowSize = Mathf.Lerp(arrowSize, nextSize, 5 * Time.deltaTime);
        arrow.localScale = new Vector3(arrowSize, arrowSize, arrowSize);

        float mapScale = etherSampler.playerShip.altitudeProfile.mapScale;
        xAxis.mapWidth = 10f / mapScale;
        yAxis.mapWidth = 10f / mapScale;

        float increment = mapScale > 0.15f ? 10 : 60;
        xAxis.SetAxis(GetIncrement(expectedPosition.x, increment), expectedPosition);
        yAxis.SetAxis(GetIncrement(expectedPosition.y, increment), expectedPosition);
    }

    private static float GetIncrement(float value, float increment) {
        return Mathf.Round(value / increment) * increment;
    }
}
