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

    private readonly RaycastHit[] movementHits = new RaycastHit[32];
    private CapsuleCollider bodyCollider;
    private float avoidanceSide = 1f;

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
        InitializeAgentReferences();
    }

    protected void InitializeAgentReferences()
    {
        bodyCollider = GetComponent<CapsuleCollider>();
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

        Move(steering);
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
        if (useObstacleAvoidance)
            steering = AvoidObstacles(steering);

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
        velocity = ConstrainVelocity(velocity);

        Vector3 displacement = velocity * Time.deltaTime;
        // Resolve up to three surfaces so a second wall cannot be crossed at a corner.
        for (int i = 0; i < 3 && displacement.sqrMagnitude > 0.00000001f; i++)
        {
            float distance = displacement.magnitude;
            Vector3 direction = displacement / distance;
            if (!CastBody(direction, distance + collisionSkin, out RaycastHit hit))
            {
                transform.position += displacement;
                break;
            }

            float allowedDistance = Mathf.Clamp(hit.distance - collisionSkin, 0f, distance);
            transform.position += direction * allowedDistance;

            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.001f)
            {
                velocity = Vector3.zero;
                break;
            }

            normal.Normalize();
            displacement -= direction * allowedDistance;
            // Remove only motion into the wall, preserving motion along it.
            displacement -= normal * Mathf.Min(0f, Vector3.Dot(displacement, normal));
            velocity -= normal * Mathf.Min(0f, Vector3.Dot(velocity, normal));
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

    public bool CanMoveDirectlyTo(Vector3 destination)
    {
        return CanTraverseSegment(transform.position, destination);
    }

    protected virtual Vector3 ConstrainVelocity(Vector3 proposed) => proposed;

    public virtual bool CanUseNode(GraphNode node) => node != null;

    public virtual bool CanTraverseSegment(Vector3 from, Vector3 to)
    {
        Vector3 offset = to - from;
        offset.y = 0f;
        return offset.sqrMagnitude < 0.0001f ||
            !CastBody(offset.normalized, offset.magnitude, out _, from - transform.position);
    }

    private bool CastBody(Vector3 direction, float distance, out RaycastHit nearestHit,
        Vector3 originOffset = default)
    {
        nearestHit = default;
        if (bodyCollider == null || !bodyCollider.enabled)
            return false;

        Transform body = bodyCollider.transform;
        Vector3 scale = body.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        int axis = bodyCollider.direction;
        Vector3 localAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
        float radiusScale = axis == 0 ? Mathf.Max(scale.y, scale.z)
            : axis == 1 ? Mathf.Max(scale.x, scale.z) : Mathf.Max(scale.x, scale.y);
        float radius = bodyCollider.radius * radiusScale;
        float halfSegment = Mathf.Max(0f, bodyCollider.height * scale[axis] * 0.5f - radius);
        Vector3 center = body.TransformPoint(bodyCollider.center) + originOffset;
        Vector3 offset = body.TransformDirection(localAxis) * halfSegment;

        int count = Physics.CapsuleCastNonAlloc(
            center + offset, center - offset, radius, direction, movementHits,
            distance, obstacleMask, QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = movementHits[i];
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearestDistance)
                continue;

            // A touching side wall must not block motion parallel to or away from it.
            if (Vector3.Dot(direction, hit.normal) >= -0.0001f)
                continue;

            nearestHit = hit;
            nearestDistance = hit.distance;
        }

        return nearestDistance < float.PositiveInfinity;
    }

    protected Vector3 AvoidObstacles(Vector3 steering)
    {
        Vector3 intendedVelocity = velocity + Vector3.ClampMagnitude(steering, maxAcceleration) * Time.deltaTime;
        intendedVelocity.y = 0f;
        if (intendedVelocity.sqrMagnitude < 0.0001f)
            return steering;

        Vector3 forward = intendedVelocity.normalized;
        float brakingDistance = velocity.sqrMagnitude / (2f * Mathf.Max(0.01f, maxAcceleration));
        float lookAhead = Mathf.Max(avoidDistance, brakingDistance + velocity.magnitude * Time.deltaTime + collisionSkin);
        // A wall beyond the arrival target must not prevent reaching that target.
        // The movement capsule sweep still prevents contact or penetration.
        if (currentBehaviour is Arrive arrive && arrive.TryGetTargetPosition(out Vector3 targetPosition))
        {
            Vector3 targetOffset = targetPosition - transform.position;
            targetOffset.y = 0f;
            lookAhead = Mathf.Min(lookAhead, targetOffset.magnitude);
        }
        if (lookAhead <= 0.0001f)
            return steering;
        if (!CastBody(forward, lookAhead, out RaycastHit frontHit))
            return steering;

        Vector3 desiredDirection = forward;
        if (aStarMovement != null && aStarMovement.CurrentTargetNode != null)
        {
            desiredDirection = aStarMovement.CurrentTargetNode.Position - transform.position;
            desiredDirection.y = 0f;
            desiredDirection.Normalize();
        }

        Vector3 bestDirection = Vector3.zero;
        float bestClearance = 0f;
        float bestScore = float.NegativeInfinity;
        // Once there is enough room to brake, favor progress over distant open space.
        // Otherwise an open corridor behind us can outweigh the narrow route ahead.
        float scoringDistance = Mathf.Min(lookAhead, Mathf.Max(1f, brakingDistance + 0.5f));
        ScoreAvoidanceDirection(forward, forward, desiredDirection, lookAhead, scoringDistance,
            ref bestDirection, ref bestClearance, ref bestScore);

        Vector3 wallNormal = frontHit.normal;
        wallNormal.y = 0f;
        if (wallNormal.sqrMagnitude > 0.001f)
        {
            // Follow the wall exactly: a 35-degree minimum turn cannot fit a tight passage.
            Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
            ScoreAvoidanceDirection(tangent, forward, desiredDirection, lookAhead, scoringDistance,
                ref bestDirection, ref bestClearance, ref bestScore);
            ScoreAvoidanceDirection(-tangent, forward, desiredDirection, lookAhead, scoringDistance,
                ref bestDirection, ref bestClearance, ref bestScore);
        }

        for (int side = -1; side <= 1; side += 2)
        {
            for (int step = 1; step <= 2; step++)
            {
                Vector3 candidate = Quaternion.Euler(0f, 5f * step * side, 0f) * forward;
                ScoreAvoidanceDirection(candidate, forward, desiredDirection, lookAhead, scoringDistance,
                    ref bestDirection, ref bestClearance, ref bestScore);
            }
        }

        // Check both sides, including turns wide enough to escape a corner.
        for (int step = 1; step <= 4; step++)
        {
            float angle = step == 4 ? 180f : Mathf.Min(150f, Mathf.Max(30f, avoidAngle) * step);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 candidate = Quaternion.Euler(0f, angle * side, 0f) * forward;
                ScoreAvoidanceDirection(candidate, forward, desiredDirection, lookAhead, scoringDistance,
                    ref bestDirection, ref bestClearance, ref bestScore);
            }
        }

        float turn = Vector3.Dot(Vector3.Cross(forward, bestDirection), Vector3.up);
        if (Mathf.Abs(turn) > 0.01f)
            avoidanceSide = Mathf.Sign(turn);

        float safeSpeed = Mathf.Min(maxSpeed,
            Mathf.Sqrt(2f * Mathf.Max(0f, maxAcceleration) * bestClearance));
        Vector3 avoidance = (bestDirection * safeSpeed - velocity) * Mathf.Max(1f, avoidWeight);
        // Blending pursuit back in here reintroduces the component aimed into the wall,
        // making the agent alternate between both sides in a tight corridor.
        return avoidance;
    }

    private void ScoreAvoidanceDirection(
        Vector3 candidate, Vector3 forward, Vector3 desiredDirection,
        float lookAhead, float scoringDistance,
        ref Vector3 bestDirection, ref float bestClearance, ref float bestScore)
    {
        float clearance = CastBody(candidate, lookAhead, out RaycastHit hit)
            ? Mathf.Max(0f, hit.distance - collisionSkin) : lookAhead;
        float score = Mathf.Clamp01(clearance / scoringDistance) * 3f
            + Vector3.Dot(candidate, desiredDirection) * 2f;
        float turn = Vector3.Dot(Vector3.Cross(forward, candidate), Vector3.up);
        if (Mathf.Abs(turn) > 0.01f && Mathf.Sign(turn) == avoidanceSide)
            score += 0.1f;

        if (score <= bestScore)
            return;

        bestScore = score;
        bestDirection = candidate;
        bestClearance = clearance;
    }

    protected virtual void ApplyBounds()
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
