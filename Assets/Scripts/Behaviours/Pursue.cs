using UnityEngine;

public class Pursue : SteeringBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float predictionTime = 1f;

    private SteeringAgent targetAgent;

    private void Awake()
    {
        if (target != null)
        {
            targetAgent = target.GetComponent<SteeringAgent>();
        }
    }

    public override Vector3 CalculateSteering()
    {
        if (target == null || targetAgent == null)
        {
            return Vector3.zero;
        }

        Vector3 futurePosition = target.position +
                                 targetAgent.Velocity * predictionTime;

        Vector3 direction = futurePosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        return direction.normalized * targetAgent.MaxAcceleration;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetAgent = newTarget != null
            ? newTarget.GetComponent<SteeringAgent>()
            : null;
    }
}