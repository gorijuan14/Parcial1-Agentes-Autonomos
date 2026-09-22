using UnityEngine;

public class HunterSensor : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float perceptionRadius = 7f;

    [Header("Detection")]
    [SerializeField] private float detectionInterval = 0.2f;

    private float detectionTimer;
    private Transform closestBoid;

    private void Update()
    {
        detectionTimer += Time.deltaTime;

        if (detectionTimer < detectionInterval)
        {
            return;
        }

        detectionTimer = 0f;

        DetectClosestBoid();
    }

    private void DetectClosestBoid()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            perceptionRadius
        );

        closestBoid = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponent<Boid>();

            if (boid == null || boid.IsDead)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                boid.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBoid = boid.transform;
            }
        }
    }

    public Transform GetClosestBoid()
    {
        return closestBoid;
    }
}