using UnityEngine;
using UnityEngine.UI;

public class CrosshairTarget : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UI Image or crosshair element to recolor.")]
    public Image crosshairImage;

    [Tooltip("Camera we raycast from (usually the player's camera).")]
    public Camera playerCamera;

    [Header("Detection")]
    [Tooltip("How far ahead we check for targets.")]
    public float checkRange = 10f;

    [Tooltip("Layers to consider as 'targetable' (characters, enemies, NPCs).")]
    public LayerMask checkMask = ~0;

    [Header("Colors")]
    public Color neutralColor = Color.white;
    public Color enemyColor = Color.red;
    public Color friendlyColor = Color.green;

    [Header("Debug (read-only)")]
    [SerializeField] private string state = "neutral";
    [SerializeField] private ActorAI lastActorHit;

    void Update()
    {
        if (crosshairImage == null || playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, checkRange, checkMask, QueryTriggerInteraction.Ignore))
        {
            // Try to find an ActorAI on what we hit
            ActorAI ai = hit.collider.GetComponent<ActorAI>();
            if (ai == null)
            {
                // not directly on the collider? Maybe collider is on a child (like a ragdoll bone).
                ai = hit.collider.GetComponentInParent<ActorAI>();
            }

            lastActorHit = ai;

            if (ai != null)
            {
                // We found something that has faction info
                switch (ai.faction)
                {
                    case ActorAI.Faction.Enemy:
                        crosshairImage.color = enemyColor;
                        state = "enemy";
                        break;

                    case ActorAI.Faction.Friendly:
                        crosshairImage.color = friendlyColor;
                        state = "friendly";
                        break;

                    default:
                        crosshairImage.color = neutralColor;
                        state = "neutral";
                        break;
                }
            }
            else
            {
                // Hit something that is not an ActorAI at all (wall, crate, etc.)
                crosshairImage.color = neutralColor;
                state = "neutral";
            }
        }
        else
        {
            // No hit in range
            crosshairImage.color = neutralColor;
            state = "neutral";
            lastActorHit = null;
        }
    }
}
