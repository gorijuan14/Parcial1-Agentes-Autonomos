using UnityEngine;

public class HunterCollector : MonoBehaviour
{
    [SerializeField] private float collectionDuration = 2f;

    private bool isCollecting;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollecting)
        {
            return;
        }

        Boid boid = other.GetComponentInParent<Boid>();

        if (boid == null)
        {
            return;
        }

        StartCoroutine(CollectBoid(boid));
    }

    private System.Collections.IEnumerator CollectBoid(Boid boid)
    {
        isCollecting = true;

        Hunter hunter = GetComponentInParent<Hunter>();

        if (hunter != null)
        {
            hunter.StopMovement();
        }

        boid.Collect();

        yield return new WaitForSeconds(collectionDuration);

        boid.Disappear();

        isCollecting = false;
    }
}