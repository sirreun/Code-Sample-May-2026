using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadiationSource : MonoBehaviour
{
    public CapsuleCollider radiation;
    public float radiationRadius = 20f;
    public float radiationStrength = 243f;

    // Start is called before the first frame update
    void Start()
    {
        // Set up collider for radiation
        if (gameObject.GetComponent<CapsuleCollider>() == null)
        {
            radiation = gameObject.AddComponent<CapsuleCollider>();
            radiation.height = radiationRadius;
            radiation.radius = radiationRadius;
            radiation.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("RadiationSource.cs: object already has a CapsuleCollider component.");
        }
        
    }

    public Vector3 GetPosition()
    {
        return gameObject.transform.position;
    }
}
