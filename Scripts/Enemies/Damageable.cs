using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
    public int MaxHealth;
    public int Health;
    public bool CanBleed = false;
    public int BleedingThreshold = 30;
    [HideInInspector]
    public HealthStatus _HealthStatus = HealthStatus.Healthy;
    public UnityEvent OnDeathFunction;
    public bool DebugCantDie = false;

    public enum HealthStatus
    {
        Healthy,
        Bleeding,
        Dead
    }

    void Awake()
    {
        Health = MaxHealth;

        if (this.gameObject.layer != LayerMask.NameToLayer("Damageable"))
        {
            Debug.LogWarning(this.gameObject + ": This object must be on layer: Damageable");
        }
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        CheckHealth();
        ChangeSpeed();
    }

    public void CheckHealth()
    {
        if (Health <= 0 && !DebugCantDie)
        {
            _HealthStatus = HealthStatus.Dead;
            OnDeathFunction.Invoke();
        }
        else if (Health < BleedingThreshold)
        {
            _HealthStatus = HealthStatus.Bleeding;
        }
        else
        {
            _HealthStatus = HealthStatus.Healthy;
        }
    }

    protected virtual void ChangeSpeed()
    {

    }
}
