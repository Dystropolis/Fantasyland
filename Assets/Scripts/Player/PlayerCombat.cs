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
        if (Input.GetMouseButtonDown(0) && currentWeapon == swordWeapon && swordWeapon != null)
        {
            swordWeapon.TrySwordAttack();
        }
    }



    public void Animation_DoDamage()
    {
        if (swordWeapon != null && swordWeapon.gameObject.activeSelf)
            swordWeapon.DoHitCheck();
    }



    void HandleBlockInput()
    {
        if (swordWeapon == null)
            return;

        if (currentWeapon == swordWeapon)
        {
            if (Input.GetMouseButton(1))
                swordWeapon.StartBlock();
            else
                swordWeapon.StopBlock();
        }
        else
        {
            swordWeapon.StopBlock();
        }
    }


    void EquipSword()
    {
        currentWeapon = swordWeapon;
        swordWeapon.StopBlock();
        swordWeapon.gameObject.SetActive(true);
        Debug.Log("Equipped sword");
    }

    void EquipBow()
    {
        currentWeapon = null;
        swordWeapon.StopBlock();
        swordWeapon.gameObject.SetActive(false);
        Debug.Log("Equipped bow");
    }
}
