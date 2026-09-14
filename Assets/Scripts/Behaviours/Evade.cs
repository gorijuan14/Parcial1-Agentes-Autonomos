using UnityEngine;

public class Evade : SteeringBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float predictionTime = 0.1f;

    private Agent targetAgent;

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

        return direction.normalized * targetAgent.MaxAcceleration;
    }
}