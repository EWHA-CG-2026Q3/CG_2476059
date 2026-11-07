using UnityEngine;

/// <summary>Optional topic 4: a SECOND object's translation, independent of the vane's rotation.</summary>
public sealed class Mid_WindNeedleMotion : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float verticalTravel = 0.50f;
    [SerializeField, Min(2f)] float periodSeconds = 8f;
    Vector3 authoredLocalPosition;
    public float Strength01 { get; private set; }

    void Awake() => authoredLocalPosition = transform.localPosition;

    void Update()
    {
        float phase = Time.timeSinceLevelLoad * 2f * Mathf.PI / Mathf.Max(2f, periodSeconds);
        Strength01 = 0.5f + 0.5f * Mathf.Sin(phase);
        // S08-S10 translation idea: add T to the ORIGINAL position, never accumulate each frame.
        // Only local Y changes. The parent rail and the pointer's X/Z stay fixed.
        Vector3 translation = Vector3.up * ((Strength01 - 0.5f) * verticalTravel);
        transform.localPosition = authoredLocalPosition + translation;
    }

    void OnDisable() { if (Application.isPlaying) transform.localPosition = authoredLocalPosition; }
}
