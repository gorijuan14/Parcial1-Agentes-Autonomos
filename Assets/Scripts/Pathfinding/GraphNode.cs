
using System.Collections.Generic;
using UnityEngine;

public class GraphNode : MonoBehaviour
{
    [Header("Connections")]
    [SerializeField] private List<GraphNode> connections = new List<GraphNode>();

    public IReadOnlyList<GraphNode> Connections => connections;

    public Vector3 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);

        Gizmos.color = Color.gray;

        foreach (GraphNode connection in connections)
        {
            if (connection == null)
                continue;

            Gizmos.DrawLine(transform.position, connection.Position);
        }
    }
}
