using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the CameraHolder object (the thing that pitches up/down), NOT the Camera component itself.")]
    public Transform cameraTransform;

    private CharacterController controller;

    [Header("Move Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    private Vector3 lastPosition;
    private Vector3 frameVelocity;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Crouch Settings")]
    [Tooltip("CharacterController.height when standing.")]
    public float standingHeight = 2f;
    [Tooltip("CharacterController.height when crouched.")]
    public float crouchHeight = 1.2f;

    [Tooltip("Local Y of CameraHolder when standing.")]
    public float cameraStandY = 1.7f;
    [Tooltip("Local Y of CameraHolder when crouched.")]
    public float cameraCrouchY = 1.0f;

    [Tooltip("How fast we lerp crouch height/camera each frame.")]
    public float crouchLerpSpeed = 10f;

    [Header("Crouch Headroom Check")]
    [Tooltip("Layers to test against when trying to stand up. EXCLUDE the Player layer so you don't detect yourself.")]
    public LayerMask standCheckMask = ~0;

    [Header("Control Lock / Pause")]
    [Tooltip("If false, player can't move or look (e.g. during dialogue).")]
    public bool canControl = true;
    [Tooltip("If true, world is paused (like Time.timeScale = 0). Prevents movement update.")]
    public bool worldPaused = false;

    // internal runtime state
    public float camPitch = 0f;
    private float verticalVelocity = 0f;

    // crouch state
    [SerializeField] private bool isCrouching = false;

    // crouch lerp targets
    private float targetControllerHeight;
    private float targetControllerCenterY;
    private float targetCameraY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // initialize controller as standing
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight * 0.5f, 0f);

        // initialize crouch targets as standing
        targetControllerHeight = standingHeight;
        targetControllerCenterY = standingHeight * 0.5f;
        targetCameraY = cameraStandY;

        // put camera holder at standing height immediately
        if (cameraTransform != null)
        {
            Vector3 camLocal = cameraTransform.localPosition;
            camLocal.y = cameraStandY;
            cameraTransform.localPosition = camLocal;
        }
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // If the world is paused, don't simulate gameplay movement/aiming.
        // We still update frameVelocity at the end so stealth code doesn't explode.
        if (!worldPaused)
        {
            HandleLook();
            HandleMove();
        }

        // Track velocity each frame for stealth noise calcs.
        Vector3 delta = transform.position - lastPosition;
        frameVelocity = (Time.deltaTime > 0f) ? delta / Time.deltaTime : Vector3.zero;
        frameVelocity.y = 0f;
        lastPosition = transform.position;
    }

    // This is needed by PlayerStealth and AI hearing logic
    public Vector3 GetVelocityXZ()
    {
        return frameVelocity;
    }

    // Are we crouched?
    public bool IsCrouching => isCrouching;

    // Are we sprinting (Shift) right now?
    public bool IsSprinting
    {
        get
        {
            if (!canControl) return false;
            if (isCrouching) return false;
            if (worldPaused) return false;
            return Input.GetKey(KeyCode.LeftShift);
        }
    }

    // Flat ground speed (for debug / UI if you ever want it)
    public float CurrentMoveSpeed
    {
        get
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            return v.magnitude;
        }
    }

    // --------------------------
    // CAMERA LOOK
    // --------------------------
    void HandleLook()
    {
        if (!canControl) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // yaw = rotate the whole player on Y
        transform.Rotate(Vector3.up * mouseX);

        // pitch = rotate the camera holder locally
        camPitch -= mouseY;
        camPitch = Mathf.Clamp(camPitch, pitchMin, pitchMax);

        if (cameraTransform != null)
        {
            cameraTransform.localEulerAngles = new Vector3(camPitch, 0f, 0f);
        }
    }

    // --------------------------
    // MOVEMENT / CROUCH / JUMP / GRAVITY
    // --------------------------
    void HandleMove()
    {
        // Movement can still be blocked if dialogue is open etc.
        bool moveAllowed = canControl;

        // --- CROUCH TOGGLE ---
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (!isCrouching)
            {
                // Standing -> crouch
                isCrouching = true;
                targetControllerHeight = crouchHeight;
                targetControllerCenterY = crouchHeight * 0.5f;
                targetCameraY = cameraCrouchY;
            }
            else
            {
                // Crouched -> try to stand
                if (CanStandUp())
                {
                    isCrouching = false;
                    targetControllerHeight = standingHeight;
                    targetControllerCenterY = standingHeight * 0.5f;
                    targetCameraY = cameraStandY;
                }
            }
        }


        // Smoothly lerp controller height/center
        controller.height = Mathf.Lerp(controller.height, targetControllerHeight, Time.deltaTime * crouchLerpSpeed);

        Vector3 newCenter = controller.center;
        newCenter.y = Mathf.Lerp(controller.center.y, targetControllerCenterY, Time.deltaTime * crouchLerpSpeed);
        controller.center = newCenter;

        // Smoothly lerp camera Y
        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCameraY, Time.deltaTime * crouchLerpSpeed);
            cameraTransform.localPosition = camPos;
        }

        // choose speed based on crouch / sprint
        float chosenSpeed = walkSpeed;
        if (isCrouching)
        {
            chosenSpeed = crouchSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            chosenSpeed = sprintSpeed;
        }

        // gather input (if we're allowed to move)
        float inputX = moveAllowed ? Input.GetAxisRaw("Horizontal") : 0f;
        float inputZ = moveAllowed ? Input.GetAxisRaw("Vertical") : 0f;

        Vector3 move = (transform.right * inputX + transform.forward * inputZ).normalized;
        move *= chosenSpeed;

        // --- GRAVITY + JUMP ---
        if (controller.isGrounded)
        {
            verticalVelocity = -1f;

            if (moveAllowed && Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    public void SnapCameraUpright()
    {
        if (!cameraTransform) return;
        // read current local X to keep pitch continuity, zero Y/Z roll on the camera holder
        Vector3 e = cameraTransform.localEulerAngles;
        float x = (e.x > 180f) ? e.x - 360f : e.x;
        camPitch = Mathf.Clamp(x, pitchMin, pitchMax);
        cameraTransform.localEulerAngles = new Vector3(camPitch, 0f, 0f);
    }

    // --------------------------
    // STAND-UP CLEARANCE CHECK
    // --------------------------
    bool CanStandUp()
    {
        // Predict capsule if standing
        float standHeight = standingHeight;
        float standRadius = controller.radius;

        // where the standing CC center would be in WORLD
        Vector3 worldCenterStanding = transform.position + new Vector3(
            0f,
            standHeight * 0.5f,
            0f
        );

        float halfHeightStanding = standHeight * 0.5f;

        Vector3 bottom = worldCenterStanding + Vector3.down * (halfHeightStanding - standRadius);
        Vector3 top = worldCenterStanding + Vector3.up * (halfHeightStanding - standRadius);

        Collider[] hits = Physics.OverlapCapsule(
            bottom,
            top,
            standRadius,
            standCheckMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == controller) continue;
            if (hits[i].transform == this.transform) continue;

            // anything else means blocked
            return false;
        }

        return true;
    }

    // --------------------------
    // PUBLIC HELPERS FOR OTHER SYSTEMS
    // --------------------------
    public void DisableControlForDialogue()
    {
        canControl = false;
        worldPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnableControlAfterDialogue()
    {
        canControl = true;
        worldPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
