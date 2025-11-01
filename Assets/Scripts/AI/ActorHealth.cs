using UnityEngine;

public class ActorHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Faction / Behavior")]
    public ActorAI ai; // assign same ActorAI on this enemy
    [Tooltip("If true, this NPC can turn hostile when hurt.")]
    public bool canTurnHostile = false;

    [Tooltip("If currentHealth <= this after taking damage, flip hostile toward the PLAYER.")]
    public float hostileHealthThreshold = 25f;

    [Header("Death / Loot")]
    [Tooltip("Ragdoll prefab to spawn on death (already posed ragdoll version of this enemy).")]
    public GameObject ragdollPrefab;

    [Tooltip("Sound to play at the moment of death.")]
    public AudioClip deathClip;

    [Tooltip("Optional: link the same EnemyDropTable component on this enemy.")]
    public EnemyDropTable dropTable; // uses SpawnDrop()  :contentReference[oaicite:1]{index=1}

    private bool isDead = false;

    void Awake()
    {
        if (ai == null)
            ai = GetComponent<ActorAI>();

        if (dropTable == null)
            dropTable = GetComponent<EnemyDropTable>();

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    // source = GameObject that dealt the damage (e.g. player's sword)
    public void TakeDamage(float amount, GameObject source)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;

        // Trigger flinch if we survived
        if (ai != null && currentHealth > 0f)
        {
            ai.PlayTakeDamage();
        }

        // If this was the player and we dipped below threshold, turn hostile
        MaybeFlipHostileTowardPlayer(source);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void MaybeFlipHostileTowardPlayer(GameObject source)
    {
        if (!canTurnHostile) return;
        if (ai == null) return;
        if (ai.faction == ActorAI.Faction.Enemy) return;
        if (currentHealth > hostileHealthThreshold) return;
        if (source == null) return;

        // Only break friendship if player hurt us
        PlayerController pc = source.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        ai.BecomeHostileToPlayer(pc.transform);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Vector3 deathPos = transform.position;
        Quaternion deathRot = transform.rotation;

        // 1. Loot drop
        if (dropTable != null)
        {
            dropTable.SpawnDrop(deathPos);
        }

        // 2. Spawn ragdoll
        if (ragdollPrefab != null)
        {
            GameObject rag = Instantiate(ragdollPrefab, deathPos, deathRot);

            // 3. Death sound:
            // easiest: add a temp AudioSource to ragdoll so sound lives after we Destroy(this)
            if (deathClip != null)
            {
                AudioSource tempSource = rag.AddComponent<AudioSource>();
                tempSource.spatialBlend = 1f;        // 3D
                tempSource.playOnAwake = false;
                tempSource.loop = false;
                tempSource.clip = deathClip;
                tempSource.Play();
                // no need to clean it up manually, ragdoll can time out later or just sit there
            }
        }
        else
        {
            // fallback: play it here if no ragdoll prefab
            if (deathClip != null)
            {
                AudioSource oneShot = gameObject.AddComponent<AudioSource>();
                oneShot.spatialBlend = 1f;
                oneShot.playOnAwake = false;
                oneShot.loop = false;
                oneShot.PlayOneShot(deathClip);
            }
        }

        // 4. Kill the original enemy object
        Destroy(gameObject);
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }
}
