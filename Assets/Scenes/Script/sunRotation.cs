using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sunRotation : MonoBehaviour
{
    public GameObject terrain;

    public float sunSpeed;

    private Vector3 centrePoint;

    public Color midday;
    public Color rising;
    public Color setting;
    public Color color;
    
    private float cumulativeAngle;
    private bool sunPhase;

    void Start() {
        float length = terrain.GetComponent<DiamondSquare>().terrainLength;
        centrePoint = new Vector3(length / 2, 0f, length / 2);

        // start on the horizon
        this.transform.position = new Vector3(length / 2, 0f, -length / 2);

        // with no rotation
        this.transform.rotation = Quaternion.identity;

        cumulativeAngle = 0;
    }

    void Update() {
        float oldInterpolator = Mathf.Cos(cumulativeAngle * Mathf.PI / 180);

        float angle = -sunSpeed * Time.deltaTime;

        cumulativeAngle += angle;
        transform.RotateAround(centrePoint, Vector3.left, angle);

        float newInterpolator = Mathf.Cos(cumulativeAngle * Mathf.PI / 180);

        if ((oldInterpolator < 0) != (newInterpolator < 0))
            sunPhase = !sunPhase;

        // change the sun light color gradually depending on the angle rotated
        if (sunPhase)
            this.color = Color.Lerp(setting, midday, Mathf.Abs(Mathf.Sin(cumulativeAngle * Mathf.PI / 180)));
        else
            this.color = Color.Lerp(rising, midday, Mathf.Abs(Mathf.Sin(cumulativeAngle * Mathf.PI / 180)));
        
    }

    // used to pass position of light to shaders
    public Vector3 getWorldPosition() {
        return this.transform.position;
    }
}
