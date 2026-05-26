using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnomolyContainmentUnit : MonoBehaviour
{
    [Range(0,1)]
    public float InsideContainerPercent = 0.8f;
    private bool containsAnomoly = false;

    public bool PickUpAnomoly(Anomoly anomoly)
    {
        if (!containsAnomoly)
        {
            GameObject anomolyGameObject = anomoly.gameObject;
            GameObject radiationSource = anomolyGameObject.transform.GetChild(0).gameObject;
            Destroy(radiationSource);
            
            Vector3 maxDimenisions = anomoly.MaxDimenisions;
            Destroy(anomoly);

            Vector3 acuDimensions = transform.localScale;
            Vector3 acuInsideDimensions = acuDimensions * InsideContainerPercent;

            float deltaX = 0;
            float deltaY = 0;
            float deltaZ = 0;

            if (maxDimenisions.x > acuInsideDimensions.x)
            {
                deltaX = maxDimenisions.x - acuInsideDimensions.x;
            }

            if (maxDimenisions.y > acuInsideDimensions.y)
            {
                deltaY = maxDimenisions.y - acuInsideDimensions.y;
            }

            if (maxDimenisions.z > acuInsideDimensions.z)
            {
                deltaZ = maxDimenisions.z - acuInsideDimensions.z;
            }

            float multiplier;

            if (deltaX >= deltaY && deltaX >= deltaZ)
            {   
                multiplier = acuInsideDimensions.x / maxDimenisions.x;
            }
            else if (deltaY >= deltaZ)
            {
                multiplier = acuInsideDimensions.y / maxDimenisions.y;
            }
            else
            {
                multiplier = acuInsideDimensions.y / maxDimenisions.y;
            }

            anomolyGameObject.transform.localScale *= multiplier;
            anomolyGameObject.transform.position = this.transform.position;
            anomolyGameObject.transform.rotation = this.transform.rotation;

            // TODO: needs to be done by the server
            anomolyGameObject.transform.SetParent(this.transform);

            containsAnomoly = true;
            return true;
        }
        else 
        {
            Debug.Log("Containment Unit Full");
            return false;
        }
    }
}
