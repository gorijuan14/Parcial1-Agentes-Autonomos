using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float maxAcceleration = 10f;

    protected Vector3 velocity;

    protected virtual void Update()
    {
        transform.position += velocity * Time.deltaTime;
    }
}
