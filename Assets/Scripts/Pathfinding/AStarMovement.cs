
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
    private bool hasDestination;
    private Vector3 destination;
    public bool IsFollowingPath => followingPath;
    public Vector3 Destination => hasDestination ? destination :
        CurrentTargetNode != null ? CurrentTargetNode.Position : transform.position;
    public NavigationGraph Graph => navigationGraph;

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

        if (currentIndex >= path.Count)
        {
            FollowDestination();
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
                if (hasDestination)
                    FollowDestination();
                else
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
        hasDestination = false;
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

    public bool TryFollowPosition(Vector3 position)
    {
        if (!TryPlanPath(position, out List<GraphNode> newPath, out _))
            return false;

        path = newPath;
        currentIndex = 0;
        while (currentIndex + 1 < path.Count && agent.CanMoveDirectlyTo(path[currentIndex + 1].Position))
            currentIndex++;
        destination = position;
        hasDestination = true;
        followingPath = true;
        arriveBehaviour.SetTargetNew(path[currentIndex].transform, nodeSlowRadius, nodeStopRadius);
        agent.SetBehaviour(arriveBehaviour);
        return true;
    }

    public bool TryPlanPath(Vector3 position, out List<GraphNode> plannedPath, out float length)
    {
        plannedPath = null;
        length = float.PositiveInfinity;
        if (navigationGraph == null)
            navigationGraph = FindFirstObjectByType<NavigationGraph>();
        if (pathfinder == null)
            pathfinder = FindFirstObjectByType<AStarPathfinder>();
        if (agent == null || arriveBehaviour == null || navigationGraph == null || pathfinder == null)
            return false;

        GraphNode start = FindAccessibleNode(transform.position);
        GraphNode goal = FindAccessibleNode(position);
        if (start == null || goal == null)
            return false;

        plannedPath = pathfinder.FindPath(start, goal, agent);
        if (plannedPath.Count == 0)
            return false;
        Vector3 previous = transform.position;
        length = 0f;
        foreach (GraphNode node in plannedPath)
        {
            Vector3 offset = node.Position - previous;
            offset.y = 0f;
            length += offset.magnitude;
            previous = node.Position;
        }
        Vector3 finalOffset = position - previous;
        finalOffset.y = 0f;
        length += finalOffset.magnitude;
        return true;
    }

    private GraphNode FindAccessibleNode(Vector3 position)
    {
        GraphNode closest = null;
        float bestDistance = float.PositiveInfinity;
        foreach (GraphNode node in navigationGraph.Nodes)
        {
            if (node == null || !agent.CanUseNode(node))
                continue;

            float distance = (node.Position - position).sqrMagnitude;
            if (distance >= bestDistance || !agent.CanTraverseSegment(position, node.Position))
                continue;

            closest = node;
            bestDistance = distance;
        }
        return closest;
    }

    private void FollowDestination()
    {
        Vector3 offset = destination - transform.position;
        offset.y = 0f;
        if (!hasDestination || offset.magnitude <= Mathf.Max(waypointRadius, nodeStopRadius))
        {
            StopFollowingPath();
            return;
        }

        arriveBehaviour.SetPositionTarget(destination, nodeSlowRadius, nodeStopRadius);
        agent.SetBehaviour(arriveBehaviour);
    }

    public void StopFollowingPath()
    {
        followingPath = false;
        hasDestination = false;
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

    private void OnDrawGizmos()
    {
        if (!followingPath || path == null)
            return;

        Gizmos.color = agent is Hunter ? StateIndicator.HunterGreen : Color.cyan;
        Vector3 previous = transform.position + Vector3.up * 0.2f;
        for (int i = currentIndex; i < path.Count; i++)
        {
            if (path[i] == null)
                continue;
            Vector3 next = path[i].Position + Vector3.up * 0.2f;
            Gizmos.DrawLine(previous, next);
            Gizmos.DrawWireSphere(next, 0.2f);
            previous = next;
        }
        if (hasDestination)
            Gizmos.DrawLine(previous, destination + Vector3.up * 0.2f);
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

        return agent != null && agent.CanMoveDirectlyTo(nextNode.Position);
    }
}
