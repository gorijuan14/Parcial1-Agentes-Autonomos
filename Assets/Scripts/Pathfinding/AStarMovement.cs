
using System.Collections.Generic;
using UnityEngine;

public class AStarMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Agent agent;
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private Arrive arriveBehaviour;
    [SerializeField] private NavigationGraph navigationGraph;

    [Header("Path")]
    [SerializeField] private GraphNode startNode;
    [SerializeField] private GraphNode goalNode;
    [SerializeField] private float waypointRadius = 0.4f;

    private List<GraphNode> path = new List<GraphNode>();
    private int currentIndex;
    private bool followingPath;

    [Header("Node Arrive")]
    [SerializeField] private float nodeSlowRadius = 2f;
    [SerializeField] private float nodeStopRadius = 0.3f;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<Agent>();

        if (arriveBehaviour == null)
            arriveBehaviour = GetComponent<Arrive>();
    }

    private void Update()
    {
        if (!followingPath || path == null || path.Count == 0)
            return;

        if (agent == null || arriveBehaviour == null || path.Exists(node => node == null))
        {
            StopFollowingPath();
            return;
        }

        GraphNode targetNode = path[currentIndex];

        Vector3 offset = targetNode.Position - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= waypointRadius ||
            HasPassedWaypoint() ||
            CanSkipWaypoint())
        {
            currentIndex++;

            if (currentIndex >= path.Count)
            {
                StopFollowingPath();
                return;
            }

            targetNode = path[currentIndex];
        }

        arriveBehaviour.SetTargetNew(targetNode.transform, nodeSlowRadius, nodeStopRadius);
        agent.SetBehaviour(arriveBehaviour);
    }

    public void CalculateAndFollowPath(GraphNode start, GraphNode goal)
    {
        if (pathfinder == null || agent == null ||
            arriveBehaviour == null || start == null || goal == null)
        {
            StopFollowingPath();
            return;
        }

        path = pathfinder.FindPath(start, goal);

        if (path.Count == 0)
        {
            StopFollowingPath();
            return;
        }

        currentIndex = path.Count > 1 ? 1 : 0;
        followingPath = true;
    }

    public void StopFollowingPath()
    {
        followingPath = false;
        path.Clear();
        currentIndex = 0;

        if (agent != null)
        {
            agent.SetBehaviour(null);
            agent.StopMovement();
        }

        if (arriveBehaviour != null)
            arriveBehaviour.SetTarget(null);
    }

    [ContextMenu("Follow Configured Path")]
    private void FollowConfiguredPath()
    {
        CalculateAndFollowPath(startNode, goalNode);
    }

    
    private bool HasPassedWaypoint()
    {
        if (currentIndex <= 0 || currentIndex >= path.Count)
            return false;

        Vector3 previousPosition = path[currentIndex - 1].Position;
        Vector3 targetPosition = path[currentIndex].Position;

        Vector3 incomingDirection =
            targetPosition - previousPosition;

        incomingDirection.y = 0f;

        if (incomingDirection.sqrMagnitude < 0.001f)
            return false;

        incomingDirection.Normalize();

        Vector3 fromTargetToHunter =
            transform.position - targetPosition;

        fromTargetToHunter.y = 0f;

        // ¿El Hunter ya cruzó el plano perpendicular al recorrido?
        if (Vector3.Dot(fromTargetToHunter, incomingDirection) <= 0f)
            return false;

        // Evitar avanzar si pasó demasiado lejos lateralmente.
        Vector3 lateralOffset =
            fromTargetToHunter -
            Vector3.Project(fromTargetToHunter, incomingDirection);

        return lateralOffset.magnitude <= waypointRadius * 2f;
    }

    public GraphNode CurrentTargetNode
    {
        get
        {
            if (!followingPath || path == null ||
                currentIndex < 0 ||
                currentIndex >= path.Count)
            {
                return null;
            }

            return path[currentIndex];
        }
    }

    private bool CanSkipWaypoint()
    {
        if (navigationGraph == null ||
            currentIndex < 1 ||
            currentIndex + 1 >= path.Count)
        {
            return false;
        }

        GraphNode currentNode = path[currentIndex];
        GraphNode nextNode = path[currentIndex + 1];

        Vector3 toCurrentNode =
            currentNode.Position - transform.position;
        toCurrentNode.y = 0f;

        // Solo evaluar cuando estamos cerca del waypoint.
        if (toCurrentNode.magnitude > nodeSlowRadius + 2f)
            return false;

        // Detectar una pared detrás del waypoint,
        // siguiendo la dirección de llegada.
        Vector3 previousNodePosition =
            path[currentIndex - 1].Position;

        Vector3 incomingDirection =
            currentNode.Position - previousNodePosition;
        incomingDirection.y = 0f;

        if (incomingDirection.sqrMagnitude < 0.001f)
            return false;

        incomingDirection.Normalize();

        Vector3 rayOrigin =
            currentNode.Position + Vector3.up * 0.5f;

        bool wallBehindNode = Physics.Raycast(
            rayOrigin,
            incomingDirection,
            5f,
            navigationGraph.ObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        if (!wallBehindNode)
            return false;

        // Comprobar si podemos continuar hacia el siguiente nodo.
        Vector3 nextDirection =
            nextNode.Position - transform.position;
        nextDirection.y = 0f;

        float nextDistance = nextDirection.magnitude;

        if (nextDistance < 0.001f)
            return false;

        return !Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            nextDirection.normalized,
            nextDistance,
            navigationGraph.ObstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }
}
