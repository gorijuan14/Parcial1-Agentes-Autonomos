using UnityEngine;

public class Patrol : SteeringBehaviour
{
    [Header("References")]
    private Agent agent;
    private WaypointPatrol waypointPatrol;
    private Arrive arrive;

    private void Awake()
    {
        agent = GetComponent<Agent>();
        waypointPatrol = GetComponent<WaypointPatrol>();
        arrive = GetComponent<Arrive>();
    }

    public override Vector3 CalculateSteering()
    {
        if (waypointPatrol == null || arrive == null || waypointPatrol.CurrentWaypoint == null)
        {
            if (agent != null)
                agent.StopMovement();

            return Vector3.zero;
        }

        if (waypointPatrol.HasReachedWaypoint())
        {
            waypointPatrol.GoToNextWaypoint();
        }

        arrive.SetTarget(waypointPatrol.CurrentWaypoint);

        return arrive.CalculateSteering();
    }
}
