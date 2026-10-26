
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

        var openSet = new List<GraphNode> { start };
        var closedSet = new HashSet<GraphNode>();

        var cameFrom = new Dictionary<GraphNode, GraphNode>();
        var gScore = new Dictionary<GraphNode, float>
        {
            [start] = 0f
        };

        var fScore = new Dictionary<GraphNode, float>
        {
            [start] = Heuristic(start, goal)
        };

        while (openSet.Count > 0)
        {
            GraphNode current = GetLowestScore(openSet, fScore);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (GraphNode neighbor in current.Connections)
            {
                if (neighbor == null || closedSet.Contains(neighbor))
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
                fScore[neighbor] =
                    tentativeGScore + Heuristic(neighbor, goal);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        // No existe una ruta entre el origen y el destino.
        return new List<GraphNode>();
    }

    private float Heuristic(GraphNode a, GraphNode b)
    {
        return Vector3.Distance(a.Position, b.Position);
    }

    private GraphNode GetLowestScore(
        List<GraphNode> openSet,
        Dictionary<GraphNode, float> fScore)
    {
        GraphNode bestNode = openSet[0];
        float bestScore = fScore.TryGetValue(bestNode, out float score)
            ? score
            : float.PositiveInfinity;

        for (int i = 1; i < openSet.Count; i++)
        {
            GraphNode candidate = openSet[i];
            float candidateScore =
                fScore.TryGetValue(candidate, out float value)
                    ? value
                    : float.PositiveInfinity;

            if (candidateScore < bestScore)
            {
                bestNode = candidate;
                bestScore = candidateScore;
            }
        }

        return bestNode;
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
