using UnityEngine;

public class Bait : MonoBehaviour
{
    [Header("References")]
    private Boid assignedBoid;
    private Hunter owner;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    public bool IsDestroyed => currentHealth <= 0;
    public bool IsAvailable => assignedBoid == null;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void SetOwner(Hunter hunter)
    {
        owner = hunter;
    }

    public bool TryAssignBoid(Boid boid)
    {
        if (assignedBoid != null)
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
        if (IsDestroyed)
        {
            return;
        }

        currentHealth -= damage;

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