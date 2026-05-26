using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase instance { get; private set; }

    // Uses GUID from InventoryItem for dictionaries
    private Dictionary<FixedString64Bytes, ItemInformationSO> itemInformationDict = new Dictionary<FixedString64Bytes, ItemInformationSO>();

    private Dictionary<FixedString64Bytes, Transform> items;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("ItemDatabase: found another instance of ItemDatabase, destroying new one.");
            Destroy(this);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        items = new Dictionary<FixedString64Bytes, Transform>();
        InventoryItem[] inventoryItems = FindObjectsOfType<InventoryItem>();

        foreach (InventoryItem item in inventoryItems)
        {
            if (TryAddItemTransform(item, true))
            {

            }
            else
            {
                Debug.LogWarning("Item Transform Database init: duplicate item with guid: " + item.GUID_SERVER);
            }

            if (TryAddItemInformation(item, true))
            {

            }
            else
            {
                Debug.LogWarning("Item Information Database init: duplicate item with guid: " + item.GUID_SERVER);
            }
        }

        //Debug.Log("item information in dict: ");
        foreach (FixedString64Bytes guid in itemInformationDict.Keys)
        {
            Debug.Log(guid);
        }

        //Debug.Log("items in list: ");
        foreach (FixedString64Bytes guid in items.Keys)
        {
            Debug.Log(guid);
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }

    /// <summary>
    /// When an item is spawned by the server, this needs to be called to add the item to the database.
    /// </summary>
    public void OnItemSpawned(InventoryItem item)
    {
        TryAddItemInformation(item);
        TryAddItemTransform(item);
    }

    public bool TryGetItemTransform(FixedString64Bytes guid, out Transform itemTransform)
    {
        if (string.IsNullOrEmpty(name))
        {
            itemTransform = null;
            return false;
        }

        return items.TryGetValue(guid, out itemTransform);
    }

    public bool TryAddItemTransform(InventoryItem item, bool initing = false)
    {
        FixedString64Bytes guidUsed = new FixedString64Bytes();
        if (initing)
        {
            guidUsed = item.guid;
        }
        else
        {
            guidUsed = item.GUID_SERVER.Value;
        }

        if (item.transform != null)
        {
            if (!items.ContainsValue(item.transform))
            {
                items.Add(guidUsed, item.transform);
                return true;
            }
        }

        return false;
    }

    public bool TryGetItemInformation(FixedString64Bytes guid, out ItemInformationSO itemInformation)
    {
        if (string.IsNullOrEmpty(name))
        {
            itemInformation = null;
            return false;
        }

        return itemInformationDict.TryGetValue(guid, out itemInformation);
    }

    public bool TryAddItemInformation(InventoryItem item, bool initing = false)
    {
        FixedString64Bytes guidUsed = new FixedString64Bytes();
        if (initing)
        {
            guidUsed = item.guid;
        }
        else
        {
            guidUsed = item.GUID_SERVER.Value;
        }

        if (!itemInformationDict.ContainsValue(item.ItemInformation) && item.ItemInformation != null)
        {
            itemInformationDict.Add(guidUsed, item.ItemInformation);
            return true;
        }

        if (item.ItemInformation == null)
        {
            Debug.Log("Item object is missing ItemInformationSO");
        }

        return false;
    }
}
