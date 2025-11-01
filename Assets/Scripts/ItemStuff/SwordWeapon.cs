using UnityEngine;

public class SwordWeapon : WeaponBase
{
    [Header("Attack Settings")]
    public float swordAttackRange = 2f;
    public int swordAttackDamage = 25;
    public float swordAttackCooldown = 0.6f;   // renamed from attackCooldown
    [Header("Timing")]
    [Tooltip("Delay before the swing resolves when auto timing is enabled.")]
    public float attackImpactDelay = 0.25f;
    [Tooltip("Automatically resolve the hit after the delay instead of relying on an animation event.")]
    public bool autoResolveImpact = false;

    [Header("Blocking")]
    public bool blocking = false;
    public float damageReductionFactor = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip swingClip;
    public AudioClip hitFleshClip;
    public AudioClip hitStoneClip;
    public AudioClip hitMetalClip;
    public AudioClip blockClip;
    public AudioClip missClip;

    [Header("Hit Effects")]
    public GameObject defaultHitFX;
    public GameObject fleshHitFX;
    public GameObject stoneHitFX;
    public GameObject metalHitFX;

    [Header("Detection")]
    public LayerMask hitMask;

    private Animator animator;
    private float nextAttackTime = 0f;

    void Start()
    {
        animator = GetComponentInParent<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void TrySwordAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + swordAttackCooldown;

        if (animator != null)
            animator.SetTrigger("Attack");

        CancelInvoke(nameof(PerformDelayedImpact));

        if (autoResolveImpact)
        {
            Invoke(nameof(PerformDelayedImpact), attackImpactDelay);
        }
    }

    protected override void Attack() { }

    void PerformDelayedImpact()
    {
        DoHitCheck();
    }

    public void DoHitCheck()
    {
        if (audioSource && swingClip)
            audioSource.PlayOneShot(swingClip);

        RaycastHit hit;
        Camera cam = Camera.main;
        if (cam == null)
            cam = GetComponentInParent<Camera>();

        Vector3 origin;
        Vector3 dir;

        if (cam != null)
        {
            origin = cam.transform.position;
            dir = cam.transform.forward;
        }
        else
        {
            origin = transform.position;
            dir = transform.forward;
        }

        if (Physics.Raycast(origin, dir, out hit, swordAttackRange, hitMask))
        {
            ActorHealth health = hit.collider.GetComponentInParent<ActorHealth>();
            if (health != null)
            {
                health.TakeDamage(swordAttackDamage, gameObject);
                PlayHitEffect(hit, health);
                return;
            }

            PlaySurfaceHitEffect(hit);
        }
        else
        {
            if (audioSource && missClip)
                audioSource.PlayOneShot(missClip);
        }
    }

    void PlaySurfaceHitEffect(RaycastHit hit)
    {
        string tag = hit.collider.tag.ToLower();

        GameObject fx = defaultHitFX;
        AudioClip clip = hitStoneClip;

        if (tag.Contains("flesh"))
        {
            fx = fleshHitFX;
            clip = hitFleshClip;
        }
        else if (tag.Contains("metal"))
        {
            fx = metalHitFX;
            clip = hitMetalClip;
        }
        else if (tag.Contains("stone"))
        {
            fx = stoneHitFX;
            clip = hitStoneClip;
        }

        if (fx != null)
        {
            GameObject fxObj = Instantiate(fx, hit.point, Quaternion.LookRotation(-hit.normal));
            Destroy(fxObj, 2f);
        }

        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }

    void PlayHitEffect(RaycastHit hit, ActorHealth health)
    {
        if (fleshHitFX != null)
        {
            GameObject fxObj = Instantiate(fleshHitFX, hit.point, Quaternion.LookRotation(-hit.normal));
            Destroy(fxObj, 2f);
        }

        if (audioSource && hitFleshClip)
            audioSource.PlayOneShot(hitFleshClip);
    }

    public void StartBlock()
    {
        if (!blocking)
        {
            blocking = true;
            if (animator != null)
                animator.SetBool("Blocking", true);
        }
    }

    public void StopBlock()
    {
        if (blocking)
        {
            blocking = false;
            if (animator != null)
                animator.SetBool("Blocking", false);
        }
    }
}
