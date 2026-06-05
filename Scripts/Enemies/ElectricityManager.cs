using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ElectricityManager : MonoBehaviour
{
    public static ElectricityManager instance { get; private set; }

    public static event Action UpdatedPowerSources;

    public List<Transform> PowerSources = new List<Transform>();

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

    public void RemovePowerSource(Transform transform)
    {
        PowerSources.Remove(transform);

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
