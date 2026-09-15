using UnityEngine;

public class Hunter : Agent
{
    [Header("Attack")]
    [SerializeField] private float tba = 2f;
    [SerializeField] private float rangeAttackRadius = 5f;
    [SerializeField] private float meleeAttackRadius = 0.5f;

    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;

    [Header("Bait")]
    [SerializeField] private GameObject baitPrefab;
    [SerializeField] private int maxBaits = 5;
    [SerializeField] private float baitSpawnInterval = 5f;

    private float baitTimer;

    public bool UpdateBaits()
    {
        baitTimer += Time.deltaTime;

        if (baitTimer < baitSpawnInterval)
        {
            return false;
        }

        baitTimer = 0f;

        int activeBaits = FindObjectsByType<Bait>(
            FindObjectsSortMode.None
        ).Length;

        if (activeBaits >= maxBaits)
        {
            return false;
        }

        SpawnBait();
        return true;
    }

    public void SpawnBait()
    {
        baitTimer = 0f;

        if (baitPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position;

        Instantiate(
            baitPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    public bool ShouldSpawnBait()
    {
        baitTimer += Time.deltaTime;

        if (baitTimer < baitSpawnInterval)
        {
            return false;
        }

        int activeBaits = FindObjectsByType<Bait>(
            FindObjectsSortMode.None
        ).Length;

        if (activeBaits >= maxBaits)
        {
            return false;
        }

        return true;
    }

}