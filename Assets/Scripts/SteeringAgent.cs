using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float maxAcceleration = 10f;
    [SerializeField] protected float maxTurnSpeed = 360f;
    [SerializeField] private Vector3 initialVelocity;

    protected Vector3 velocity;
    public Vector3 Velocity => velocity;

    public float MaxSpeed => maxSpeed;
    public float MaxAcceleration => maxAcceleration;


    protected virtual void Awake()
    {
        velocity = initialVelocity;
    }

    protected virtual void Update()
    {
        SteeringBehaviour steeringBehaviour = GetCurrentBehaviour();


        if (steeringBehaviour == null)
        {
            return;
        }

        Vector3 steering = steeringBehaviour.CalculateSteering();

        Vector3 acceleration = Vector3.ClampMagnitude(steering, maxAcceleration);

        acceleration.y = 0f;

        velocity += acceleration * Time.deltaTime;

        velocity.y = 0f;

        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                maxTurnSpeed * Time.deltaTime
            );
        }
    }

    private SteeringBehaviour GetBehaviour(SteeringBehaviourType type)
    {
        switch (type)
        {
            case SteeringBehaviourType.Seek:
                return GetComponent<Seek>();

            case SteeringBehaviourType.Arrive:
                return GetComponent<Arrive>();

            case SteeringBehaviourType.Evade:
                return GetComponent<Evade>();

            case SteeringBehaviourType.Flocking:
                return GetComponent<Flocking>();

            default:
                return null;
        }
    }

    private SteeringBehaviour GetCurrentBehaviour()
    {
        // Por ahora todos los agentes usan Flocking
        return GetComponent<Flocking>();
    }

}
