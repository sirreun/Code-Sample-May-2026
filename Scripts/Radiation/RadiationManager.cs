using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadiationManager : MonoBehaviour
{
    public static RadiationManager instance { get; private set; }

    public RadiationSource[] Sources;

    private void Awake()
    {
        instance = this;
        Sources = FindObjectsOfType<RadiationSource>();
    }
}
