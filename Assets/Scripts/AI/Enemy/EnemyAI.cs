using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Sword, Bow, Ice }

    [Header("Setup")]
    public EnemyType enemyType = EnemyType.Sword;
    public Transform targetPlayer; // assign Player transform in Inspector

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float desiredRange = 8f; // used for bow/ice types
    public float chaseRange = 20f;  // will only engage if player within this

    private CharacterController controller;
    private EnemyAttack attack;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        attack = GetComponent<EnemyAttack>();
    }

    void Update()
    {
        if (targetPlayer == null) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        Vector3 toPlayer = (targetPlayer.position - transform.position);
        toPlayer.y = 0f;
        Vector3 dir = toPlayer.normalized;

        // face the player
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        if (dist <= chaseRange)
        {
            switch (enemyType)
            {
                case EnemyType.Sword:
                    // run straight at player
                    Move(dir);
                    break;

                case EnemyType.Bow:
                    // stay around desiredRange
                    if (dist > desiredRange)
                        Move(dir);
                    else if (dist < desiredRange * 0.7f)
                        Move(-dir); // back up
                    break;

                case EnemyType.Ice:
                    // similar to Bow but maybe wants longer range
                    if (dist > desiredRange + 4f)
                        Move(dir);
                    else if (dist < (desiredRange + 4f) * 0.7f)
                        Move(-dir);
                    break;
            }

            // try attacking
            if (attack != null)
            {
                attack.TryAttack(targetPlayer);
            }
        }
    }

    void Move(Vector3 direction)
    {
        Vector3 vel = direction * moveSpeed;
        vel.y += Physics.gravity.y;
        controller.Move(vel * Time.deltaTime);
    }
}
