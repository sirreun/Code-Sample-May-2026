using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Tooltip("The prompt that is displayed when the player looks at an interactable. The Start function adds the button to be pressed to the message.")]
    public string interactionPrompt;
    public bool ConditionalInteractable = false;
    [SerializeField] private bool wallColliderWarningResolved = false;

    void Start()
    {
        interactionPrompt = "[E] " + interactionPrompt;
#if UNITY_EDITOR
        if (!wallColliderWarningResolved)
        {
            Debug.LogWarning(gameObject.name + ": Back wall interactable collider required to make sure interaction prompt does not go thorugh walls");
        }
#endif
    }
    
    public virtual void Interact(PlayerInteract player)
    {
        
    }

    public virtual bool ConditionMet(PlayerInteract player)
    {
        return false;
    }
}
