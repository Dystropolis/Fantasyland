using UnityEngine;

public class SwordWeapon : WeaponBase
{
    [Header("Attack Settings")]
    public float swordAttackRange = 2f;
    public int swordAttackDamage = 25;
    public float swordAttackCooldown = 0.6f;   // renamed from attackCooldown

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

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TrySwordAttack();

        if (Input.GetMouseButton(1))
            StartBlock();
        else
            StopBlock();
    }

    public void TrySwordAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + swordAttackCooldown;

        if (animator != null)
            animator.SetTrigger("Attack");

        if (audioSource && swingClip)
            audioSource.PlayOneShot(swingClip);

        Invoke(nameof(DoHitCheck), 0.25f);
    }

    protected override void Attack() { }

    public void DoHitCheck()
    {
        RaycastHit hit;
        Vector3 origin = Camera.main.transform.position;
        Vector3 dir = Camera.main.transform.forward;

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

    void StartBlock()
    {
        if (!blocking)
        {
            blocking = true;
            if (animator != null)
                animator.SetBool("Blocking", true);
        }
    }

    void StopBlock()
    {
        if (blocking)
        {
            blocking = false;
            if (animator != null)
                animator.SetBool("Blocking", false);
        }
    }
}
