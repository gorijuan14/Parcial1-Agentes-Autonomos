using UnityEngine;

public class Hunter : Agent
{
    [Header("Attack")]
    [SerializeField] private float tba = 2f;
    [SerializeField] private float rangeAttackRadius = 10f;
    [SerializeField] private float meleeAttackRadius = 2f;

    public float TBA => tba;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
}