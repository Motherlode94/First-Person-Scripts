using UnityEngine;
using System.Collections.Generic;

public class ReputationStatusPanel : MonoBehaviour
{
    [SerializeField] private GameObject reputationEntryPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    
    private Dictionary<string, UIReputation> factionEntries = new Dictionary<string, UIReputation>();
    private bool isInitialized = false;
    
    private void Start()
    {
        // Cacher au démarrage
        gameObject.SetActive(false);
        
        // Initialiser lors de la première demande
        if (ReputationManager.instance != null)
        {
            InitializePanel();
        }
    }
    
    private void Update()
    {
        // Toggle le panneau avec la touche configurée
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }
    
    public void TogglePanel()
    {
        if (!isInitialized && gameObject.activeSelf)
        {
            InitializePanel();
        }
        
        gameObject.SetActive(!gameObject.activeSelf);
    }
    
    private void InitializePanel()
    {
        if (ReputationManager.instance == null || reputationEntryPrefab == null || contentParent == null)
            return;
            
        // Nettoyer les entrées existantes
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        factionEntries.Clear();
        
        // Créer une entrée pour chaque faction
        List<ReputationManager.FactionData> factions = ReputationManager.instance.GetAllFactions();
        foreach (var faction in factions)
        {
            GameObject entryGO = Instantiate(reputationEntryPrefab, contentParent);
            UIReputation uiReputation = entryGO.GetComponent<UIReputation>();
            
            if (uiReputation != null)
            {
                // Configuration de l'UI
                factionEntries[faction.factionID] = uiReputation;
                
                // Actualiser immédiatement
                uiReputation.SetFaction(faction.factionID);
            }
        }
        
        isInitialized = true;
    }
    
    public void RefreshAllEntries()
    {
        if (!isInitialized)
            InitializePanel();
            
        foreach (var entry in factionEntries.Values)
        {
            entry.UpdateUI(false);
        }
    }
}