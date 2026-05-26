using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemInformation", menuName = "ScriptableObjects/Item Information")]
public class ItemInformationSO : ScriptableObject
{
    public string Name;
    public GameObject Prefab;
    public int Class = 1; // Range 1 - 7
    public Sprite Icon;
    public bool CanBeInHub = true;
    public bool CanBeInField = true;
    public bool isHeavyItem = false;
}
