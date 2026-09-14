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

    private SteeringBehaviour currentBehaviour;

    protected virtual void Awake()
    {
        velocity = initialVelocity;
    }

    protected virtual void Update()
    {
        Vector3 steering;

        if (currentBehaviour != null)
        {
            steering = currentBehaviour.CalculateSteering();
        }
        else
        {
            steering = CalculatePatrolAndFlocking();
        }

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

    private Vector3 CalculatePatrolAndFlocking()
    {
        Patrol patrol = GetComponent<Patrol>();
        Flocking flocking = GetComponent<Flocking>();

        Vector3 steering = Vector3.zero;

        if (patrol != null)
        {
            steering += patrol.CalculateSteering();
        }

        if (flocking != null)
        {
            steering += flocking.CalculateSteering();
        }

        return steering;
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

            case SteeringBehaviourType.Pursue:
                return GetComponent<Pursue>();

            case SteeringBehaviourType.Flocking:
                return GetComponent<Flocking>();

            default:
                return null;
        }
    }



    public void SetBehaviour(SteeringBehaviour behaviour)
    {
        currentBehaviour = behaviour;
    }

    private SteeringBehaviour GetCurrentBehaviour()
    {
        Pursue pursue = GetComponent<Pursue>();

        if (pursue != null)
        {
            return pursue;
        }

        return null;
    }

}
