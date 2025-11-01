using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Base Weapon Settings")]
    public float attackCooldown = 0.5f; // <— base shared cooldown

    // Every weapon must implement its Attack()
    protected abstract void Attack();
}
