using UnityEngine;

public class StateIndicator : MonoBehaviour
{
    [Header("Hunter Sprites")]
    [SerializeField] private Sprite hunterPatrol;
    [SerializeField] private Sprite hunterChase;
    [SerializeField] private Sprite hunterRangedAttack;
    [SerializeField] private Sprite hunterMeleeAttack;
    [SerializeField] private Sprite hunterGather;
    [SerializeField] private Sprite hunterBait;
    private SpriteRenderer spriteRenderer;

    [Header("Boid Sprites")]
    [SerializeField] private Sprite boidPatrol;
    [SerializeField] private Sprite boidEvade;
    [SerializeField] private Sprite boidDistracted;


    [SerializeField] private float patrolHideDelay = 2f;
    private float patrolTimer;

    private enum IndicatorState
    {
        None,
        Patrol,
        Evade,
        Distracted,
        Chase,
        RangedAttack,
        MeleeAttack,
        Bait,
        Gather
    }

    private IndicatorState currentState;

    private SpriteRenderer SpriteRenderer
    {
        get
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            return spriteRenderer;
        }
    }

    private void Update()
    {
        if (currentState != IndicatorState.Patrol)
        return;

        if (!SpriteRenderer.enabled)
            return;

        patrolTimer += Time.deltaTime;

        if (patrolTimer >= patrolHideDelay)
        {
            SpriteRenderer.enabled = false;
        }
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(-90, 0, 0);
    }

    public void SetHunterPatrol()
    {
        if (currentState != IndicatorState.Patrol)
        {
            currentState = IndicatorState.Patrol;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = hunterPatrol;   
        }
    }

    public void SetHunterChase()
    {
        if (currentState != IndicatorState.Chase)
        {
            currentState = IndicatorState.Chase;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = hunterChase;   
        }
    }

    public void SetHunterRangedAttack()
    {
        if (currentState != IndicatorState.RangedAttack)
        {
            currentState = IndicatorState.RangedAttack;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = hunterRangedAttack;   
        }
    }

    public void SetHunterMeleeAttack()
    {
        if (currentState != IndicatorState.MeleeAttack)
        {
            currentState = IndicatorState.MeleeAttack;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = hunterMeleeAttack;   
        }
    }

    public void SetHunterGather()
    {
        if (currentState != IndicatorState.Gather)
        {
            currentState = IndicatorState.Gather;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = hunterGather;   
        }
    }

    public void SetHunterBait()
    {
        if (currentState != IndicatorState.Bait)
        {
            currentState = IndicatorState.Bait;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = hunterBait;   
        }
    }

    public void SetBoidPatrol()
    {
        if (currentState != IndicatorState.Patrol)
        {
            currentState = IndicatorState.Patrol;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = boidPatrol;   
        }
    }

    public void SetBoidEvade()
    {
        if (currentState != IndicatorState.Evade)
        {
            currentState = IndicatorState.Evade;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = boidEvade;   
        }
    }

    public void SetBoidDistracted()
    {
        if (currentState != IndicatorState.Distracted)
        {
            currentState = IndicatorState.Distracted;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = boidDistracted;   
        }
    }
}
