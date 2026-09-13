using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float maxAcceleration = 10f;

    protected Vector3 velocity;

    public float MaxSpeed => maxSpeed;
    public Vector3 Velocity => velocity;

    [Header("Steering")]
    [SerializeField] private SteeringBehaviourType behaviourType;

    private SteeringBehaviour steeringBehaviour;


    protected virtual void Awake()
    {
        steeringBehaviour = GetBehaviour(behaviourType);
    }

    protected virtual void Update()
    {
        if (steeringBehaviour == null)
        {
            return;
        }

        Vector3 steering = steeringBehaviour.CalculateSteering();

        Vector3 acceleration = Vector3.ClampMagnitude(steering, maxAcceleration);

        velocity += acceleration * Time.deltaTime;

        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        transform.position += velocity * Time.deltaTime;
    }

    private SteeringBehaviour GetBehaviour(SteeringBehaviourType type)
    {
        switch (type)
        {
            case SteeringBehaviourType.Seek:
                return GetComponent<Seek>();

            case SteeringBehaviourType.Arrive:
                return GetComponent<Arrive>();

            default:
                return null;
        }
    }

}
