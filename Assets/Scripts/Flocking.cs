using UnityEngine;

public class Flocking : SteeringBehaviour
{

    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float perceptionRadius = 5f;
    [SerializeField] private float separationWeight = 1f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    public override Vector3 CalculateSteering()
    {
        Collider[] neighbors = Physics.OverlapSphere(
            transform.position,
            perceptionRadius
        );

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        int neighborCount = 0;

        SteeringAgent agent = GetComponent<SteeringAgent>();

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform)
            {
                continue;
            }

            Vector3 direction = transform.position - neighbor.transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            SteeringAgent neighborAgent = neighbor.GetComponent<SteeringAgent>();

            if (neighborAgent == null)
            {
                continue;
            }

            if (distance > 0f && distance < perceptionRadius)
            {
                alignment += neighborAgent.Velocity;
                cohesion += neighbor.transform.position;
                neighborCount++;
            }

            if (distance > 0f && distance < separationRadius)
            {
                separation += direction.normalized / distance;
            }
        }

        if (neighborCount > 0)
        {
            alignment /= neighborCount;

            if (agent != null)
            {
                alignment -= agent.Velocity;
                Vector3 averagePosition = cohesion / neighborCount;

                Vector3 directionToCenter = averagePosition - transform.position;
                directionToCenter.y = 0f;

                if (directionToCenter.sqrMagnitude > 0.001f)
                {
                    cohesion = directionToCenter.normalized * agent.MaxSpeed - agent.Velocity;
                }
                else
                {
                    cohesion = Vector3.zero;
                }
            }
        }



        return separation * separationWeight + alignment * alignmentWeight + cohesion * cohesionWeight;   
    }
}