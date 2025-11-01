using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
    public ActorAI ai; // drag the root ActorAI (SkeletonEnemy root)

    void Awake()
    {
        if (ai == null)
        {
            ai = GetComponentInParent<ActorAI>();
        }
    }

    // Called by Animation Event at the strike frame
    public void DealDamageToPlayerEvent()
    {
        if (ai != null)
            ai.DealDamageToPlayerEvent();
    }

    // Called by Animation Event at the very end of the attack animation
    public void FinishAttack()
    {
        if (ai != null)
            ai.FinishAttack();
    }

    // Called by Animation Event on the 'TakeDamage' animation start
    public void PlayTakeDamage()
    {
        if (ai != null)
            ai.PlayTakeDamage();
    }
}
