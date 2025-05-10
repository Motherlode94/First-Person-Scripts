using UnityEngine;
using System.Collections.Generic;  // Ajout de cette ligne pour corriger l'erreur

public interface IEquipmentComponent
{
    void EquipItem(InventoryItem item);
    void UnequipItem(InventoryItem item);
    bool IsEquipped(InventoryItem item);
    InventoryItem GetEquippedItem(EquipmentSlot slot);
}

// Implémentation basique pour les tests
public class EquipmentComponent : MonoBehaviour, IEquipmentComponent
{
    private Dictionary<EquipmentSlot, InventoryItem> equippedItems = new Dictionary<EquipmentSlot, InventoryItem>();
    
    public void EquipItem(InventoryItem item)
    {
        if (item != null && item.equipSlot != EquipmentSlot.None)
        {
            equippedItems[item.equipSlot] = item;
            Debug.Log($"Equipped {item.displayName} in slot {item.equipSlot}");
        }
    }
    
    public void UnequipItem(InventoryItem item)
    {
        if (item != null && equippedItems.ContainsKey(item.equipSlot) && equippedItems[item.equipSlot] == item)
        {
            equippedItems.Remove(item.equipSlot);
            Debug.Log($"Unequipped {item.displayName} from slot {item.equipSlot}");
        }
    }
    
    public bool IsEquipped(InventoryItem item)
    {
        if (item != null && equippedItems.ContainsKey(item.equipSlot))
            return equippedItems[item.equipSlot] == item;
        return false;
    }
    
    public InventoryItem GetEquippedItem(EquipmentSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
            return equippedItems[slot];
        return null;
    }
}