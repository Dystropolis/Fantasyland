using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerUI : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider;
    public Text healthText;

    [Header("Damage Feedback")]
    [Tooltip("UI Image that covers the screen with a red tint when hurt.")]
    public Image damageFlashImage;

    [Tooltip("How strong the flash alpha should be at the moment of impact (0-1).")]
    public float flashMaxAlpha = 0.4f;

    [Tooltip("How fast the flash fades back to 0.")]
    public float flashFadeSpeed = 4f;

    private float currentFlashAlpha = 0f;

    [Header("Hurt Audio")]
    [Tooltip("AudioSource used for player hurt sounds. Should be 2D (spatialBlend = 0).")]
    public AudioSource hurtAudioSource;

    [Tooltip("Sound to play when player takes damage.")]
    public AudioClip hurtClip;

    [Header("Death Screen")]
    public GameObject deathScreenRoot;
    public Text deathMessageText;

    [Header("Refs")]
    public PlayerHealth playerHealth;
    public PlayerController playerController;

    void Start()
    {
        if (deathScreenRoot != null)
            deathScreenRoot.SetActive(false);

        if (playerHealth != null)
        {
            UpdateHealthBar(playerHealth.currentHealth, playerHealth.maxHealth);
        }

        // make sure flash starts invisible
        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
        }
    }

    void Update()
    {
        // Handle fading the red flash every frame
        if (damageFlashImage != null && currentFlashAlpha > 0f)
        {
            currentFlashAlpha -= flashFadeSpeed * Time.unscaledDeltaTime;
            if (currentFlashAlpha < 0f) currentFlashAlpha = 0f;

            Color c = damageFlashImage.color;
            c.a = currentFlashAlpha;
            damageFlashImage.color = c;
        }
    }

    // Called by PlayerHealth whenever HP changes
    public void UpdateHealthBar(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (healthText != null)
        {
            healthText.text = current + "";
        }
    }

    // NEW: called by PlayerHealth when the player gets hit but is still alive
    public void PlayDamageFeedback()
    {
        // flash
        if (damageFlashImage != null)
        {
            currentFlashAlpha = flashMaxAlpha;

            Color c = damageFlashImage.color;
            c.a = currentFlashAlpha;
            damageFlashImage.color = c;
        }

        // audio
        if (hurtAudioSource != null && hurtClip != null)
        {
            hurtAudioSource.PlayOneShot(hurtClip);
        }
    }

    // Called by PlayerHealth when the player dies
    public void ShowDeathScreen()
    {
        if (playerController != null)
        {
            playerController.canControl = false;
            playerController.worldPaused = true;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (deathScreenRoot != null)
            deathScreenRoot.SetActive(true);

        if (deathMessageText != null)
            deathMessageText.text = "You Died";
    }

    // ✅ New/Confirmed Restart Button
    public void RestartLevel()
    {
        // unpause game
        Time.timeScale = 1f;

        // reload the active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
