using UnityEngine;
using System.Collections;

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

    private IEnumerator CollectBoid(Boid boid)
    {
        isCollecting = true;

        Hunter hunter = GetComponentInParent<Hunter>();
        HunterFSM hunterFSM = GetComponentInParent<HunterFSM>();

        if (hunter != null)
        {
            hunter.StopMovement();
            hunter.SetBehaviour(null);
        }

        boid.Collect();

        yield return new WaitForSeconds(collectionDuration);

        boid.Disappear();

        isCollecting = false;

        if (hunterFSM != null)
        {
            hunterFSM.ChangeState(
                new HunterPatrolState(hunterFSM)
            );
        }
    }
}
