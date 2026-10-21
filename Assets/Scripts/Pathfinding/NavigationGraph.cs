
using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private List<GraphNode> nodes = new List<GraphNode>();

    [Header("Connection Validation")]
    [SerializeField] private LayerMask obstacleMask;
    public LayerMask ObstacleMask => obstacleMask;
    [SerializeField] private float raycastHeight = 0.5f;

    public IReadOnlyList<GraphNode> Nodes => nodes;

    public bool CanConnect(GraphNode from, GraphNode to)
    {
        if (from == null || to == null || from == to)
            return false;

        Vector3 start = from.Position + Vector3.up * raycastHeight;
        Vector3 end = to.Position + Vector3.up * raycastHeight;
        Vector3 direction = end - start;

        if (Physics.Raycast(
            start,
            direction.normalized,
            direction.magnitude,
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (nodes == null)
            return;

        Gizmos.color = Color.yellow;

        foreach (GraphNode node in nodes)
        {
            if (node == null)
                continue;

            Gizmos.DrawWireSphere(node.Position, 0.3f);
        }
    }
}
