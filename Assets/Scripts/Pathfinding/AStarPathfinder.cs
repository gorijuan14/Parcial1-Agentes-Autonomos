
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavigationGraph graph;

    public List<GraphNode> FindPath(GraphNode start, GraphNode goal, Agent agent = null)
    {
        if (graph == null || start == null || goal == null)
            return new List<GraphNode>();

        if (start == goal)
            return new List<GraphNode> { start };

        var frontier = new PriorityQueue<GraphNode>();
        frontier.Enqueue(start, Heuristic(start, goal));
        var processed = new HashSet<GraphNode>();

        var cameFrom = new Dictionary<GraphNode, GraphNode>();
        var gScore = new Dictionary<GraphNode, float>
        {
            [start] = 0f
        };

        while (frontier.Count > 0)
        {
            GraphNode current = frontier.Dequeue();
            // A cheaper route can enqueue the same node again; ignore stale entries.
            if (!processed.Add(current))
                continue;

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (GraphNode neighbor in current.Connections)
            {
                if (neighbor == null)
                    continue;

                // Nunca utilizar una conexión que atraviese una pared.
                if (!graph.CanConnect(current, neighbor))
                    continue;

                if (agent != null && !agent.CanTraverseSegment(current.Position, neighbor.Position))
                    continue;

                float tentativeGScore =
                    gScore[current] +
                    Vector3.Distance(current.Position, neighbor.Position);

                if (gScore.TryGetValue(neighbor, out float knownGScore) &&
                    tentativeGScore >= knownGScore)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                frontier.Enqueue(neighbor, tentativeGScore + Heuristic(neighbor, goal));
                processed.Remove(neighbor);
            }
        }

        // No existe una ruta entre el origen y el destino.
        return new List<GraphNode>();
    }

    private float Heuristic(GraphNode a, GraphNode b)
    {
        return Vector3.Distance(a.Position, b.Position);
    }

    private List<GraphNode> ReconstructPath(
        Dictionary<GraphNode, GraphNode> cameFrom,
        GraphNode current)
    {
        var path = new List<GraphNode> { current };

        while (cameFrom.TryGetValue(current, out GraphNode previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
