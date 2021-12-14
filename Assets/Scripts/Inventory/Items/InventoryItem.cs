using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InventoryItem : ScriptableObject
{

    [SerializeField]
    protected string itemName;
    public string ItemName => itemName;

    [Tooltip("Value of the item for selling or buying")]
    [Range(1,10000)]
    [SerializeField]
    protected int itemValue;
    public int ItemValue => itemValue;

    public List<Quantity<InventoryItem>> disensambleGain;
    public List<Quantity<InventoryItem>> ensambleItemGain;

}
