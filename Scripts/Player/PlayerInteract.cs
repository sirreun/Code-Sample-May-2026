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
                                return; 
                            }
                            //Debug.Log("Hitting damageable entity");
                            Damageable damageable = hitInformation.collider.GetComponent<Damageable>();
                            if (inputManager.AttackTriggered())
                            {
                                damageable.TakeDamage(weapon.Damage);
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
        // Only continues to if statement if the ray hits something.
        if (Physics.Raycast(ray, out hitInformation, interactDistance, interactMask))
        {
            if (hitInformation.collider.GetComponent<Interactable>() != null)
            {
                Interactable interactable = hitInformation.collider.GetComponent<Interactable>();

                InventoryItem inventoryItem = hitInformation.collider.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    if (!inventoryItem.IsPickedUp_SERVER.Value)
                    {
                        playerUI.UpdateText(interactable);


                        if (inputManager.InteractTriggered())
                        {
                            // TODO: have options for if interact is server-side or client side
                            interactable.Interact(this);
                        }
                    }
                }
                else
                {
                    playerUI.UpdateText(interactable);


                    if (inputManager.InteractTriggered())
                    {
                        // TODO: have options for if interact is server-side or client side
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
