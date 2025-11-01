using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum PickupType { Health, Gem }

    public PickupType pickupType = PickupType.Health;
    public int amount = 20; // heal amount or gem amount

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        switch (pickupType)
        {
            case PickupType.Health:
                ph.Heal(amount);
                break;
            case PickupType.Gem:
                ph.AddGem(amount);
                break;
        }

        Destroy(gameObject);
    }
}
