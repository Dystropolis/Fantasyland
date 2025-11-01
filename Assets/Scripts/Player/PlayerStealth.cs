using UnityEngine;

public class PlayerStealth : MonoBehaviour
{
    [Header("State Flags (read-only at runtime)")]
    public bool isCrouching = false;
    public bool isRunning = false;
    public bool isWalking = false;

    [Header("Noise Levels")]
    [Tooltip("How loud crouch movement is. Lower = stealthier.")]
    public float crouchNoiseRadius = 2f;

    [Tooltip("How far walking can be heard.")]
    public float walkNoiseRadius = 6f;

    [Tooltip("How far running can be heard.")]
    public float runNoiseRadius = 10f;

    [Header("Crouch Height")]
    [Tooltip("Camera height while standing.")]
    public float standCamHeight = 1.7f;

    [Tooltip("Camera height while crouched.")]
    public float crouchCamHeight = 1.0f;

    [Tooltip("How fast camera slides between heights.")]
    public float crouchLerpSpeed = 8f;

    [Header("Refs")]
    public Transform cameraRoot;        // assign PlayerCamera parent transform (the thing that moves up/down)
    public PlayerController controller; // movement script so we can read speed

    void Update()
    {

        // determine if running
        // Shift = run only if we are not crouching
        isRunning = !isCrouching && Input.GetKey(KeyCode.LeftShift);

        // walking means moving, not running, not crouching
        // note: "moving" we can infer from controller velocity
        Vector3 horizontalVel = controller != null ? controller.GetVelocityXZ() : Vector3.zero;
        bool isMoving = horizontalVel.sqrMagnitude > 0.01f;

        isWalking = isMoving && !isRunning && !isCrouching;

        // Smooth camera height for crouch
        if (cameraRoot != null)
        {
            float targetHeight = isCrouching ? crouchCamHeight : standCamHeight;
            Vector3 p = cameraRoot.localPosition;
            p.y = Mathf.Lerp(p.y, targetHeight, Time.deltaTime * crouchLerpSpeed);
            cameraRoot.localPosition = p;
        }
    }

    // This answers: "how far away can enemies hear me RIGHT NOW?"
    // If you're standing still, you're basically silent = 0.
    public bool IsInStealthKillMode()
    {
        // Instead of Input.GetKey(KeyCode.LeftControl),
        // read from the player controller's crouch state.
        return controller != null && controller.IsCrouching;
    }

    // This is what AI uses to see if it can hear you.
    public float GetCurrentNoiseRadius()
    {
        if (controller == null) return 0f;

        // if crouching = basically silent
        if (controller.IsCrouching)
            return 0f;

        // Are we moving?
        Vector3 vel = controller.GetVelocityXZ();
        bool isMoving = vel.sqrMagnitude > 0.01f;
        if (!isMoving)
            return 0f;

        // Sprint louder than walk
        if (controller.IsSprinting)
            return runNoiseRadius;

        return walkNoiseRadius;
    }

}
