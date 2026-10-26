using UnityEngine;

public class Bait : MonoBehaviour
{
    [Header("References")]
    private Boid assignedBoid;
    private Hunter owner;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    public bool IsDestroyed => currentHealth <= 0;
    public bool IsAvailable => isActiveAndEnabled && !IsDestroyed && assignedBoid == null;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
    }

    public void SetOwner(Hunter hunter)
    {
        owner = hunter;
    }

    public bool TryAssignBoid(Boid boid)
    {
        if (!IsAvailable || boid == null || boid.IsDead || !boid.isActiveAndEnabled)
        {
            return false;
        }

        assignedBoid = boid;
        return true;
    }

    public void ReleaseBoid(Boid boid)
    {
        if (assignedBoid == boid)
        {
            assignedBoid = null;
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDestroyed || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            DestroyObject();
        }
    }

    private void DestroyObject()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.RemoveBait();
        }
    }
}
