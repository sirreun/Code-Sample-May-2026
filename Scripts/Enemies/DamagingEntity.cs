using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagingEntity : MonoBehaviour
{
    [SerializeField] private int attackDamage = 0;
    [SerializeField] private float attackTimer = 10f;
    private bool attackTimeOut = false;

    public float AttackDistance = 3f;
    [SerializeField] private LayerMask mask;

    public bool Attack(PlayerManager player)
    {
        if (!attackTimeOut)
        {
            StartCoroutine(AttackTimer());
            player.TakeDamage(attackDamage);

            if (player._HealthStatus == Damageable.HealthStatus.Dead)
            {
                return true;
            }
        }
        
        return false;
    }

    private IEnumerator AttackTimer()
    {
        attackTimeOut = true;
        yield return new WaitForSeconds(attackTimer);
        attackTimeOut = false;
    }
}
