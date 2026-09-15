using UnityEngine;

public class HunterFSM : MonoBehaviour
{
    [Header("References")]
    private HunterState currentState;

    private void Start()
    {
        ChangeState(new HunterPatrolState(this));
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    public void ChangeState(HunterState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.Enter();
        }
    }
}