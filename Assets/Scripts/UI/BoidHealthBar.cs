using UnityEngine;
using UnityEngine.UI;

public class BoidHealthBar : MonoBehaviour
{
    [SerializeField] private Image fill;

    private Boid boid;

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