using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public List<GameObject> inventory = new List<GameObject>();

    private int currentInventorySlot = 1; // Ranges from 1 to 5. Defaults to slot one.
    private int maxInventoryItems = 5;
    [SerializeField] private Transform playerHandTransform;
    private float infrontOfPlayerModifier = 1f;

    [Header("Inventory UI")]
    [SerializeField] private List<GameObject> itemUI;
    [SerializeField] private PlayerUI playerUI;

    private Player player;

    // Start is called before the first frame update
    void Awake()
    {
        InitializeInventoryUI();
        playerUI = GetComponent<PlayerUI>();
        player = GetComponent<Player>();
    }

    #region /// ITEM INFO FUNCTIONS ///

    public GameObject GetCurrentSelectedItem()
    {
        if (currentInventorySlot <= inventory.Count)
        {
            return inventory[currentInventorySlot - 1];
        }
        return null;
    }

    public bool HoldingItem()
    {
        if (currentInventorySlot > inventory.Count)
        {
            return false;
        }
        return true;
    }

    public bool HasAnomoly()
    {
        foreach (GameObject item in inventory)
        {
            AnomolyContainmentUnit ACU = item.GetComponent<AnomolyContainmentUnit>();
            if (ACU != null)
            {
                return ACU.ContainsAnomoly();
            }
        }

        return false;
    }

    public InventoryInteractable GetCurrentItemInfo()
    {
        return GetItemInfoFromIndex(currentInventorySlot - 1);
    }
    
    private InventoryInteractable GetItemInfoFromIndex(int index)
    {
        if (index >= inventory.Count)
        {
            return null;
        }

        if (inventory[index] == null)
        {
            return null;
        }

        try 
        {
            return inventory[index].GetComponent<InventoryInteractable>();
        }
        catch (UnassignedReferenceException)
        {
            Debug.LogWarning("InventoryManager: item " + (index + 1) + " does not have the required InventoryInteractable");
            return null;
        }

    }

    #endregion


    /// <summary>
    /// Tries to add the gameobject to the inventory.
    /// </summary>
    /// <param name="itemObject"></param>
    /// <returns>Whether the item was able to be added to the inventory.</returns>
    public bool AddItemToInventory(GameObject itemObject)
    {
        if(itemObject == null)
        {
            Debug.LogWarning("Item " + itemObject.gameObject.name + " does not have an InventoryItem script attached to it. Please add it.");
            UpdateInventoryText();
            return false;
        }

        if (inventory.Count < maxInventoryItems)
        {
            InventoryItem inventoryItem = itemObject.GetComponent<InventoryItem>();
            InventoryInteractable inventoryInteractable = itemObject.GetComponent<InventoryInteractable>();

            InventoryNetworkUtilities.instance.SetParentTransform_TO_SERVER(inventoryItem.GUID_SERVER.Value, player.ID);
            itemObject.GetComponent<Collider>().isTrigger = true;
            
            inventory.Add(itemObject);

            // Only show item if currently on the item slot
            if (currentInventorySlot == inventory.Count)
            {
                itemObject.SetActive(true);
            }
            else
            {
                itemObject.SetActive(false);
            }

            inventoryItem.SetOwner(playerUI);
            
            if (currentInventorySlot == inventory.Count)
            {
                if (inventoryInteractable)
                {
                    inventoryInteractable.ItemSelected();
                }
            }

            InitializeUI();
            UpdateInventoryText();
            return true;
        }
        else
        {
            Debug.LogWarning("Inventory is full, cannot pick up item.");
            InitializeUI();
            UpdateInventoryText();
            return false;
        }
    }

    public void RemoveCurrentItemFromInventory()
    {
        RemoveItemFromInventory(currentInventorySlot - 1);
    }

    private void RemoveItemFromInventory(int index)
    {
        InventoryItem currentItem = inventory[index].GetComponent<InventoryItem>();
        InventoryInteractable currentInteractable = GetItemInfoFromIndex(index);
        if (currentInteractable)
        {
            currentInteractable.ItemUnselected();
        }
        currentItem.ClearOwner();

        if (currentInventorySlot > 0 && currentInventorySlot <= maxInventoryItems)
        {
            // Only drop items and not empty slots.
            if (inventory.Count >= currentInventorySlot && inventory.Count != 0)
            {
                //Debug.Log("Dropping Item...");
                // Remove item from inventory list
                //string droppedObjectName = inventory[currentInventorySlot - 1].gameObject.name; 

                Vector3 newPosition = this.gameObject.GetComponent<PlayerInteract>().RaycastEndPoint(infrontOfPlayerModifier);

                GameObject itemObject = inventory[currentInventorySlot - 1].gameObject;
                InventoryNetworkUtilities.instance.ResetParentTransform_TO_SERVER(itemObject.GetComponent<InventoryItem>().GUID_SERVER.Value);
                itemObject.transform.position = newPosition;
                itemObject.transform.rotation = Quaternion.identity;
                itemObject.GetComponent<Collider>().isTrigger = false;

                itemObject.SetActive(true);

                inventory.RemoveAt(currentInventorySlot - 1);
                //Debug.Log("Dropped item " + droppedObjectName + ".");
            }

        }
        InitializeUI();
        UpdateInventoryText();
    }

    public void ClearInventory()
    {
        for(int i = 0; i < inventory.Count; i++)
        {
            RemoveItemFromInventory(i);
        }
    }

    /// Given -1 or +1 determines if we are adding or subtracting the current inventory slot.
    /// Returns the new current inventory slot.
    public int ChangeCurrentInventorySlot(float direction)
    {
        InventoryInteractable previousItem = GetItemInfoFromIndex(currentInventorySlot - 1);
        if (previousItem)
        {
            previousItem.ItemUnselected();
        }

        //Debug.Log("Scroll Direction: " + direction);
        if (direction == 0)
        {
            Debug.Log("InventoryManager.cs: ChangeCurrentInventorySlot: direction given is 0, which is useless.");
            return -1;
        }

        if (direction > 0)
        {
            // Scrolls Down.
            if (currentInventorySlot == maxInventoryItems)
            {   
                SelectItem(1);
                currentInventorySlot = 1;
            }
            else
            {
                SelectItem(currentInventorySlot + 1);
                currentInventorySlot += 1;
            }
        }
        else
        {
            // Scrolls Up.
            if (currentInventorySlot == 1)
            {
                SelectItem(maxInventoryItems);
                currentInventorySlot = maxInventoryItems;
            }
            else
            {
                SelectItem(currentInventorySlot - 1);
                currentInventorySlot -= 1;
            }
        }

        InventoryInteractable currentItem = GetItemInfoFromIndex(currentInventorySlot - 1);
        if (currentItem)
        {
            currentItem.ItemSelected();
        }
        InitializeUI();
        //Debug.Log("Current inventory slot is No. " + currentInventorySlot);
        return currentInventorySlot;
    }

    /// Changes inventory slot based on the inputed number.
    public int ChangeCurrentInventorySlotWithNumber(int itemSlot)
    {
        InventoryInteractable previousItem = GetItemInfoFromIndex(currentInventorySlot - 1);
        if (previousItem)
        {
            previousItem.ItemUnselected();
        }

        if (itemSlot != currentInventorySlot)
        {
            SelectItem(itemSlot);
            currentInventorySlot = itemSlot;
        }

        InventoryInteractable currentItem = GetItemInfoFromIndex(currentInventorySlot - 1);
        if (currentItem)
        {
            currentItem.ItemSelected();
        }

        InitializeUI();
        return currentInventorySlot;
    }

    private void InitializeUI()
    {
        //Debug.LogWarning("initing interactable ui for item " + currentInventorySlot);
        try 
        {
            if (currentInventorySlot <= inventory.Count)
            {
                if (inventory[currentInventorySlot - 1] != null)
                {
                    InventoryInteractable currentItemInfo = inventory[currentInventorySlot - 1].GetComponent<InventoryInteractable>(); 
                    currentItemInfo.InitializeInteractableUI();
                }
                else
                {
                    playerUI.RemoveInteractactableUI();
                }
                
            }
            else 
            {
                playerUI.RemoveInteractactableUI();
            }
        }
        catch (UnassignedReferenceException)
        {
            playerUI.RemoveInteractactableUI();
            Debug.LogWarning("ERROR: InventoryManager: item in inventory slot " + currentInventorySlot + " does not have class inventory interactable. Please add one.");
        }
    }

    #region /// INVENTORY UI ANIMATIONS ///
    private void InitializeInventoryUI()
    {
        currentInventorySlot = 1;

        // Select first item slot.
        itemUI[0].transform.GetChild(0).gameObject.SetActive(false);
        itemUI[0].transform.GetChild(1).gameObject.SetActive(true);

        // Only shows selected item
        if (inventory.Count >= 1)
        {
            inventory[0].SetActive(true);
        }

        // Deselect other item slots.
        for (int i = 1; i < maxInventoryItems; i++)
        {
            itemUI[i].transform.GetChild(0).gameObject.SetActive(true);
            itemUI[i].transform.GetChild(1).gameObject.SetActive(false);

            // Hides unselected items
            if (inventory.Count >= i)
            {
                inventory[i].SetActive(false);
            }
        }


        UpdateInventoryText();
    }

    /// Instructions: Must be called before currentInventorySlot is changed.
    private void SelectItem(int inventorySlot)
    {
        //Debug.Log("new inventory slot: " + inventorySlot + ", old inventory slot: " + currentInventorySlot);
        // Select new slot
        itemUI[inventorySlot - 1].transform.GetChild(0).gameObject.SetActive(false);
        itemUI[inventorySlot - 1].transform.GetChild(1).gameObject.SetActive(true);
        
        // Shows item (TODO: add switch item animation)
        if (inventory.Count >= inventorySlot && inventorySlot != 0)
        {
            inventory[inventorySlot - 1].SetActive(true);
        }
        

        // Deselect old slot
        itemUI[currentInventorySlot - 1].transform.GetChild(0).gameObject.SetActive(true);
        itemUI[currentInventorySlot - 1].transform.GetChild(1).gameObject.SetActive(false);

        if (inventory.Count >= currentInventorySlot && inventory.Count != 0)
        {
            if (inventory[currentInventorySlot - 1] != null)
            {
                inventory[currentInventorySlot - 1].SetActive(false);
            }
            
        }
    }

    /// Updates the text in the inventory.
    /// Is called whenever an item is added or removed from the inventory, or when Awake.
    private void UpdateInventoryText()
    {
        //Debug.Log("Updating inventory text.");
        int count = inventory.Count; // Only works since new items are always added to the bottom of the list, and when removed the list moves up

        for (int i = 0; i < maxInventoryItems; i++)
        {
            if (i < count)
            {
                itemUI[i].transform.Find("InventoryText").gameObject.GetComponent<TextMeshProUGUI>().text = inventory[i].gameObject.GetComponent<InventoryItem>().ItemInformation.Name;
            }
            else
            {
                itemUI[i].transform.Find("InventoryText").gameObject.GetComponent<TextMeshProUGUI>().text = "--";
            }
        }
    }
    #endregion

    // Calls the item interact function for the currently held object
    public void ItemInteract()
    {
        if (inventory.Count >= currentInventorySlot)
        {
            inventory[currentInventorySlot - 1].GetComponent<InventoryInteractable>().ItemInteract();
        }
    }
}
