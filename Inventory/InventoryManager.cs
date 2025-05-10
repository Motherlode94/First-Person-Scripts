// Assets/Scripts/Inventory/InventoryManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Serializable]
    public struct ItemStack
    {
        public InventoryItem item;
        public int count;
    }

    public int capacity = 20;
    public List<ItemStack> items = new List<ItemStack>();

    public event Action OnInventoryChanged;

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

    // Utiliser un item
    public bool UseItem(InventoryItem itemToUse)
    {
        if (itemToUse == null) return false;
        
        // Trouver l'item dans l'inventaire
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item == itemToUse)
            {
                // Appliquer l'effet de l'item
                bool used = ApplyItemEffect(itemToUse);
                
                if (used)
                {
                    // Si l'item est consommable, réduire sa quantité
                    if (itemToUse.consumable)
                    {
                        var stack = items[i];
                        stack.count--;
                        
                        if (stack.count <= 0)
                            items.RemoveAt(i);
                        else
                            items[i] = stack;
                    }
                    
                    // Notifier du changement
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                return false;
            }
        }
        
        return false;
    }

    // Appliquer l'effet de l'item
    private bool ApplyItemEffect(InventoryItem item)
    {
        // Selon le type d'item, appliquer différents effets
        switch (item.itemType)
        {
            case ItemType.Consumable:
                // Exemple: Soigner le joueur en utilisant IHealthManager
                var healthManager = PlayerManager.Instance?.GetHealthComponent();
                if (healthManager != null)
                {
                    // Tenter d'appeler une méthode de guérison (AddHealth, Heal, etc.)
                    try {
                        var method = healthManager.GetType().GetMethod("AddHealth") ?? 
                                   healthManager.GetType().GetMethod("Heal");
                                   
                        if (method != null) {
                            method.Invoke(healthManager, new object[] { item.effectValue });
                            return true;
                        }
                    }
                    catch (Exception ex) {
                        Debug.LogWarning($"[InventoryManager] Impossible d'appliquer l'effet de santé: {ex.Message}");
                    }
                }
                break;
                
            case ItemType.Equipment:
                // Équiper l'item
                var equipmentComp = PlayerManager.Instance?.GetComponent<IEquipmentComponent>();
                if (equipmentComp != null)
                {
                    equipmentComp.EquipItem(item);
                    return true;
                }
                break;
                
            case ItemType.Quest:
                // Les objets de quête ne sont généralement pas utilisables
                return false;
                
            // Autres types selon votre jeu
            default:
                return false;
        }
        
        return false;
    }

    // Jeter un item
    public bool DropItem(InventoryItem itemToDrop, int amount = 1)
    {
        if (itemToDrop == null) return false;
        
        bool removed = RemoveItem(itemToDrop, amount);
        
        if (removed)
        {
            // Instancier un objet physique dans le monde
            SpawnDroppedItem(itemToDrop, amount);
        }
        
        return removed;
    }

    // Permuter deux items
    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Count || toIndex < 0 || toIndex >= capacity)
            return;
            
        // Si la destination est vide et hors de la liste actuelle
        if (toIndex >= items.Count)
        {
            // Ajouter des emplacements vides si nécessaire
            while (items.Count <= toIndex)
            {
                items.Add(new ItemStack { item = null, count = 0 });
            }
        }
        
        // Permuter les items
        ItemStack temp = items[fromIndex];
        
        if (toIndex < items.Count)
        {
            items[fromIndex] = items[toIndex];
            items[toIndex] = temp;
        }
        else
        {
            // Déplacer vers un emplacement vide
            items[fromIndex] = new ItemStack { item = null, count = 0 };
            items[toIndex] = temp;
        }
        
        // Nettoyer les emplacements vides à la fin de la liste
        CleanEmptySlots();
        
        // Notifier du changement
        OnInventoryChanged?.Invoke();
    }

    // Nettoyer les emplacements vides
    private void CleanEmptySlots()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].item == null || items[i].count <= 0)
                items.RemoveAt(i);
        }
    }

    // Instancier un objet dans le monde
    private void SpawnDroppedItem(InventoryItem item, int amount)
    {
        if (item.dropPrefab == null)
        {
            Debug.LogWarning($"[InventoryManager] Pas de prefab de drop pour {item.displayName}");
            return;
        }
        
        // Trouver la position et orientation pour le drop
        Vector3 dropPosition = GetDropPosition();
        Quaternion dropRotation = Quaternion.identity;
        
        // Instancier le prefab
        GameObject droppedItem = Instantiate(item.dropPrefab, dropPosition, dropRotation);
        
        // Configurer le collectible si présent
        Collectible collectible = droppedItem.GetComponent<Collectible>();
        if (collectible != null)
        {
            collectible.amount = amount;
            collectible.itemID = item.itemID;
            collectible.inventoryItem = item;
        }
    }

    // Obtenir la position où jeter l'item
    private Vector3 GetDropPosition()
    {
        // Trouver la caméra du joueur
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            return transform.position + Vector3.forward;
        }
        
        // Calculer une position devant le joueur
        Vector3 dropDirection = playerCamera.transform.forward;
        Vector3 dropPosition = playerCamera.transform.position + dropDirection * 2f;
        
        // Ajout optionnel : Raycast pour éviter de traverser les murs
        if (Physics.Raycast(playerCamera.transform.position, dropDirection, out RaycastHit hit, 2f))
        {
            dropPosition = hit.point - dropDirection * 0.3f; // Légèrement en retrait du point d'impact
        }
        
        return dropPosition;
    }

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
}