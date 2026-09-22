using UnityEngine;

public class Flocking : SteeringBehaviour
{
    [Header("Stats")]
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float alignmentRadius = 5f;
    [SerializeField] private float cohesionRadius = 5f;

    [SerializeField, Range(0f, 5f)] private float separationWeight = 1f;
    [SerializeField, Range(0f, 5f)] private float alignmentWeight = 1f;
    [SerializeField, Range(0f, 5f)] private float cohesionWeight = 1f;

    public override Vector3 CalculateSteering()
    {
        Collider[] neighbors = Physics.OverlapSphere(
            transform.position,
            Mathf.Max(alignmentRadius, cohesionRadius)
        );

        Vector3 separation = CalculateSeparation(neighbors);
        Vector3 alignment = CalculateAlignment(neighbors);
        Vector3 cohesion = CalculateCohesion(neighbors);

        return separation * separationWeight
             + alignment * alignmentWeight
             + cohesion * cohesionWeight;
    }

    private Vector3 CalculateSeparation(Collider[] neighbors)
    {
        Vector3 separation = Vector3.zero;
        Boid boid = GetComponent<Boid>();

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform)
                continue;

            Boid neighborBoid = neighbor.GetComponent<Boid>();

            if (neighborBoid == null)
                continue;

            Vector3 direction = transform.position - neighborBoid.transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > 0f && distance < separationRadius)
            {
                separation += direction.normalized / distance;
            }
        }

        return separation;
    }

    private Vector3 CalculateAlignment(Collider[] neighbors)
    {
        Vector3 alignment = Vector3.zero;
        int alignmentCount = 0;

        Boid boid = GetComponent<Boid>();

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform)
                continue;

            Boid neighborBoid = neighbor.GetComponent<Boid>();

            if (neighborBoid == null)
                continue;

            Vector3 direction = transform.position - neighborBoid.transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > 0f && distance < alignmentRadius)
            {
                alignment += neighborBoid.Velocity;
                alignmentCount++;
            }
        }

        if (alignmentCount > 0)
        {
            alignment /= alignmentCount;

            if (boid != null)
                alignment -= boid.Velocity;
        }

        return alignment;
    }

    private Vector3 CalculateCohesion(Collider[] neighbors)
    {
        Vector3 cohesion = Vector3.zero;
        int cohesionCount = 0;

        Boid boid = GetComponent<Boid>();

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform)
                continue;

            Boid neighborBoid = neighbor.GetComponent<Boid>();

            if (neighborBoid == null)
                continue;

            Vector3 direction = transform.position - neighborBoid.transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance > 0f && distance < cohesionRadius)
            {
                cohesion += neighborBoid.transform.position;
                cohesionCount++;
            }
        }

        if (cohesionCount > 0)
        {
            Vector3 averagePosition = cohesion / cohesionCount;
            Vector3 directionToCenter =
                averagePosition - transform.position;

            directionToCenter.y = 0f;

            if (directionToCenter.sqrMagnitude > 0.001f && boid != null)
            {
                cohesion =
                    directionToCenter.normalized * boid.MaxSpeed
                    - boid.Velocity;
            }
            else
            {
                cohesion = Vector3.zero;
            }
        }

        return cohesion;
    }
}