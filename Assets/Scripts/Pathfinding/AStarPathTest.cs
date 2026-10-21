
using System.Collections.Generic;
using UnityEngine;

public class AStarPathTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private GraphNode startNode;
    [SerializeField] private GraphNode goalNode;

    [Header("Visualization")]
    [SerializeField] private bool calculateOnStart = true;
    [SerializeField] private float pathHeight = 0.15f;

    private List<GraphNode> currentPath = new List<GraphNode>();

    private void Start()
    {
        if (calculateOnStart)
            CalculatePath();
    }

    [ContextMenu("Calculate Path")]
    public void CalculatePath()
    {
        if (pathfinder == null || startNode == null || goalNode == null)
        {
            return;
        }

        currentPath = pathfinder.FindPath(startNode, goalNode);

        if (currentPath.Count == 0)
        {
            return;
        }
    }

    private void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < currentPath.Count; i++)
        {
            if (currentPath[i] == null)
                continue;

            Vector3 position =
                currentPath[i].Position + Vector3.up * pathHeight;

            Gizmos.DrawSphere(position, 0.25f);

            if (i + 1 < currentPath.Count && currentPath[i + 1] != null)
            {
                Vector3 nextPosition =
                    currentPath[i + 1].Position +
                    Vector3.up * pathHeight;

                Gizmos.DrawLine(position, nextPosition);
            }
        }
    }
}
