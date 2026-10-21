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

    [Header("Navigation")]
    [SerializeField] protected AStarMovement aStarMovement;

    [Header("Obstacle Avoidance")]
    [SerializeField] protected bool useObstacleAvoidance = false;
    [SerializeField] protected float avoidDistance = 3f;
    [SerializeField] protected float avoidAngle = 35f;
    [SerializeField] protected float avoidWeight = 6f;
    [SerializeField] protected float eyeHeight = 0.6f;
    [SerializeField] protected LayerMask obstacleMask;

    protected virtual void Start()
    {
        if (aStarMovement == null)
        aStarMovement = GetComponent<AStarMovement>();
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
        Vector3 forward = velocity.sqrMagnitude > 0.1f
            ? velocity.normalized
            : transform.forward;

        forward.y = 0f;
        forward.Normalize();

        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        Vector3 leftDirection =
            Quaternion.Euler(0f, -avoidAngle, 0f) * forward;

        Vector3 rightDirection =
            Quaternion.Euler(0f, avoidAngle, 0f) * forward;

        bool frontBlocked = Physics.Raycast(
            origin, forward, out RaycastHit frontHit,
            avoidDistance, obstacleMask,
            QueryTriggerInteraction.Ignore);

        bool leftBlocked = Physics.Raycast(
            origin, leftDirection, out RaycastHit leftHit,
            avoidDistance, obstacleMask,
            QueryTriggerInteraction.Ignore);

        bool rightBlocked = Physics.Raycast(
            origin, rightDirection, out RaycastHit rightHit,
            avoidDistance, obstacleMask,
            QueryTriggerInteraction.Ignore);

        Vector3 avoidance = Vector3.zero;

        avoidance += Whisker(origin, forward, avoidWeight);

        if (!frontBlocked && !leftBlocked && !rightBlocked)
            return avoidance;

        // Preferir el lado que permita avanzar hacia el waypoint actual.
        Vector3 desiredDirection = forward;

        if (aStarMovement != null &&
            aStarMovement.CurrentTargetNode != null)
        {
            desiredDirection =
                aStarMovement.CurrentTargetNode.Position - transform.position;

            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude > 0.001f)
                desiredDirection.Normalize();
            else
                desiredDirection = forward;
        }

        float leftScore = Vector3.Dot(leftDirection, desiredDirection);
        float rightScore = Vector3.Dot(rightDirection, desiredDirection);

        if (leftBlocked)
            leftScore -= 2f;

        if (rightBlocked)
            rightScore -= 2f;

        Vector3 chosenDirection =
            leftScore >= rightScore ? leftDirection : rightDirection;

        // Si solo hay obstáculos laterales, no forzar un giro.
        if (!frontBlocked)
            return Vector3.zero;

        // Intensificar la evasión cuanto más cerca esté la pared.
        float proximity =
        1f - Mathf.Clamp01(frontHit.distance / avoidDistance);

        // La evasión aumenta cuando la pared está cerca.
        float urgency = Mathf.Lerp(1f, 2f, proximity);

        // Añadir una componente lateral fuerte para cambiar
        // la trayectoria antes de alcanzar la pared.
        Vector3 lateralDirection =
            Vector3.Cross(Vector3.up, forward).normalized;

        float lateralSign =
            Vector3.Dot(lateralDirection, chosenDirection) >= 0f
                ? 1f
                : -1f;

        Vector3 avoidanceDirection =
            (forward * 0.2f + lateralDirection * lateralSign).normalized;

        return avoidanceDirection * avoidWeight * urgency;
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
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            return Vector3.zero;
        }

        if (hit.transform.IsChildOf(transform))
            return Vector3.zero;

        float proximity =
            1f - Mathf.Clamp01(hit.distance / avoidDistance);

        Vector3 forward = velocity.sqrMagnitude > 0.1f
            ? velocity.normalized
            : transform.forward;

        forward.y = 0f;
        forward.Normalize();

        Vector3 lateral =
            Vector3.Cross(Vector3.up, forward).normalized;

        // Elegir el sentido que se aleja de la pared.
        if (Vector3.Dot(lateral, hit.normal) < 0f)
            lateral = -lateral;

        return lateral * proximity * weight;
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