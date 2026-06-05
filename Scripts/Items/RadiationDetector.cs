using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RadiationDetector : InventoryInteractable
{
    private Camera cam;
    [SerializeField] private LayerMask mask; // Should always be Radiation
    private Vector3 playerPosition;

    public float noiseGeneration = 0.02f; // by default is 2%, how much noise is generated
    public float readingReliability = 85f; // by default is 85%, how reliable the detector readings are
    public float detectionDistance = 15f; // by default is 200
    public float ReadingFrequency = 0.1f;
    private bool waitingForReading = false;
    private const float maxRadiationStrength = 243f;
    private float sourceIncrease = 1.05f; // How much a reading increases by when you are looking at it

    private float reading;

    private bool radiationDetectorOn = false;

    protected override void InventoryInteractableUpdate()
    {
        if (itemOn && !waitingForReading)
        {
            GetRadiationReading();
            StartCoroutine(WaitForNewReading());
        }
    }

    private IEnumerator WaitForNewReading()
    {
        waitingForReading = true;
        DebugChangeColor();
        yield return new WaitForSeconds(ReadingFrequency);

        waitingForReading = false;
        DebugChangeColor();
    }

    /// <summary>
    /// Put in Item Unselected Functions
    /// </summary>
    public void HideGraph()
    {
        if (!CheckIfOwned())
        {
            return;
        }

        inventoryItem.ownerPlayerUI.HideRadiationGraph();
    }
    
    /// <summary>
    /// Put in Item Selected Functions
    /// </summary>
    public void ShowGraph()
    {
        if (!CheckIfOwned())
        {
            return;
        }

        inventoryItem.ownerPlayerUI.ShowRadiationGraph();
    }

    private void GetRadiationReading()
    {
        if (!CheckIfOwned())
        {
            return;
        }

        reading = 0f; // reset value

        // Determine player distance from all radiation sources, and find which ones are near enough to the player.
        float distance;
        playerPosition = inventoryItem.ownerPlayerUI.transform.position;
        cam = inventoryItem.ownerPlayerUI._Camera;
        foreach (RadiationSource source in RadiationManager.instance.Sources)
        {
            Vector3 sourcePosition = source.GetPosition();

            // 3D distance formula
            distance = Mathf.Pow(sourcePosition.x - playerPosition.x, 2) + Mathf.Pow(sourcePosition.y - playerPosition.y, 2) + Mathf.Pow(sourcePosition.z - playerPosition.z, 2);
            distance = Mathf.Sqrt(distance);

            // Check if detector would have picked up the source
            if (distance < detectionDistance)
            {
                //Debug.Log("In detection distance: " + distance);
                if (distance == 0f)
                {
                    reading += source.radiationStrength;
                }
                else
                {
                    reading += source.radiationStrength * (source.radiationRadius + detectionDistance)/Mathf.Pow(distance, 3);
                }
                //Debug.Log("Originial Reading: " + reading);
            }
        }


        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInformation;

        // Use the ray to determine if the player is facing towards the source
        if (Physics.Raycast(ray, out hitInformation, detectionDistance, mask))
        {
            if(hitInformation.collider.GetComponent<RadiationSource>() != null)
            {
                // Get radiation reading
                RadiationSource source = hitInformation.collider.GetComponent<RadiationSource>();

                // Determine the angle from which you are looking away from the source. Use this value to make the sourceIncrease modifier.
                // 3D distance formula
                Vector3 sourcePosition = source.GetPosition();
                distance = Mathf.Pow(sourcePosition.x - playerPosition.x, 2) + Mathf.Pow(sourcePosition.y - playerPosition.y, 2) + Mathf.Pow(sourcePosition.z - playerPosition.z, 2);
                distance = Mathf.Sqrt(distance);
                
                float height = Mathf.Pow(distance, 2) - Mathf.Pow(hitInformation.distance, 2);
                float maxHeight = source.radiationRadius;
                if (height == 0f)
                {
                    sourceIncrease = 1f;
                }
                else
                {
                    sourceIncrease = height/Mathf.Pow(maxHeight, 2) + 1f;
                }
                
                float signalStrength = sourceIncrease * (source.radiationStrength/maxRadiationStrength);

                if (Random.Range(1f, 100f) > readingReliability)
                {
                    // Unreliable reading, modify based on noise generation
                    float noise = Random.Range(1f - noiseGeneration, 1f + noiseGeneration);
                    reading = reading * noise * signalStrength;
                }

            }
        }
        else
        {
            // Adds random reading noise
            if (Random.Range(1f, 100f) > readingReliability)
            {
                // Unreliable reading, modify based on noise generation
                float noise = Random.Range(1f - noiseGeneration, 1f + noiseGeneration);
                reading = reading * noise;
            }
            
        }

        if (reading > 243f)
        {
            float noise = 1f;
            if (Random.Range(1f, 100f) > readingReliability)
            {
                // Unreliable reading, modify based on noise generation
                noise = Random.Range(1f - (noiseGeneration/2f), 1f);
                reading = reading * noise;
            }
            reading = 243f * noise;
        }

        //Debug.Log("Radiation Reading: " + reading);
        inventoryItem.ownerPlayerUI.GetRadiationGraph().AddData(reading);
    }

    private void DebugChangeColor()
    {
        var _renderer = this.gameObject.GetComponent<Renderer>();
        if (radiationDetectorOn)
        {
            _renderer.material.color = Color.green;
        }
        else
        {
            _renderer.material.color = Color.gray;
        }
    }
}
