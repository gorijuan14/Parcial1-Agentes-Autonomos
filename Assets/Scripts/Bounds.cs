using UnityEngine;

public class Bounds : MonoBehaviour
{
    [SerializeField] private float minX = -25f;
    [SerializeField] private float maxX = 25f;
    [SerializeField] private float minZ = -25f;
    [SerializeField] private float maxZ = 25f;

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

        Vector3 center = new Vector3(
            (minX + maxX) / 2f,
            0f,
            (minZ + maxZ) / 2f
        );

        Vector3 size = new Vector3(
            maxX - minX,
            0f,
            maxZ - minZ
        );

        Gizmos.DrawWireCube(center, size);
    }
}