using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Economy")]
    public int gemCount = 0;

    [Header("References")]
    public PlayerCombat playerCombat;
    public SwordWeapon swordWeapon;
    public PlayerUI ui;
    public PlayerController controller;

    private bool isDead = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hurtClip;


    void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (ui != null)
        {
            ui.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        int finalDamage = amount;

        if (IsBlocking())
        {
            float reduceFactor = swordWeapon.damageReductionFactor;

            if (reduceFactor < 0f) reduceFactor = 0f;
            if (reduceFactor > 1f) reduceFactor = 1f;

            float scaled = amount * (1f - reduceFactor);
            finalDamage = Mathf.RoundToInt(scaled);
            if (finalDamage < 0) finalDamage = 0;
        }

        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log("Player took " + finalDamage + " damage. HP now " + currentHealth + "/" + maxHealth);

        // update health bar
        if (ui != null)
        {
            ui.UpdateHealthBar(currentHealth, maxHealth);
        }

        // if we're still alive, play hurt flash + sound
        if (currentHealth > 0)
        {
            if (ui != null)
                ui.PlayDamageFeedback();

            if (audioSource != null && hurtClip != null)
                audioSource.PlayOneShot(hurtClip);
        }
        else
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        Debug.Log("Player healed " + amount + ". HP now " + currentHealth + "/" + maxHealth);

        if (ui != null)
        {
            ui.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void AddGem(int amount)
    {
        if (isDead) return;

        gemCount += amount;
        Debug.Log("Picked up " + amount + " gem(s). Total gems: " + gemCount);
    }

    public bool SpendGems(int amount)
    {
        if (gemCount >= amount)
        {
            gemCount -= amount;
            Debug.Log("Spent " + amount + " gems. Remaining: " + gemCount);
            return true;
        }

        Debug.Log("Not enough gems. Need " + amount + " but only have " + gemCount);
        return false;
    }

    bool IsBlocking()
    {
        if (swordWeapon == null) return false;
        if (playerCombat == null) return false;

        bool swordIsActive = swordWeapon.gameObject.activeSelf;
        bool holdingBlock = swordWeapon.blocking;

        return swordIsActive && holdingBlock;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player died.");

        if (controller != null)
        {
            controller.canControl = false;
            controller.worldPaused = true;
        }

        if (ui != null)
        {
            ui.ShowDeathScreen();
        }
        else
        {
            Time.timeScale = 0f;
        }
    }
}
