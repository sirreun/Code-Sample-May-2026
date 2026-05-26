using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ElectricityManager : MonoBehaviour
{
    public static event Action UpdatedPowerSources; // Scripts that use power source locations subscribe to this event

    public List<Transform> PowerSources = new List<Transform>();

    public static ElectricityManager instance { get; private set; }

    void Awake()
    {
        instance = this;
    }

    public void AddPowerSource(Transform newTransform)
    {
        if (!PowerSources.Contains(newTransform))
        {
            PowerSources.Add(newTransform);
        }

        UpdatedPowerSources?.Invoke();
    }

    public void RemovePowerSource(Transform removedTransform)
    {
        PowerSources.Remove(removedTransform);

        UpdatedPowerSources?.Invoke();
    }

    public Transform ClosestPowerSource(Vector3 position, out bool foundPowerSource)
    {
        foundPowerSource = false;
        Transform output = null;

        float shortestDistance = -1;

        foreach (Transform powerSource in PowerSources)
        {
            float distance = Vector3.Distance(powerSource.position, position);

            if (shortestDistance < 0 || distance <= shortestDistance)
            {
                foundPowerSource = true;
                shortestDistance = distance;
                output = powerSource;
            }
        }

        return output;
    }
}
