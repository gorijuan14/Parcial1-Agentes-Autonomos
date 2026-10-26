using UnityEngine;

public class WaypointPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] waypoints;
    public Transform CurrentWaypoint
    {
        get
        {
            if (waypoints == null || waypoints.Length == 0)
                return null;

            currentWaypoint %= waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[currentWaypoint] != null)
                    return waypoints[currentWaypoint];

                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            }

            return null;
        }
    }

    [Header("Stats")]
    [SerializeField] private float arrivalDistance = 5f;
    private int currentWaypoint;

    private void Start()
    {
        if (CurrentWaypoint == null)
        {
            Debug.LogWarning($"{name} no tiene waypoints asignados.");
        }
    }

    public bool HasReachedWaypoint()
    {
        Transform waypoint = CurrentWaypoint;
        if (waypoint == null)
        {
            return false;
        }

        Vector3 offset = waypoint.position - transform.position;
        offset.y = 0f;

        return offset.magnitude <= arrivalDistance;
    }

    public void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        currentWaypoint++;

        if (currentWaypoint >= waypoints.Length)
        {
            currentWaypoint = 0;
        }
    }
}
