// Assets/Scripts/Inventory/InventoryManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's inventory, handling items, stacking, and interactions
/// </summary>
public class InventoryManager : MonoBehaviour
{
    #region Singleton
    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
    
    #region Item Stack Structure
    [Serializable]
    public struct ItemStack
    {
        public InventoryItem item;
        public int count;
    }
    #endregion

    #region Properties and Fields
    public int capacity = 20;
    public List<ItemStack> items = new List<ItemStack>();
    public event Action OnInventoryChanged;
    #endregion

    #region Item Management Methods
    /// <summary>
    /// Adds an item to the inventory, stacking if possible
    /// </summary>
    /// <returns>True if the item was added successfully</returns>
    public bool AddItem(InventoryItem newItem, int amount = 1)
    {
        if (newItem == null)
            return false;

        // Stackable items: try to add to existing stacks
        if (newItem.stackable)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item == newItem && items[i].count < newItem.maxStack)
                {
                    int space = newItem.maxStack - items[i].count;
                    int toAdd = Mathf.Min(space, amount);
                    var stack = items[i];
                    stack.count += toAdd;
                    items[i] = stack;
                    amount -= toAdd;
                    if (amount <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        // Add new stacks if capacity allows
        while (amount > 0 && items.Count < capacity)
        {
            int toPut = newItem.stackable ? Mathf.Min(amount, newItem.maxStack) : 1;
            items.Add(new ItemStack { item = newItem, count = toPut });
            amount -= toPut;
        }

        OnInventoryChanged?.Invoke();
        return amount <= 0;
    }

    /// <summary>
    /// Removes an item from the inventory
    /// </summary>
    /// <returns>True if the required amount was successfully removed</returns>
    public bool RemoveItem(InventoryItem itemToRemove, int amount = 1)
    {
        if (itemToRemove == null)
            return false;

        for (int i = items.Count - 1; i >= 0 && amount > 0; i--)
        {
            if (items[i].item == itemToRemove)
            {
                int toTake = Mathf.Min(items[i].count, amount);
                var stack = items[i];
                stack.count -= toTake;
                amount -= toTake;
                if (stack.count <= 0)
                    items.RemoveAt(i);
                else
                    items[i] = stack;
            }
        }

        OnInventoryChanged?.Invoke();
        return amount <= 0;
    }

    /// <summary>
    /// Checks if the inventory contains a specific item
    /// </summary>
    public bool HasItem(InventoryItem checkItem, int amount = 1)
    {
        if (checkItem == null)
            return false;

        int total = 0;
        foreach (var stack in items)
        {
            if (stack.item == checkItem)
                total += stack.count;
        }
        return total >= amount;
    }

    /// <summary>
    /// Uses an item, applying its effect and consuming it if necessary
    /// </summary>
    public bool UseItem(InventoryItem itemToUse)
    {
        if (itemToUse == null) return false;
        
        // Find the item in the inventory
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item == itemToUse)
            {
                // Apply the item effect
                bool used = ApplyItemEffect(itemToUse);
                
                if (used)
                {
                    // If the item is consumable, reduce its quantity
                    if (itemToUse.consumable)
                    {
                        var stack = items[i];
                        stack.count--;
                        
                        if (stack.count <= 0)
                            items.RemoveAt(i);
                        else
                            items[i] = stack;
                    }
                    
                    // Notify of the change
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                return false;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Drops an item into the world
    /// </summary>
    public bool DropItem(InventoryItem itemToDrop, int amount = 1)
    {
        if (itemToDrop == null) return false;
        
        bool removed = RemoveItem(itemToDrop, amount);
        
        if (removed)
        {
            // Spawn a physical object in the world
            SpawnDroppedItem(itemToDrop, amount);
        }
        
        return removed;
    }

    /// <summary>
    /// Swaps two items in the inventory
    /// </summary>
    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Count || toIndex < 0 || toIndex >= capacity)
            return;
            
        // If the destination is empty and outside the current list
        if (toIndex >= items.Count)
        {
            // Add empty slots if needed
            while (items.Count <= toIndex)
            {
                items.Add(new ItemStack { item = null, count = 0 });
            }
        }
        
        // Swap the items
        ItemStack temp = items[fromIndex];
        
        if (toIndex < items.Count)
        {
            items[fromIndex] = items[toIndex];
            items[toIndex] = temp;
        }
        else
        {
            // Move to an empty slot
            items[fromIndex] = new ItemStack { item = null, count = 0 };
            items[toIndex] = temp;
        }
        
        // Clean up empty slots at the end of the list
        CleanEmptySlots();
        
        // Notify of the change
        OnInventoryChanged?.Invoke();
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Apply the effect of an item
    /// </summary>
    private bool ApplyItemEffect(InventoryItem item)
    {
        // Apply different effects based on item type
        switch (item.itemType)
        {
            case ItemType.Consumable:
                // Example: Heal the player
                var healthComp = PlayerManager.Instance?.GetHealthComponent();
                if (healthComp != null)
                {
                    // Adjust health based on effect value
                    try {
                        // Use reflection to handle different health systems
                        var method = healthComp.GetType().GetMethod("AddHealth") ?? 
                                    healthComp.GetType().GetMethod("Heal");
                        
                        if (method != null) {
                            method.Invoke(healthComp, new object[] { item.effectValue });
                            return true;
                        }
                    }
                    catch (Exception e) {
                        Debug.LogWarning($"Failed to apply health effect: {e.Message}");
                    }
                }
                break;
                
            case ItemType.Equipment:
                // Equip the item
                var equipmentComp = PlayerManager.Instance?.GetComponent<IEquipmentComponent>();
                if (equipmentComp != null)
                {
                    equipmentComp.EquipItem(item);
                    return true;
                }
                break;
                
            case ItemType.Quest:
                // Quest items are generally not usable
                return false;
                
            default:
                return false;
        }
        
        return false;
    }

    /// <summary>
    /// Clean up empty slots from the inventory
    /// </summary>
    private void CleanEmptySlots()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].item == null || items[i].count <= 0)
                items.RemoveAt(i);
        }
    }

    /// <summary>
    /// Spawn a dropped item in the world
    /// </summary>
    private void SpawnDroppedItem(InventoryItem item, int amount)
    {
        if (item.dropPrefab == null)
        {
            Debug.LogWarning($"[InventoryManager] No drop prefab for {item.displayName}");
            return;
        }
        
        // Find position and orientation for the drop
        Vector3 dropPosition = GetDropPosition();
        Quaternion dropRotation = Quaternion.identity;
        
        // Instantiate the prefab
        GameObject droppedItem = Instantiate(item.dropPrefab, dropPosition, dropRotation);
        
        // Configure the collectible if present
        Collectible collectible = droppedItem.GetComponent<Collectible>();
        if (collectible != null)
        {
            collectible.amount = amount;
            collectible.itemID = item.itemID;
            collectible.inventoryItem = item;
        }
    }

    /// <summary>
    /// Get the position to drop an item
    /// </summary>
    private Vector3 GetDropPosition()
    {
        // Find the player camera
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            return transform.position + Vector3.forward;
        }
        
        // Calculate a position in front of the player
        Vector3 dropDirection = playerCamera.transform.forward;
        Vector3 dropPosition = playerCamera.transform.position + dropDirection * 2f;
        
        // Optional: Raycast to avoid going through walls
        if (Physics.Raycast(playerCamera.transform.position, dropDirection, out RaycastHit hit, 2f))
        {
            dropPosition = hit.point - dropDirection * 0.3f; // Slightly back from the impact point
        }
        
        return dropPosition;
    }
    #endregion
}