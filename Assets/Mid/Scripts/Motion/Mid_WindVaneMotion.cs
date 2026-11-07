using UnityEngine;

/// <summary>Required topic 4: explicit local Transform rotation, using the S08-S10 TRS idea.</summary>
public sealed class Mid_WindVaneMotion : MonoBehaviour
{
    [SerializeField] float centerDegrees = 20f;
    [SerializeField, Range(0f, 90f)] float sweepDegrees = 65f;
    [SerializeField, Min(4f)] float periodSeconds = 24f;
    Quaternion authoredRotation;
    public float DirectionDegrees { get; private set; }

    void Awake() => authoredRotation = transform.localRotation;

    void Update()
    {
        // Synthetic observation only: no live weather, rigid bodies or cumulative Rotate calls.
        // The slow main oscillation and smaller drift imitate a changing direction.
        float phase = Time.timeSinceLevelLoad * 2f * Mathf.PI / Mathf.Max(4f, periodSeconds);
        DirectionDegrees = centerDegrees + sweepDegrees * Mathf.Sin(phase)
            + 12f * Mathf.Sin(phase * 0.43f);
        // T(parent) * R(local) * S(child): the shaft stays still; all vane children turn together.
        transform.localRotation = authoredRotation * Quaternion.Euler(0f, DirectionDegrees, 0f);
    }

    void OnDisable() { if (Application.isPlaying) transform.localRotation = authoredRotation; }
}
