using UnityEngine;

public enum HunterStates
{
    Patrol,
    Attack,
    Gather
}

public class Hunter : Agent
{
    [Header("Attack")]
    [SerializeField] private float tba = 2f;
    [SerializeField] private float rangeAttackRadius = 5f;
    [SerializeField] private float meleeAttackRadius = 0.5f;

    [Header("References")]
    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    private HunterFSM hunterFSM;
    private Transform currentTarget;
    public Transform CurrentTarget => currentTarget;


    [Header("Bait")]
    [SerializeField] private GameObject baitPrefab;
    private int activeBaits;
    [SerializeField] private int maxBaits = 5;
    [SerializeField] private float baitSpawnInterval = 5f;
    private float baitTimer;

    private void Awake()
    {
        hunterFSM = new HunterFSM();

        HunterPatrolState patrolState = new HunterPatrolState(hunterFSM, this);
        HunterAttackState attackState = new HunterAttackState(hunterFSM, this);
        HunterGatherState gatherState = new HunterGatherState(hunterFSM, this);

        hunterFSM.RegisterState(HunterStates.Patrol, patrolState);
        hunterFSM.RegisterState(HunterStates.Attack, attackState);
        hunterFSM.RegisterState(HunterStates.Gather, gatherState);

        hunterFSM.ChangeState(HunterStates.Patrol);
    }

    protected override void Update()
    {
        hunterFSM.Update();
        base.Update();
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }
    

    public bool UpdateBaits()
    {
        baitTimer += Time.deltaTime;

        if (baitTimer < baitSpawnInterval)
        {
            return false;
        }

        if (activeBaits >= maxBaits)
        {
            return false;
        }

        baitTimer = 0f;
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

        GameObject baitObject = Instantiate(
            baitPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Bait bait = baitObject.GetComponent<Bait>();

        if (bait != null)
        {
            bait.SetOwner(this);
            activeBaits++;
        }
    }
    
    public void RemoveBait()
    {
        activeBaits--;

        if (activeBaits < 0)
        {
            activeBaits = 0;
        }
    }
}