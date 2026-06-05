using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;

    [Header("Interactables")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask;

    [Space(5)]
    [Header("Attacks")]
    [SerializeField] private float attackDistance = 3f;
    [SerializeField] private LayerMask attackMask;

    private PlayerUI playerUI;
    private InputManager inputManager;
    private InventoryManager inventoryManager;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<PlayerManager>().cam;
        playerUI = GetComponent<PlayerUI>();
        inputManager = GetComponent<InputManager>();
        inventoryManager = GetComponent<InventoryManager>();

    }

    // Update is called once per frame
    void Update()
    {
        // Interactable UI is cleared when not looking at an interactable.
        playerUI.ClearText();

        UpdateDefaultInteractions();

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInformation;

        if (inventoryManager.HoldingItem())
        {
            if (inventoryManager.GetCurrentItemInfo())
            {
                if (inventoryManager.GetCurrentItemInfo()._Type == InventoryInteractable.IType.Weapon)
                {
                    //Debug.Log("Holding a weapon");
                    Weapon weapon = inventoryManager.GetCurrentSelectedItem().GetComponent<Weapon>();
                    ray = new Ray(cam.transform.position, cam.transform.forward);
                    if (Physics.Raycast(ray, out hitInformation, attackDistance, attackMask))
                    {
                        if (hitInformation.collider.GetComponent<Damageable>() != null)
                        {
                            if (hitInformation.collider.gameObject == this.gameObject)
                            {
                                // Can't damage self
                                return; 
                            }
                            //Debug.Log("Hitting damageable entity");
                            Damageable damageable = hitInformation.collider.GetComponent<Damageable>();
                            if (inputManager.AttackTriggered())
                            {
                                damageable.TakeDamage_TO_SERVER(weapon.Damage);
                            }
                        }
                    }
                }
            } 
        }
    }

    private void UpdateDefaultInteractions()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInformation;
        
        if (Physics.Raycast(ray, out hitInformation, interactDistance, interactMask))
        {
            if (hitInformation.collider.GetComponent<Interactable>() != null)
            {
                Interactable interactable = hitInformation.collider.GetComponent<Interactable>();

                InventoryItem inventoryItem = hitInformation.collider.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    if (playerUI.UpdateText(inventoryItem, interactable))
                    {
                        //Debug.Log("able to interact with this object");
                        if (inputManager.InteractTriggered())
                        {
                            interactable.Interact(this);
                        }
                    }
                }
                else
                {
                    playerUI.UpdateText(interactable);
                    //Debug.Log("interactable does not have an inventory item");

                    if (inputManager.InteractTriggered())
                    {
                        interactable.Interact(this);
                    }
                }
            }
        }
    }

    public Vector3 RaycastEndPoint(float distance)
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        return ray.GetPoint(distance);
    }
}
