using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    
    public InventoryManager inventory;
    public GameObject slotPrefab;
    public Transform slotsParent;
    
    [Header("Interaction")]
    [SerializeField] private GameObject itemDetailsPanel;
    [SerializeField] private TMPro.TextMeshProUGUI itemNameText;
    [SerializeField] private TMPro.TextMeshProUGUI itemDescriptionText;
    [SerializeField] private UnityEngine.UI.Image itemIcon;
    [SerializeField] private GameObject useButton;
    [SerializeField] private GameObject dropButton;
    
    [Header("Drag & Drop")]
    [SerializeField] private GameObject dragItemPrefab;
    [SerializeField] private bool enableDragDrop = true;
    
    private List<InventorySlot> uiSlots = new List<InventorySlot>();
    private InventoryItem selectedItem;
    private int selectedSlotIndex = -1;
    private GameObject draggedItem;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;

        if (slotPrefab == null || slotsParent == null)
        {
            Debug.LogError("[InventoryUI] slotPrefab or slotsParent not assigned.");
            return;
        }

        // Créer les slots UI
        CreateSlots();

        // S'abonner aux événements
        inventory.OnInventoryChanged += RefreshUI;
        
        // Cacher le panneau de détails
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
            
        // Premier refresh
        RefreshUI();
    }
    
    private void CreateSlots()
    {
        // Nettoyer les slots existants
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        uiSlots.Clear();
        
        // Créer les nouveaux slots
        for (int i = 0; i < inventory.capacity; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);
            InventorySlot slot = slotGO.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                // Configurer le slot
                slot.slotIndex = i;
                slot.OnSlotClicked += HandleSlotClicked;
                
                if (enableDragDrop)
                {
                    // Ajouter les comportements de drag & drop
                    ConfigureDragDrop(slot, i);
                }
                
                uiSlots.Add(slot);
            }
        }
    }

    public void RefreshUI()
    {
        // Mettre à jour tous les slots
        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < inventory.items.Count)
            {
                var stack = inventory.items[i];
                uiSlots[i].Set(stack.item, stack.count);
            }
            else
            {
                uiSlots[i].Clear();
            }
        }
        
        // Mettre à jour le panneau de détails
        UpdateItemDetails();
    }
    
    private void HandleSlotClicked(int slotIndex)
    {
        // Mémoriser le slot sélectionné
        selectedSlotIndex = slotIndex;
        
        // Mémoriser l'item sélectionné
        if (slotIndex >= 0 && slotIndex < inventory.items.Count)
        {
            selectedItem = inventory.items[slotIndex].item;
        }
        else
        {
            selectedItem = null;
        }
        
        // Mettre à jour l'UI de détails
        UpdateItemDetails();
    }
    
    private void UpdateItemDetails()
    {
        if (itemDetailsPanel == null) return;
        
        if (selectedItem != null)
        {
            // Afficher le panneau de détails
            itemDetailsPanel.SetActive(true);
            
            // Mettre à jour les informations
            if (itemNameText != null)
                itemNameText.text = selectedItem.displayName;
                
            if (itemDescriptionText != null)
                itemDescriptionText.text = selectedItem.description;
                
            if (itemIcon != null)
                itemIcon.sprite = selectedItem.icon;
                
            // Afficher/masquer les boutons selon le type d'item
            if (useButton != null)
                useButton.SetActive(selectedItem.isUsable);
                
            if (dropButton != null)
                dropButton.SetActive(true);
        }
        else
        {
            // Masquer le panneau si aucun item n'est sélectionné
            itemDetailsPanel.SetActive(false);
        }
    }
    
    // Utiliser l'item sélectionné
    public void UseSelectedItem()
    {
        if (selectedItem != null && selectedSlotIndex >= 0)
        {
            inventory.UseItem(selectedItem);
            
            // Après utilisation, l'item peut avoir été consommé, donc rafraîchir
            if (selectedSlotIndex >= inventory.items.Count || inventory.items[selectedSlotIndex].item != selectedItem)
            {
                selectedItem = null;
                selectedSlotIndex = -1;
            }
            
            UpdateItemDetails();
        }
    }
    
    // Jeter l'item sélectionné
    public void DropSelectedItem()
    {
        if (selectedItem != null && selectedSlotIndex >= 0)
        {
            inventory.DropItem(selectedItem);
            
            // Après avoir jeté l'item, il peut avoir été consommé
            selectedItem = null;
            selectedSlotIndex = -1;
            
            UpdateItemDetails();
        }
    }
    
    // Configuration du drag & drop
    private void ConfigureDragDrop(InventorySlot slot, int index)
    {
        EventTrigger trigger = slot.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = slot.gameObject.AddComponent<EventTrigger>();
            
        // Ajouter les entrées pour le drag & drop
        AddEventTriggerEntry(trigger, EventTriggerType.BeginDrag, (data) => { OnBeginDrag(slot, index, data); });
        AddEventTriggerEntry(trigger, EventTriggerType.Drag, (data) => { OnDrag(data); });
        AddEventTriggerEntry(trigger, EventTriggerType.EndDrag, (data) => { OnEndDrag(slot, index, data); });
        AddEventTriggerEntry(trigger, EventTriggerType.Drop, (data) => { OnDrop(slot, index, data); });
    }
    
    private void AddEventTriggerEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
    
    private void OnBeginDrag(InventorySlot slot, int index, BaseEventData data)
    {
        if (index >= inventory.items.Count) return;
        
        // Créer un visuel pour l'item en cours de drag
        draggedItem = Instantiate(dragItemPrefab, transform);
        
        // Configurer le visuel
        UnityEngine.UI.Image img = draggedItem.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            img.sprite = inventory.items[index].item.icon;
            
        // Position initiale
        RectTransform rt = draggedItem.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.position = Input.mousePosition;
        }
    }
    
    private void OnDrag(BaseEventData data)
    {
        if (draggedItem == null) return;
        
        // Suivre la souris
        PointerEventData pointerData = (PointerEventData)data;
        draggedItem.transform.position = pointerData.position;
    }
    
    private void OnEndDrag(InventorySlot slot, int fromIndex, BaseEventData data)
    {
        // Nettoyer
        if (draggedItem != null)
            Destroy(draggedItem);
            
        draggedItem = null;
    }
    
    private void OnDrop(InventorySlot targetSlot, int toIndex, BaseEventData data)
    {
        // Récupérer le slot de départ
        PointerEventData pointerData = (PointerEventData)data;
        GameObject dropped = pointerData.pointerDrag;
        InventorySlot fromSlot = dropped.GetComponent<InventorySlot>();
        
        if (fromSlot != null)
        {
            int fromIndex = fromSlot.slotIndex;
            
            // Demander à l'inventaire de permuter les items
            inventory.SwapItems(fromIndex, toIndex);
        }
    }
}