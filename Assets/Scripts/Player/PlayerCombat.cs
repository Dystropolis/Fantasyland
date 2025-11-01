using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Weapons")]
    public SwordWeapon swordWeapon;

    private WeaponBase currentWeapon;

    void Start()
    {
        EquipSword();
    }

    void Update()
    {
        HandleWeaponSwap();
        HandleInput();
        HandleBlockInput();
    }

    void HandleWeaponSwap()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipSword();
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipBow();
    }

    void HandleInput()
    {
        // Left click to attack
        if (Input.GetMouseButtonDown(0))
        {
            if (swordWeapon != null)
            {
                swordWeapon.TrySwordAttack();
            }
        }

        // We do NOT call DoHitCheck() here anymore.
        // Damage happens on the animation event calling PerformHit().
    }



    public void Animation_DoDamage()
    {
        if (swordWeapon != null && swordWeapon.gameObject.activeSelf)
            swordWeapon.DoHitCheck();
    }



    void HandleBlockInput()
    {
        if (currentWeapon == swordWeapon)
        {
            swordWeapon.blocking = Input.GetMouseButton(1);
        }
        else
        {
            swordWeapon.blocking = false;
        }
    }


    void EquipSword()
    {
        currentWeapon = swordWeapon;
        swordWeapon.gameObject.SetActive(true);
        Debug.Log("Equipped sword");
    }

    void EquipBow()
    {
        swordWeapon.gameObject.SetActive(false);
        Debug.Log("Equipped bow");
    }
}
