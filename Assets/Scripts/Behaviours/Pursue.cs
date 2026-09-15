using UnityEngine;

public class Pursue : SteeringBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    private Agent targetAgent;
    private Agent agent;

    [Header("Stats")]
    private float maxPredictionTime = 2f;

    private void Awake()
    {
        agent = GetComponent<Agent>();

        if (target != null)
        {
            targetAgent = target.GetComponent<Agent>();
        }
    }

    public override Vector3 CalculateSteering()
    {
        if (target == null || targetAgent == null || agent == null)
        {
            return Vector3.zero;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        float predictionTime = Mathf.Min(maxPredictionTime, distance / agent.MaxSpeed);

        Vector3 futurePosition = target.position + targetAgent.Velocity * predictionTime;

        Vector3 direction = futurePosition - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        return direction.normalized * agent.MaxAcceleration;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        targetAgent = newTarget != null
            ? newTarget.GetComponent<Agent>()
            : null;
    }
}
