using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float maxAcceleration = 10f;

    protected Vector3 velocity;
    protected Vector3 steering;

    protected virtual void Update()
    {
        Vector3 acceleration = Vector3.ClampMagnitude(steering, maxAcceleration);

        velocity += acceleration * Time.deltaTime;

        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        transform.position += velocity * Time.deltaTime;
    }
}
