using UnityEngine;
public class RaysRotate : MonoBehaviour
{
    [Tooltip("Degrees per second. Keep this slow (5-15) for a gentle glow effect. Negative = spin the other way.")]
    [SerializeField] private float degreesPerSecond = 8f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}