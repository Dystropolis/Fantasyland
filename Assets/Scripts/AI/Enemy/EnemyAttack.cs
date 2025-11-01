using UnityEngine;

// This is now just a thin helper that lets animation/AI call a unified TryAttack()
// We removed enemyType / ranged logic.

[RequireComponent(typeof(ActorAI))]
public class EnemyAttack : MonoBehaviour
{
    private ActorAI ai;

    void Awake()
    {
        ai = GetComponent<ActorAI>();
    }

    // Player-facing attack request.
    // ActorAI already handles melee cooldown, distance checks, etc.
    // We keep this here in case something else in the project calls EnemyAttack.TryAttack()
    public void TryAttack(Transform targetPlayer)
    {
        // We no longer do logic here.
        // All real attack state, cooldown, and animation triggering happens in ActorAI.DoAttack().
        // This exists just to avoid null refs / compile errors from legacy calls.
        // So: nothing is required here anymore.
    }

    // Called by animation event via EnemyAnimationRelay when the swing should deal damage.
    // We forward that to ActorAI so we don't duplicate logic.
    public void DealDamageNow()
    {
        if (ai != null)
        {
            ai.DealDamageToPlayerEvent();
        }
    }
}
