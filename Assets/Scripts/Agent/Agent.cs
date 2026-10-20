using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    [Header("References")]
    protected SteeringBehaviour currentBehaviour;

    public Vector3 Velocity => velocity;
    public float MaxSpeed => maxSpeed;
    public float MaxAcceleration => maxAcceleration;

    [Header("Stats")]
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float maxAcceleration = 10f;
    [SerializeField] protected float maxTurnSpeed = 360f;

    protected Vector3 velocity;

    private readonly RaycastHit[] movementHits = new RaycastHit[16];

    [Header("Collision")]
    [SerializeField] private float collisionSkin = 0.02f;

    [Header("Obstacle Avoidance")]
    [SerializeField] protected bool useObstacleAvoidance = false;
    [SerializeField] protected float avoidDistance = 3f;
    [SerializeField] protected float avoidAngle = 35f;
    [SerializeField] protected float avoidWeight = 6f;
    [SerializeField] protected float eyeHeight = 0.6f;
    [SerializeField] protected LayerMask obstacleMask;

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        Vector3 steering = Vector3.zero;

        if (currentBehaviour != null)
        {
            steering = currentBehaviour.CalculateSteering();
        }

        if (useObstacleAvoidance)
        {
            steering += AvoidObstacles();
        }

        Move(steering);
        ApplyBounds();
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

        Collider agentCollider = GetComponent<Collider>();

        Vector3 displacement = velocity * Time.deltaTime;
        float distance = displacement.magnitude;

        if (distance > 0.0001f &&
            agentCollider is CapsuleCollider capsule &&
            capsule.enabled)
        {
            Transform t = capsule.transform;

            Vector3 point1 = t.TransformPoint(
                capsule.center + Vector3.up *
                (capsule.height * 0.5f - capsule.radius)
            );

            Vector3 point2 = t.TransformPoint(
                capsule.center - Vector3.up *
                (capsule.height * 0.5f - capsule.radius)
            );

            float scale = Mathf.Max(
                t.lossyScale.x,
                t.lossyScale.z
            );

            float radius = capsule.radius * scale;

            if (Physics.CapsuleCast(
                point1,
                point2,
                radius,
                displacement.normalized,
                out RaycastHit hit,
                distance + collisionSkin,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                float allowedDistance = Mathf.Max(
                    0f,
                    hit.distance - collisionSkin
                );

                transform.position +=
                    displacement.normalized * allowedDistance;

                velocity = Vector3.zero;
            }
            else
            {
                transform.position += displacement;
            }
        }
        else
        {
            transform.position += displacement;
        }

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

    protected Vector3 AvoidObstacles()
    {
        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        Vector3 avoidance =
            Whisker(origin, transform.forward, 1f);

        avoidance += Whisker(
            origin,
            Quaternion.Euler(0f, avoidAngle, 0f)
                * transform.forward,
            0.5f
        );

        avoidance += Whisker(
            origin,
            Quaternion.Euler(0f, -avoidAngle, 0f)
                * transform.forward,
            0.5f
        );

        return Vector3.ClampMagnitude(
            avoidance,
            avoidWeight
        );
    }

    private Vector3 Whisker(
        Vector3 origin,
        Vector3 direction,
        float weight)
    {
        if (!Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            avoidDistance,
            obstacleMask))
        {
            return Vector3.zero;
        }

        if (hit.transform.IsChildOf(transform))
        {
            return Vector3.zero;
        }

        float proximity =
            1f - hit.distance / avoidDistance;

        Vector3 away =
            Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;

        Vector3 lateral =
            Vector3.ProjectOnPlane(away, transform.forward);

        if (lateral.sqrMagnitude < 0.0001f)
        {
            lateral = transform.right;
        }

        return lateral.normalized * proximity * weight;
    }

    protected void ApplyBounds()
    {
        if (Bounds.Instance != null)
        {
            Bounds.Instance.WrapPosition(transform);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!useObstacleAvoidance)
        {
            return;
        }

        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.magenta;

        Gizmos.DrawRay(
            origin,
            transform.forward * avoidDistance
        );

        Gizmos.DrawRay(
            origin,
            Quaternion.Euler(0f, avoidAngle, 0f)
                * transform.forward * avoidDistance
        );

        Gizmos.DrawRay(
            origin,
            Quaternion.Euler(0f, -avoidAngle, 0f)
                * transform.forward * avoidDistance
        );
    }
}