
using UnityEngine;

public class ZoneGizmo : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private Vector3 zoneSize = new Vector3(10f, 0.1f, 10f);
    [SerializeField] private Color zoneColor = Color.green;

    public Vector3 Center => transform.position;
    public Vector3 Size => zoneSize;

    private void OnDrawGizmos()
    {
        Gizmos.color = zoneColor;
        Gizmos.DrawCube(transform.position, zoneSize);
    }
}
