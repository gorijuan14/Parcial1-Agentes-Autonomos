
using UnityEngine;

public class ZoneGizmo : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private Vector3 zoneSize = new Vector3(10f, 0.1f, 10f);
    [SerializeField] private Color zoneColor = Color.green;

    public Vector3 Center => transform.position;
    public Vector3 Size => zoneSize;

    public bool Contains(Vector3 position, float padding = 0f)
    {
        Vector3 half = zoneSize * 0.5f;
        return Mathf.Abs(position.x - Center.x) <= Mathf.Max(0f, half.x - padding) &&
            Mathf.Abs(position.z - Center.z) <= Mathf.Max(0f, half.z - padding);
    }

    public bool IntersectsSegment(Vector3 from, Vector3 to, float clearance = 0f)
    {
        Vector3 half = zoneSize * 0.5f + new Vector3(clearance, 0f, clearance);
        float enter = 0f;
        float exit = 1f;
        return ClipAxis(from.x - Center.x, to.x - from.x, half.x, ref enter, ref exit) &&
            ClipAxis(from.z - Center.z, to.z - from.z, half.z, ref enter, ref exit);
    }

    private static bool ClipAxis(float origin, float direction, float halfSize, ref float enter, ref float exit)
    {
        if (Mathf.Abs(direction) < 0.0001f)
            return Mathf.Abs(origin) <= halfSize;
        float first = (-halfSize - origin) / direction;
        float second = (halfSize - origin) / direction;
        enter = Mathf.Max(enter, Mathf.Min(first, second));
        exit = Mathf.Min(exit, Mathf.Max(first, second));
        return enter <= exit;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = zoneColor;
        Gizmos.DrawCube(transform.position, zoneSize);
    }
}
