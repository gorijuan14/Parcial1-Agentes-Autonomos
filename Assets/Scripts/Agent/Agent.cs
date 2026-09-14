using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float maxAcceleration = 10f;
    [SerializeField] protected float maxTurnSpeed = 360f;
    [SerializeField] protected Vector3 initialVelocity;

    protected Vector3 velocity;
    protected SteeringBehaviour currentBehaviour;

    public Vector3 Velocity => velocity;
    public float MaxSpeed => maxSpeed;
    public float MaxAcceleration => maxAcceleration;

    protected virtual void Start()
    {
        velocity = initialVelocity;
    }

    protected virtual void Update()
    {
        if (currentBehaviour == null)
        {
            return;
        }

        Move(currentBehaviour.CalculateSteering());
    }

    public void SetBehaviour(SteeringBehaviour behaviour)
    {
        currentBehaviour = behaviour;
    }

    public void StopMovement()
    {
        velocity = Vector3.zero;
    }

    protected void Move(Vector3 steering)
    {
        Vector3 acceleration = Vector3.ClampMagnitude(
            steering,
            maxAcceleration
        );

        acceleration.y = 0f;

        velocity += acceleration * Time.deltaTime;
        velocity.y = 0f;

        velocity = Vector3.ClampMagnitude(
            velocity,
            maxSpeed
        );

        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(velocity);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                maxTurnSpeed * Time.deltaTime
            );
        }
    }
}