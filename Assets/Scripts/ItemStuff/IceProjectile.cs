using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IceProjectile : MonoBehaviour
{
    public float lifeTime = 6f;
    private float despawnTime;

    private Vector3 velocity;
    private int damage;

    public void Init(Vector3 direction, float speed, int dmg)
    {
        velocity = direction.normalized * speed;
        damage = dmg;
        despawnTime = Time.time + lifeTime;
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;

        if (Time.time >= despawnTime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // hit world
        Destroy(gameObject);
    }
}
