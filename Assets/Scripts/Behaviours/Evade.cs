using UnityEngine;

public class Evade : SteeringBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    private Agent targetAgent;
    
    [Header("Stats")]
    [SerializeField] private float predictionTime = 0.1f;
    [SerializeField] private float evadeStrength = 0.5f;

    private void Awake()
    {
        if (target != null)
        {
            targetAgent = target.GetComponent<Agent>();
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

        Vector3 direction = transform.position - futurePosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        return direction.normalized * targetAgent.MaxAcceleration * evadeStrength;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        targetAgent = newTarget != null
            ? newTarget.GetComponent<Agent>()
            : null;
    }

}