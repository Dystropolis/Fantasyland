using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController controller;
    public PlayerController playerController; // assign in Inspector
    public Transform footRayOrigin;           // something around player body
    public AudioSource audioSource;

    [Header("Timing")]
    public float walkInterval = 0.5f;
    public float sprintInterval = 0.32f;
    public float crouchInterval = 0.7f;
    public float footstepRayDistance = 2f;

    private float nextStepTime;

    [Header("Clips by surface")]
    public AudioClip[] stepStone;
    public AudioClip[] stepWood;
    public AudioClip[] stepMetal;
    public AudioClip[] stepDirt;
    public AudioClip[] stepDefault;

    void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        // Player footsteps are first-person sounds, so keep them non-3D
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        // 1. Are we allowed to play footsteps right now?
        if (playerController == null) return;
        if (playerController.worldPaused) return;
        if (!playerController.canControl) return;

        // grounded + actually moving horizontally
        Vector3 horizVel = controller.velocity;
        horizVel.y = 0f;
        bool grounded = controller.isGrounded;
        bool isMoving = grounded && horizVel.magnitude > 0.1f;

        if (!isMoving)
        {
            return;
        }

        // 2. Pick which interval to use right now
        float currentInterval;

        if (playerController.IsCrouching)
        {
            currentInterval = crouchInterval;
        }
        else if (playerController.IsSprinting)
        {
            currentInterval = sprintInterval;
        }
        else
        {
            currentInterval = walkInterval;
        }

        // 3. Only fire a new step if we've passed the timer
        if (Time.time >= nextStepTime)
        {
            nextStepTime = Time.time + currentInterval;
            PlayFootstep();
        }
    }

    void PlayFootstep()
    {
        // Determine surface under player
        HitSurface.SurfaceType surfaceType = HitSurface.SurfaceType.Default;

        Ray ray = new Ray(footRayOrigin.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, footstepRayDistance))
        {
            // Prefer explicit component
            HitSurface surf = hit.collider.GetComponentInParent<HitSurface>();
            if (surf != null)
            {
                surfaceType = surf.surfaceType;
            }
            else
            {
                // Fallback by tag if you didn't add HitSurface to this ground
                Collider c = hit.collider;
                if (c.CompareTag("Metal")) surfaceType = HitSurface.SurfaceType.Metal;
                else if (c.CompareTag("Stone")) surfaceType = HitSurface.SurfaceType.Stone;
                else if (c.CompareTag("Wood")) surfaceType = HitSurface.SurfaceType.Wood;
                else if (c.CompareTag("Dirt")) surfaceType = HitSurface.SurfaceType.Dirt;
                else surfaceType = HitSurface.SurfaceType.Default;
            }
        }

        // Pick SFX bank based on that surface
        AudioClip[] bank = stepDefault;

        switch (surfaceType)
        {
            case HitSurface.SurfaceType.Metal:
                bank = stepMetal;
                break;
            case HitSurface.SurfaceType.Stone:
                bank = stepStone;
                break;
            case HitSurface.SurfaceType.Wood:
                bank = stepWood;
                break;
            case HitSurface.SurfaceType.Dirt:
                bank = stepDirt;
                break;
            default:
                bank = stepDefault;
                break;
        }

        if (bank != null && bank.Length > 0 && audioSource != null)
        {
            int i = Random.Range(0, bank.Length);
            audioSource.PlayOneShot(bank[i]);
        }
    }
}
