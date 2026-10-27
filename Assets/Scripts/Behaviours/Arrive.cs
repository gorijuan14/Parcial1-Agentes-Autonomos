using UnityEngine;

public class Arrive : SteeringBehaviour
{
    [Header("References")]
    private Agent agent;
    [SerializeField] private Transform target;
    private bool hasPositionTarget;
    private Vector3 positionTarget;

    [Header("Stats")]
    [SerializeField] private float slowRadius = 5f;
    [SerializeField] private float stopRadius = 1f;
    

    private void Awake()
    {
        agent = GetComponent<Agent>();
    }

    public override Vector3 CalculateSteering()
    {
        return CalculateSteering(slowRadius, stopRadius);
    }

    public bool TryGetTargetPosition(out Vector3 position)
    {
        position = hasPositionTarget ? positionTarget : target != null ? target.position : transform.position;
        return hasPositionTarget || target != null;
    }

    public Vector3 CalculateSteering(float slowRadius, float stopRadius)
    {
        if ((!hasPositionTarget && target == null) || agent == null)
        {
            return Vector3.zero;
        }

        Vector3 offset = (hasPositionTarget ? positionTarget : target.position) - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;

        if (distance <= stopRadius)
        {
            return -agent.Velocity;
        }

        float targetSpeed = agent.MaxSpeed;

        if (distance < slowRadius)
        {
            targetSpeed *= distance / slowRadius;
        }

        Vector3 desiredVelocity = offset.normalized * targetSpeed;

        return desiredVelocity - agent.Velocity;
    }

    public void SetTarget(Transform newTarget)
    {
        hasPositionTarget = false;
        target = newTarget;
    }

    public void SetPositionTarget(Vector3 position, float newSlowRadius, float newStopRadius)
    {
        hasPositionTarget = true;
        positionTarget = position;
        slowRadius = newSlowRadius;
        stopRadius = newStopRadius;
    }

    public void SetTargetNew(Transform newTarget, float newSlowRadius, float newStopRadius)
    {
        hasPositionTarget = false;
        target = newTarget;
        slowRadius = newSlowRadius;
        stopRadius = newStopRadius;
    }

}
