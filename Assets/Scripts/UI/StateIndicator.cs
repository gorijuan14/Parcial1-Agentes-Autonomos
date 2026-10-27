using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StateIndicator : MonoBehaviour
{
    public static readonly Color HunterGreen = new Color(0.23f, 1f, 0f);
    public static readonly Color HunterYellow = new Color(1f, 0.97f, 0.14f);
    public static readonly Color HunterWarning = new Color(1f, 0.65f, 0.15f);
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

    [Header("Scene View")]
    [SerializeField, Min(8f)] private float sceneIconSize = 28f;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void RegisterSceneOverlay()
    {
        SceneView.duringSceneGui -= DrawSceneIndicators;
        SceneView.duringSceneGui += DrawSceneIndicators;
    }

    private static void DrawSceneIndicators(SceneView sceneView)
    {
        if (!sceneView.drawGizmos || Event.current.type != EventType.Repaint)
            return;

        Handles.BeginGUI();
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        try
        {
            // Draw in Scene View's GUI pass, above world gizmos and ordinary GUI.
            GUI.depth = int.MinValue + 1;
            foreach (StateIndicator indicator in FindObjectsByType<StateIndicator>(FindObjectsSortMode.None))
            {
                if (indicator.isActiveAndEnabled)
                    indicator.DrawSceneIcon(sceneView);
            }
            foreach (Wolf wolf in FindObjectsByType<Wolf>(FindObjectsSortMode.None))
                if (wolf.isActiveAndEnabled)
                    wolf.DrawSceneOverlay(sceneView);

            // Health is the final overlay, above the state icons as well.
            GUI.depth = int.MinValue;
            GUI.color = Color.white;
            foreach (BoidHealthBar healthBar in FindObjectsByType<BoidHealthBar>(FindObjectsSortMode.None))
            {
                if (healthBar.isActiveAndEnabled)
                    healthBar.DrawSceneOverlay(sceneView);
            }
        }
        finally
        {
            GUI.depth = previousDepth;
            GUI.color = previousColor;
            Handles.EndGUI();
        }
    }

    private void DrawSceneIcon(SceneView sceneView)
    {
        SpriteRenderer renderer = SpriteRenderer;
        if (renderer == null || !renderer.enabled || renderer.sprite == null)
            return;

        Camera camera = sceneView.camera;
        if (Vector3.Dot(camera.transform.forward, transform.position - camera.transform.position) <= 0f)
            return;

        Sprite sprite = renderer.sprite;
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);
        Vector2 position = HandleUtility.WorldToGUIPoint(transform.position);
        float width = sceneIconSize * textureRect.width / Mathf.Max(textureRect.width, textureRect.height);
        float height = sceneIconSize * textureRect.height / Mathf.Max(textureRect.width, textureRect.height);

        GUI.color = renderer.color;
        GUI.DrawTextureWithTexCoords(new Rect(position.x - width * 0.5f,
            position.y - height * 0.5f, width, height), sprite.texture, uv, true);
    }
#endif

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
        Gather,
        Exit,
        Flee,
        Cautious
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

    public void SetHunterSearch()
    {
        SpriteRenderer.color = HunterGreen;
        if (currentState != IndicatorState.Patrol)
        {
            currentState = IndicatorState.Patrol;
            patrolTimer = 0f;
            SpriteRenderer.enabled = true; 
            SpriteRenderer.sprite = hunterPatrol;   
        }
    }

    public void SetHunterExit()
    {
        currentState = IndicatorState.Exit;
        SpriteRenderer.enabled = true;
        SpriteRenderer.sprite = hunterPatrol;
        SpriteRenderer.color = HunterGreen;
    }

    public void SetHunterFlee()
    {
        currentState = IndicatorState.Flee;
        SpriteRenderer.enabled = true;
        SpriteRenderer.sprite = hunterChase;
        SpriteRenderer.color = HunterWarning;
    }

    public void SetHunterCautious()
    {
        currentState = IndicatorState.Cautious;
        SpriteRenderer.enabled = true;
        SpriteRenderer.sprite = hunterPatrol;
        SpriteRenderer.color = HunterYellow;
    }

    public void SetHunterChase()
    {
        SpriteRenderer.color = HunterYellow;
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
        SpriteRenderer.color = HunterYellow;
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
        SpriteRenderer.color = HunterYellow;
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
        SpriteRenderer.color = HunterGreen;
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
        SpriteRenderer.color = HunterYellow;
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
