using UnityEngine;

public class LeafSway : MonoBehaviour
{
    [Header("Main sway")]
    public float angle = 4f;        // kitna hilna hai (degrees)
    public float speed = 1.2f;      // hilne ki speed

    [Header("Gust (halka sa jhonka)")]
    public bool useGust = true;
    public float gustAngle = 2f;    // extra hilna
    public float gustSpeed = 0.4f;  // jhonke ki speed (slow rakhein)

    [Header("Variation")]
    public bool randomizeStart = true;

    float offset;
    float startZ;

    void Start()
    {
        startZ = transform.localEulerAngles.z;
        offset = randomizeStart ? Random.value * 10f : 0f;
    }

    void Update()
    {
        float t = Time.time + offset;

        float z = Mathf.Sin(t * speed) * angle;

        if (useGust)
            z += Mathf.Sin(t * gustSpeed) * Mathf.Sin(t * gustSpeed * 2.7f) * gustAngle;

        transform.localRotation = Quaternion.Euler(0, 0, startZ + z);
    }
}