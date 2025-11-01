// Scripts/Systems/Cutscenes/CutsceneManager.cs
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("Scene References")]
    public GameObject playerRoot;        // your Player root (tagged Player)
    public Camera playerCamera;          // PlayerCamera
    public Camera cutsceneCamera;        // Dedicated cutscene camera (disabled by default)

    private PlayerController playerController;
    private CharacterController charController;

    // Saved pose BEFORE we align player to the cutscene start
    private Vector3 savedPlayerPos;
    private Quaternion savedPlayerRot;

    // For convenience if you want to restore the player's camera pitch cleanly
    private float savedTimeScale = 1f;
    private bool isActive;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (playerRoot)
        {
            playerController = playerRoot.GetComponent<PlayerController>();
            charController = playerRoot.GetComponent<CharacterController>();
        }

        if (cutsceneCamera) cutsceneCamera.gameObject.SetActive(false);
    }

    /// <summary>
    /// Call to start a cutscene. You provide a Transform that represents the *start pose* of the cutscene camera.
    /// </summary>
    public void BeginCutscene(Transform cutsceneStartPose)
    {
        if (isActive || !playerRoot || !playerCamera || !cutsceneCamera || !cutsceneStartPose) return;
        isActive = true;

        // Save original player pose (BEFORE we snap them to the cutscene pose)
        savedPlayerPos = playerRoot.transform.position;
        savedPlayerRot = playerRoot.transform.rotation;

        // Optional: save timescale if you do pausing tricks elsewhere
        savedTimeScale = Time.timeScale;

        // 1) Move player so their camera exactly matches the cutscene start pose (your requirement)
        AlignPlayerRootToCameraPose(cutsceneStartPose);

        // 2) Disable player control & camera, enable cutscene cam
        if (playerController)
        {
            playerController.canControl = false;
            playerController.worldPaused = true; // prevents your movement loop while scenes play
        }
        if (charController) charController.enabled = false;

        playerCamera.gameObject.SetActive(false);

        cutsceneCamera.transform.SetPositionAndRotation(cutsceneStartPose.position, cutsceneStartPose.rotation);
        cutsceneCamera.gameObject.SetActive(true);
    }

    /// <summary>
    /// Animation Event helper: move cutscene cam to the player's original (pre-cutscene) pose,
    /// so the final swap back is seamless.
    /// </summary>
    public void Event_MoveCutsceneToSavedPlayerPose()
    {
        if (!cutsceneCamera) return;
        cutsceneCamera.transform.SetPositionAndRotation(savedPlayerPos, savedPlayerRot);
    }

    /// <summary>
    /// Animation Event helper: End the cutscene now (re-enable player, swap cameras).
    /// </summary>
    public void Event_EndCutscene()
    {
        EndCutscene();
    }

    /// <summary>
    /// Animation Event helper: Enable any object (parameter type must be UnityEngine.Object in the Animation Event).
    /// </summary>
    public void Event_EnableObject(UnityEngine.Object obj)
    {
        if (!obj) return;
        if (obj is GameObject go) go.SetActive(true);
        else if (obj is Component c) c.gameObject.SetActive(true);
    }

    /// <summary>
    /// Animation Event helper: Disable any object (parameter type must be UnityEngine.Object in the Animation Event).
    /// </summary>
    public void Event_DisableObject(UnityEngine.Object obj)
    {
        if (!obj) return;
        if (obj is GameObject go) go.SetActive(false);
        else if (obj is Component c) c.gameObject.SetActive(false);
    }

    /// <summary>
    /// Animation Event helper: Enable by index from a provided list on a relay (see CutsceneEventRelay).
    /// Included here in case you prefer calling the manager from the event.
    /// </summary>
    public void Event_EnableByIndex(CutsceneEventRelay relay, int index)
    {
        if (!relay || relay.objectsToToggle == null) return;
        if (index < 0 || index >= relay.objectsToToggle.Length) return;

        var go = relay.objectsToToggle[index];
        if (go) go.SetActive(true);
    }

    public void Event_DisableByIndex(CutsceneEventRelay relay, int index)
    {
        if (!relay || relay.objectsToToggle == null) return;
        if (index < 0 || index >= relay.objectsToToggle.Length) return;

        var go = relay.objectsToToggle[index];
        if (go) go.SetActive(false);
    }

    public void EndCutscene()
    {
        if (!isActive) return;
        isActive = false;

        // 1) Disable cutscene camera
        if (cutsceneCamera) cutsceneCamera.gameObject.SetActive(false);

        // 2) Restore player to their original pose (BEFORE we aligned them)
        playerRoot.transform.SetPositionAndRotation(savedPlayerPos, savedPlayerRot);

        // 3) Re-enable player controller/camera
        if (charController) charController.enabled = true;

        if (playerController)
        {
            playerController.worldPaused = false;
            playerController.canControl = true;

            // Belt & suspenders after any camera roll during the cutscene
            try { playerController.SnapCameraUpright(); } catch { }
        }

        playerCamera.gameObject.SetActive(true);

        // Restore timescale if you ever tweaked it
        Time.timeScale = savedTimeScale;
    }

    private void AlignPlayerRootToCameraPose(Transform targetCamPose)
    {
        // Put the entire player at the exact pose of the cutscene camera start
        playerRoot.transform.SetPositionAndRotation(targetCamPose.position, targetCamPose.rotation);

        // If your PlayerController manages pitch separately, you could also set cam pitch here;
        // but since we’re disabling control immediately after, it’s not strictly needed.
    }
}
