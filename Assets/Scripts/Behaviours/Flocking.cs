using UnityEngine;

public class Flocking : SteeringBehaviour
{

    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float alignmentRadius = 5f;
    [SerializeField] private float cohesionRadius = 5f;
    [SerializeField, Range(0f, 1f)] private float separationWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float alignmentWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float cohesionWeight = 1f;

    

    public override Vector3 CalculateSteering()
    {
        float perceptionRadius = Mathf.Max(alignmentRadius, cohesionRadius);
        
        Collider[] neighbors = Physics.OverlapSphere(
            transform.position,
            perceptionRadius
        );

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        int alignmentCount = 0;
        int cohesionCount = 0;

        Agent agent = GetComponent<Agent>();

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform)
            {
                continue;
            }

            Vector3 direction = transform.position - neighbor.transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            Agent neighborAgent = neighbor.GetComponent<Agent>();

            if (neighborAgent == null)
            {
                continue;
            }

            if (distance > 0f && distance < alignmentRadius)
            {
                alignment += neighborAgent.Velocity;
                alignmentCount++;
            }

            if (distance > 0f && distance < cohesionRadius)
            {
                cohesion += neighbor.transform.position;
                cohesionCount++;
            }

            if (distance > 0f && distance < separationRadius)
            {
                separation += direction.normalized / distance;
            }
        }

        if (alignmentCount > 0)
        {
            alignment /= alignmentCount;

            if (agent != null)
            {
                alignment -= agent.Velocity;
            }
        }

        if (cohesionCount > 0)
        {
            Vector3 averagePosition = cohesion / cohesionCount;

            Vector3 directionToCenter = averagePosition - transform.position;
            directionToCenter.y = 0f;

            if (directionToCenter.sqrMagnitude > 0.001f && agent != null)
            {
                cohesion = directionToCenter.normalized * agent.MaxSpeed - agent.Velocity;
            }
            else
            {
                cohesion = Vector3.zero;
            }
        }

        return separation * separationWeight + alignment * alignmentWeight + cohesion * cohesionWeight;   
    }
}