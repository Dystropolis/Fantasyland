// Scripts/Systems/Cutscenes/CutsceneUse.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CutsceneUse : MonoBehaviour
{
    [Header("Interaction (matches Door style)")]
    public float interactDistance = 3f;
    [Tooltip("Layers the interact ray can hit. This object’s layer is auto-included at Start.")]
    public LayerMask interactMask = ~0;

    [Header("Cutscene")]
    [Tooltip("Empty transform that marks the cutscene camera’s START pose.")]
    public Transform cutsceneStartPose;

    private Camera playerCam;
    private int myLayerBit;

    void Start()
    {
        playerCam = Camera.main;

        // Ensure this object’s layer is included (like your door ensures its own layer is hit)
        int myLayer = gameObject.layer;
        myLayerBit = 1 << myLayer;
        interactMask |= myLayerBit;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && CanInteractByLook())
        {
            if (CutsceneManager.Instance)
            {
                CutsceneManager.Instance.BeginCutscene(cutsceneStartPose);
            }
        }
    }

    bool CanInteractByLook()
    {
        if (!playerCam) return false;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            // Accept if we or our children were hit (same acceptance logic as your door)
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) return true;
        }
        return false;
    }
}
