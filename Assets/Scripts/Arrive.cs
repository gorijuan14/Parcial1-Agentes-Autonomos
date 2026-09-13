using UnityEngine;

public class Arrive : SteeringBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float slowRadius = 3f;
    [SerializeField] private float stopRadius = 0.1f;

    private SteeringAgent agent;

    private void Awake()
    {
        agent = GetComponent<SteeringAgent>();
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
}