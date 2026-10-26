using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }
    [SerializeField] private ZoneGizmo huntingZone;

    public Vector3 Center => huntingZone != null ? huntingZone.Center
        : new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
    public Vector3 Size => huntingZone != null ? huntingZone.Size
        : new Vector3(maxX - minX, 0f, maxZ - minZ);

    [SerializeField] private float minX = -25f;
    [SerializeField] private float maxX = 25f;
    [SerializeField] private float minZ = -25f;
    [SerializeField] private float maxZ = 25f;

    private void Awake()
    {
        Instance = this;
    }

    public bool Contains(Vector3 position, float padding = 0f)
    {
        Vector3 clamped = ClampPosition(position, padding);
        return Mathf.Abs(clamped.x - position.x) < 0.001f &&
            Mathf.Abs(clamped.z - position.z) < 0.001f;
    }

    public Vector3 ClampPosition(Vector3 position, float padding = 0f)
    {
        Vector3 center = Center;
        Vector3 halfSize = Size * 0.5f;
        float halfX = Mathf.Max(0f, halfSize.x - padding);
        float halfZ = Mathf.Max(0f, halfSize.z - padding);
        position.x = Mathf.Clamp(position.x, center.x - halfX, center.x + halfX);
        position.z = Mathf.Clamp(position.z, center.z - halfZ, center.z + halfZ);
        return position;
    }

    public Vector3 RandomPosition(float height, float padding = 0f)
    {
        Vector3 min = ClampPosition(Center - Size, padding);
        Vector3 max = ClampPosition(Center + Size, padding);
        return new Vector3(Random.Range(min.x, max.x), height, Random.Range(min.z, max.z));
    }

    public void WrapPosition(Transform agent)
    {
        Vector3 position = agent.position;

        if (position.x < minX)
        {
            position.x = maxX;
        }
        else if (position.x > maxX)
        {
            position.x = minX;
        }

        if (position.z < minZ)
        {
            position.z = maxZ;
        }
        else if (position.z > maxZ)
        {
            position.z = minZ;
        }

       agent.position = position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(Center, Size);
    }
}
