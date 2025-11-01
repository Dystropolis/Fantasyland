using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    [Header("Interact")]
    public float interactDistance = 3f;
    public bool isLocked = false;
    [Tooltip("Layers the interact ray can hit. The door's own layer will be auto-included at runtime.")]
    public LayerMask interactMask = ~0;

    [Header("Door Motion")]
    [Tooltip("Pivot / hinge transform that rotates (local Y is hinge axis).")]
    public Transform doorTransform;

    [Tooltip("Yaw degrees from closed to fully open.")]
    public float openAngle = 90f;

    [Tooltip("Degrees/second-ish lerp factor.")]
    public float openSpeed = 6f;

    [Tooltip("Local Y when fully closed. Leave 0 to capture at Start.")]
    public float closedY = 0f;

    [Header("Side Definition (Designer Controlled)")]
    [Tooltip("Place an empty child here. Its FORWARD (+Z) defines 'Side A'. If player is on Side A, door opens using +openAngle, otherwise -openAngle.")]
    public Transform sideRef;
    [Tooltip("Flip this if your Side A/B logic feels reversed.")]
    public bool invertSideLogic = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;

    // runtime
    private bool isOpen = false;
    private bool isMoving = false;
    private float targetY;
    private Camera playerCam;
    private int doorLayerMaskBit;

    void Start()
    {
        if (!doorTransform) doorTransform = transform;
        if (!sideRef) sideRef = doorTransform; // safe default: use door transform

        playerCam = Camera.main;

        // capture closed yaw if not set
        if (Mathf.Abs(closedY) < 0.0001f)
            closedY = doorTransform.localEulerAngles.y;

        SetDoorYInstant(closedY);
        isOpen = false;
        isMoving = false;
        targetY = closedY;

        // Ensure the ray can hit this door's layer even if the mask was misconfigured
        int doorLayer = doorTransform.gameObject.layer;
        doorLayerMaskBit = 1 << doorLayer;
        interactMask |= doorLayerMaskBit;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && CanInteractByLook())
        {
            Toggle();
        }

        if (isMoving)
        {
            float currentY = doorTransform.localEulerAngles.y;
            float newY = Mathf.LerpAngle(currentY, targetY, Time.deltaTime * openSpeed);
            SetDoorYInstant(newY);

            if (Mathf.Abs(Mathf.DeltaAngle(newY, targetY)) < 0.5f)
            {
                SetDoorYInstant(targetY);
                isMoving = false;

                if (!isOpen)
                    SetDoorYInstant(closedY);
            }
        }
    }

    bool CanInteractByLook()
    {
        if (!playerCam) return false;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            // Accept if this door or any child was hit
            if (hit.transform == doorTransform || hit.transform.IsChildOf(doorTransform)) return true;
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) return true;
        }
        return false;
    }

    void Toggle()
    {
        if (!isOpen)
        {
            if (isLocked)
            {
                if (audioSource && lockedSound) audioSource.PlayOneShot(lockedSound);
                return;
            }

            // Decide side by projecting player position relative to sideRef
            Vector3 playerPos = playerCam ? playerCam.transform.position : (Camera.main ? Camera.main.transform.position : Vector3.zero);
            bool onSideA = IsPlayerOnSideA(playerPos);

            float yaw = closedY + (onSideA ^ invertSideLogic ? +openAngle : -openAngle);

            targetY = yaw;
            isOpen = true;
            isMoving = true;

            if (audioSource && openSound) audioSource.PlayOneShot(openSound);
        }
        else
        {
            targetY = closedY;
            isOpen = false;
            isMoving = true;

            if (audioSource && closeSound) audioSource.PlayOneShot(closeSound);
        }
    }

    bool IsPlayerOnSideA(Vector3 playerWorldPos)
    {
        // Compute which side of the sideRef forward plane the player is on.
        // Side A = in front of sideRef.forward; Side B = behind.
        Vector3 toPlayer = playerWorldPos - sideRef.position;
        float side = Vector3.Dot(sideRef.forward, toPlayer);
        return side >= 0f;
    }

    void SetDoorYInstant(float yDegrees)
    {
        Vector3 e = doorTransform.localEulerAngles;
        e.y = yDegrees;
        doorTransform.localEulerAngles = e;
    }

    public void UnlockDoor() { isLocked = false; }
    public void LockDoor() { isLocked = true; }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!doorTransform) doorTransform = transform;
        if (!sideRef) sideRef = doorTransform;

        // Draw side plane axis
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(sideRef.position, sideRef.position + sideRef.forward * 1.0f);
        Gizmos.DrawWireSphere(sideRef.position + sideRef.forward * 1.0f, 0.05f);

        // Visualize open targets
        float closed = (Application.isPlaying ? closedY : doorTransform.localEulerAngles.y);
        Vector3 pivot = doorTransform.position;
        Vector3 axis = doorTransform.up;

        Quaternion qA = Quaternion.Euler(0f, closed + openAngle, 0f);
        Quaternion qB = Quaternion.Euler(0f, closed - openAngle, 0f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawRay(pivot, (qA * Vector3.forward) * 0.8f);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawRay(pivot, (qB * Vector3.forward) * 0.8f);
    }
#endif
}
