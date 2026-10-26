using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BoidHealthBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [Header("Scene View")]
    [SerializeField, Min(8f)] private float sceneBarWidth = 50f;
    [SerializeField, Min(3f)] private float sceneBarHeight = 7f;

    private Boid boid;

#if UNITY_EDITOR
    public void DrawSceneOverlay(SceneView sceneView)
    {
        if (boid == null)
            boid = GetComponentInParent<Boid>();
        if (boid == null || fill == null)
            return;

        Camera camera = sceneView.camera;
        if (Vector3.Dot(camera.transform.forward, transform.position - camera.transform.position) <= 0f)
            return;

        Vector2 position = HandleUtility.WorldToGUIPoint(transform.position);
        Rect background = new Rect(position.x - sceneBarWidth * 0.5f,
            position.y - sceneBarHeight * 0.5f, sceneBarWidth, sceneBarHeight);
        Image backgroundImage = GetComponent<Image>();
        EditorGUI.DrawRect(background, backgroundImage != null ? backgroundImage.color : Color.black);
        float health = Mathf.Clamp01((float)boid.CurrentHealth / Mathf.Max(1, boid.MaxHealth));
        Rect foreground = new Rect(background.x + 1f, background.y + 1f,
            (background.width - 2f) * health, background.height - 2f);
        EditorGUI.DrawRect(foreground, fill.color);
    }
#endif

    private void Awake()
    {
        boid = GetComponentInParent<Boid>();
    }

    private void Update()
    {
        if (boid == null)
        {
            return;
        }

        fill.fillAmount =
            (float)boid.CurrentHealth / boid.MaxHealth;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
    }
}
