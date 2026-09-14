using UnityEngine;

public class Patrol : SteeringBehaviour
{
    private SteeringAgent agent;
    private WaypointPatrol waypointPatrol;
    private Arrive arrive;

    private void Awake()
    {
        agent = GetComponent<SteeringAgent>();
        waypointPatrol = GetComponent<WaypointPatrol>();
        arrive = GetComponent<Arrive>();
    }

    public override Vector3 CalculateSteering()
    {
        if (waypointPatrol == null || arrive == null)
        {
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