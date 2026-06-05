using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatsElectricity : Enemy // TODO make new enemy template
{
    protected enum CustomState
    {
        TrackingElectricity
    }

    protected override void EnemyStart()
    {
        ElectricityManager.UpdatedPowerSources += UpdatingElectricityTracking;
    }

    protected override void EnemyUpdate()
    {

    }

    protected override void CustomStateUpdate()
    {
        TransformToDestinationANode();
    }

    public void UpdatingElectricityTracking()
    {
        if (!IsHost) return;

        trackingTransform = ElectricityManager.instance.ClosestPowerSource(this.gameObject.transform.position, out bool foundPowerSource);
        if (foundPowerSource)
        {
            if (currentState != State.Tracking && currentState != State.Attacking)
            {
                //Debug.Log("Tracking power sources");
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
                default:
                    break;
            }
        }
    }

    public override void UpdatingTarget()
    {
        base.UpdatingTarget();
    }

    public void JustBeforeDeath()
    {
        ElectricityManager.UpdatedPowerSources -= UpdatingElectricityTracking;
    }

    public override void OnDestroy()
    {
        ElectricityManager.UpdatedPowerSources -= UpdatingElectricityTracking;
        base.OnDestroy();
    }
}
