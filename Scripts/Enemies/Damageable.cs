using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class Damageable : NetworkBehaviour
{
    public int MaxHealth;
    public NetworkVariable<int> Health;
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
        Health = new NetworkVariable<int> (MaxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        Health.OnValueChanged += OnHealthChanged_CLIENT;

        if (this.gameObject.layer != LayerMask.NameToLayer("Damageable"))
        {
            Debug.LogWarning(this.gameObject + ": This object must be on layer: Damageable");
        }
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged_CLIENT;
    }

    public void TakeDamage_TO_SERVER(int amount)
    {
        TakeDamageServerRpc(amount);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount)
    {
        Health.Value -= amount;
    }   

    protected void OnHealthChanged_CLIENT(int previousHealth, int newHealth)
    {
        CheckHealth();
        ChangeSpeed();
    }


    public void CheckHealth()
    {
        if (Health.Value <= 0 && !DebugCantDie)
        {
            _HealthStatus = HealthStatus.Dead;
            OnDeathFunction.Invoke();
        }
        else if (Health.Value < BleedingThreshold)
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
