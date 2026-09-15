using UnityEngine;

public class Bait : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;

    private int currentHealth;

    public bool IsDestroyed => currentHealth <= 0;

    private Boid assignedBoid;

    public bool IsAvailable => assignedBoid == null;

    private void Start()
    {
        currentHealth = maxHealth;
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

        Debug.Log(
            $"{name} recibió {damage} de daño. HP: {currentHealth}"
        );

        if (currentHealth <= 0)
        {
            DestroyObject();
        }
    }

    private void DestroyObject()
    {
        Debug.Log($"{name} fue destruido.");

        Destroy(gameObject);
    }
}