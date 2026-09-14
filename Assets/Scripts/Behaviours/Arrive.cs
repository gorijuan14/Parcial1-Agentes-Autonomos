using UnityEngine;

public class Arrive : SteeringBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float slowRadius = 10f;
    [SerializeField] private float stopRadius = 5f;

    private Agent agent;

    private void Awake()
    {
        agent = GetComponent<Agent>();
    }

    public override Vector3 CalculateSteering()
    {
        if (target == null || agent == null)
        {
            return Vector3.zero;
        }

        Vector3 offset = target.position - transform.position;
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
        target = newTarget;
    }

}
