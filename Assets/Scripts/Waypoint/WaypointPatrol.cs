using UnityEngine;

public class WaypointPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float arrivalDistance = 5f;

    private int currentWaypoint;

    public Transform CurrentWaypoint => waypoints[currentWaypoint];

    private void Start()
    {
        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"{name} no tiene waypoints asignados.");
        }
    }

    public bool HasReachedWaypoint()
    {
        if (waypoints.Length == 0)
        {
            return false;
        }

        Vector3 offset = CurrentWaypoint.position - transform.position;
        offset.y = 0f;

        return offset.magnitude <= arrivalDistance;
    }

    public void GoToNextWaypoint()
    {
        if (waypoints.Length == 0)
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