using UnityEngine;

public class DialogueCameraLook : MonoBehaviour
{
    [Tooltip("How fast the camera aligns toward the NPC head.")]
    public float lookSpeed = 8f;

    [Tooltip("Optional pitch clamp (degrees) away from current to avoid extreme snaps.")]
    public float maxPitchDeltaPerSec = 180f;

    [Tooltip("Optional yaw clamp (degrees) per second.")]
    public float maxYawDeltaPerSec = 360f;

    void LateUpdate()
    {
        if (!DialogueInteractable.IsDialogueOpen) return;

        Transform tgt = DialogueInteractable.CurrentHeadTarget;
        if (!tgt) return;

        Vector3 dir = tgt.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // Optional clamp: convert to local yaw/pitch deltas and cap per second to prevent overshoot
        Quaternion current = transform.rotation;

        // Smooth slerp toward target
        transform.rotation = Quaternion.Slerp(current, target, Time.unscaledDeltaTime * lookSpeed);
    }
}
