using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image icon;
    public TextMeshProUGUI countText;
    public Image background;
    public Image highlight;
    
    [Header("States")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    [HideInInspector]
    public int slotIndex = -1;
    
    private InventoryItem currentItem;
    private int currentCount;
    private bool isEmpty = true;
    private bool isSelected = false;
    
    // Événement déclenché quand le slot est cliqué
    public event Action<int> OnSlotClicked;

    private void Awake()
    {
        // Initialiser l'état
        if (highlight != null)
            highlight.gameObject.SetActive(false);
            
        Clear();
    }

    public void Set(InventoryItem item, int count)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        currentItem = item;
        currentCount = count;
        isEmpty = false;

        // Configurer l'UI
        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = true;
        }
        
        if (countText != null)
        {
            countText.text = item.stackable ? count.ToString() : string.Empty;
            countText.gameObject.SetActive(item.stackable && count > 1);
        }
        
        if (background != null)
        {
            background.color = normalColor;
        }
    }

    public void Clear()
    {
        currentItem = null;
        currentCount = 0;
        isEmpty = true;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
        
        if (countText != null)
        {
            countText.text = string.Empty;
            countText.gameObject.SetActive(false);
        }
        
        if (background != null)
        {
            background.color = emptyColor;
        }
        
        SetSelected(false);
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (highlight != null)
            highlight.gameObject.SetActive(selected);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // Notifier que ce slot a été cliqué
        OnSlotClicked?.Invoke(slotIndex);
        
        // Si le slot n'est pas vide, le sélectionner
        if (!isEmpty)
        {
            SetSelected(true);
        }
    }
    
    public InventoryItem GetItem()
    {
        return currentItem;
    }
    
    public int GetCount()
    {
        return currentCount;
    }
    
    public bool IsEmpty()
    {
        return isEmpty;
    }
}