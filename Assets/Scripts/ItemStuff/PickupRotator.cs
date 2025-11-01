using UnityEngine;

public class PickupRotator : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeed = 90f;
    public bool rotateX = false;
    public bool rotateY = true;   // default to Y like before
    public bool rotateZ = false;

    [Header("Bobbing")]
    public float bobAmount = 0.2f;
    public float bobSpeed = 2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        Vector3 axis = new Vector3(
            rotateX ? 1f : 0f,
            rotateY ? 1f : 0f,
            rotateZ ? 1f : 0f
        );

        // Rotate only on the chosen axes
        transform.Rotate(axis * rotateSpeed * Time.deltaTime, Space.Self);

        // Bob up and down
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = startPos + Vector3.up * bob;
    }
}
