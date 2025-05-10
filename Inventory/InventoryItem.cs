using UnityEngine;

public enum ItemType
{
    Consumable,
    Equipment,
    Quest,
    Material,
    Weapon,
    Ammo
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    [Header("Identification")]
    public string itemID;
    public string displayName;
    [TextArea(3, 8)]
    public string description;
    
    [Header("Visuals")]
    public Sprite icon;
    public GameObject itemModel; // Pour affichage 3D
    public GameObject dropPrefab; // Pour jeter l'item dans le monde
    
    [Header("Properties")]
    public ItemType itemType = ItemType.Consumable;
    public bool stackable = true;
    public int maxStack = 99;
    public float weight = 0.1f;
    public bool consumable = true;
    public bool isUsable = true;
    
    [Header("Effects")]
    public float effectValue = 10f; // Valeur de l'effet (ex: points de vie restaurés)
    
    [Header("Equipment")]
    public EquipmentSlot equipSlot = EquipmentSlot.None;
    
    [Header("Sound Effects")]
    public AudioClip pickupSound;
    public AudioClip useSound;
    public AudioClip equipSound;
}

public enum EquipmentSlot
{
    None,
    Head,
    Chest,
    Legs,
    Feet,
    Hands,
    MainHand,
    OffHand,
    Accessory1,
    Accessory2
}