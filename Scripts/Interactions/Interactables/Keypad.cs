using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Keypad : Interactable
{
    // The gameobject affected when the keypad is used.
    [SerializeField] private GameObject affectedGameObject;
    private bool isGreen = false;

    // Overrides the interact function from the Interactable base class.
    public override void Interact(PlayerInteract player)
    {
        //Debug.Log("Interacted with " + gameObject.name);

        isGreen = !isGreen;
        if (isGreen)
        {
            affectedGameObject.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        }
        else
        {
            affectedGameObject.GetComponent<Renderer>().material.color = new Color(255, 255, 255);
        }
        
    }
}
