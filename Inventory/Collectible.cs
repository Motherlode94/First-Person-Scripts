// Correction pour Collectible.cs
using UnityEngine;
using System;
using NeoFPS;              // pour InteractivePickup

[RequireComponent(typeof(InteractivePickup))]
public class Collectible : MonoBehaviour
{
    [Tooltip("Même ID que dans ta mission Collect (ex: \"HealthPack\", \"LootBox\"…)")]
    public string itemID;
    
    [Tooltip("Référence à l'item d'inventaire")]
    public InventoryItem inventoryItem;
    
    [Tooltip("Nombre d'items à ajouter à l'inventaire")]
    public int amount = 1;

    // Event global écouté par MissionManager
    public static event Action<string> OnItemCollected;

    private InteractivePickup m_Pickup;

    void Awake()
    {
        m_Pickup = GetComponent<InteractivePickup>();
        if (m_Pickup != null)
            m_Pickup.onPickedUp += HandlePickedUp;
    }

    void OnDestroy()
    {
        if (m_Pickup != null)
            m_Pickup.onPickedUp -= HandlePickedUp;
    }

    // Dans Collectible.cs
    private void HandlePickedUp(IInventory inv, IInventoryItem item)
    {
        // Notification pour les missions
        OnItemCollected?.Invoke(itemID);
        
        // Ajout à l'inventaire
        if (inventoryItem != null && InventoryManager.Instance != null)
        {
            bool added = InventoryManager.Instance.AddItem(inventoryItem, amount);
            
            if (added)
            {
                Debug.Log($"Collectible {itemID} ajouté à l'inventaire: {amount}x {inventoryItem.displayName}");
                
                // Jouer un son si configuré
                if (inventoryItem.pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(inventoryItem.pickupSound, transform.position);
                }
                
                // Afficher une notification
                ShowPickupNotification();
            }
            else
            {
                Debug.LogWarning($"Impossible d'ajouter {itemID} à l'inventaire - peut-être plein?");
            }
        }
        else
        {
            Debug.LogWarning($"InventoryItem non assigné pour Collectible {itemID} ou InventoryManager non disponible");
        }
    }

    private void ShowPickupNotification()
    {
        // Afficher une notification d'item collecté
        // Exemple basique - vous pouvez l'améliorer avec votre propre système de notification
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTemporaryMessage($"{amount}x {inventoryItem.displayName} ramassé", 2f);
        }
    }
} // Cette accolade fermante manquait