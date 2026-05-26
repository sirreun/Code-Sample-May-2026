using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keypad : Interactable
{
    [SerializeField] private GameObject affectedGameObject;
    private bool isGreen = false;

    /// <summary>
    /// Overrides the interact function from the Interactable base class.
    /// </summary>
    /// <param name="player"></param>
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
