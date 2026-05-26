using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatsElectricity : Enemy
{
    protected enum CustomState
    {
        TrackingElectricity
    }

    protected override void EnemyStart()
    {
        // Subscribe to ElectricityManager
        ElectricityManager.UpdatedPowerSources += UpdatingElectricityTracking;
    }

    protected override void EnemyUpdate()
    {

    }

    /// <summary>
    /// EatsElectricity enemy has one custom state: tracking electricity.
    /// </summary>
    protected override void CustomStateUpdate()
    {
        TransformToDestinationANode();
    }

    public void UpdatingElectricityTracking()
    {
        trackingTransform = ElectricityManager.instance.ClosestPowerSource(this.gameObject.transform.position, out bool foundPowerSource);

        if (foundPowerSource)
        {
            if (currentState != State.Tracking && currentState != State.Attacking)
            {
                Debug.Log("Tracking power sources");
                ChangeState(State.Custom);
            }   
        }
        else
        {
            switch (currentState)
            {
                case State.Custom:
                    ChangeState(State.Wandering);
                    break;
            }
        }
    }

    public override void UpdatingTarget()
    {
        base.UpdatingTarget();
    }

    public void OnDestroy()
    {
        // Unsubscribe to ElectrictyManager
        ElectricityManager.UpdatedPowerSources -= UpdatingElectricityTracking;
    }
}
