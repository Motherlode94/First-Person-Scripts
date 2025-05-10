using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ReputationManager : MonoBehaviour
{
    public static ReputationManager instance;

    [System.Serializable]
    public class FactionData
    {
        public string factionID;
        public string displayName;
        public Sprite icon;
        public Color color = Color.white;
        public int startingReputation = 0;
        public int minReputation = -100;
        public int maxReputation = 100;
        
        [Header("Seuils de réputation")]
        public int hostileThreshold = -40;  // En-dessous, la faction devient hostile
        public int suspiciousThreshold = -10; // Entre hostile et suspicieux
        public int neutralThreshold = 10;   // Entre suspicieux et neutre
        public int friendlyThreshold = 40;  // Entre neutre et amical
        public int alliedThreshold = 80;    // Au-dessus, allié total
    }

    [System.Serializable]
    public enum ReputationLevel
    {
        Hostile,
        Suspicious,
        Neutral,
        Friendly,
        Allied
    }

    [Header("Configuration des factions")]
    [SerializeField] private List<FactionData> factions = new List<FactionData>();
    
    // Dictionnaire pour stocker la réputation par faction
    private Dictionary<string, int> reputationValues = new Dictionary<string, int>();
    
    // Dictionnaire pour stocker le niveau actuel de réputation par faction
    private Dictionary<string, ReputationLevel> reputationLevels = new Dictionary<string, ReputationLevel>();

    // Événements pour notifier des changements de réputation
    public event Action<string, int, int> OnReputationChanged; // faction, oldValue, newValue
    public event Action<string, ReputationLevel, ReputationLevel> OnReputationLevelChanged; // faction, oldLevel, newLevel

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeReputation();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeReputation()
    {
        // Initialiser la réputation de base pour chaque faction
        foreach (var faction in factions)
        {
            reputationValues[faction.factionID] = faction.startingReputation;
            reputationLevels[faction.factionID] = CalculateReputationLevel(faction, faction.startingReputation);
            
            Debug.Log($"Réputation initiale avec {faction.displayName}: {faction.startingReputation} ({reputationLevels[faction.factionID]})");
        }
    }

    // Fonction pour modifier la réputation
    public void ChangeReputation(string factionID, int amount)
    {
        if (!reputationValues.ContainsKey(factionID))
        {
            Debug.LogWarning($"Faction inconnue: {factionID}");
            return;
        }

        FactionData faction = GetFactionData(factionID);
        if (faction == null) return;

        int oldValue = reputationValues[factionID];
        ReputationLevel oldLevel = reputationLevels[factionID];
        
        // Limiter la réputation aux bornes min/max de la faction
        int newValue = Mathf.Clamp(oldValue + amount, faction.minReputation, faction.maxReputation);
        reputationValues[factionID] = newValue;
        
        // Calculer le nouveau niveau de réputation
        ReputationLevel newLevel = CalculateReputationLevel(faction, newValue);
        reputationLevels[factionID] = newLevel;
        
        // Notifier si la valeur a changé
        if (newValue != oldValue)
        {
            Debug.Log($"Réputation avec {faction.displayName} modifiée: {oldValue} → {newValue} ({oldLevel} → {newLevel})");
            OnReputationChanged?.Invoke(factionID, oldValue, newValue);
            
            // Notifier le UI
            if (ReputationNotifier.instance != null)
            {
                ReputationNotifier.instance.ShowNotification(faction.displayName, amount);
            }
            
            // Notifier si le niveau a changé
            if (newLevel != oldLevel)
            {
                OnReputationLevelChanged?.Invoke(factionID, oldLevel, newLevel);
                
                // Déclencher des événements spécifiques au changement de niveau
                HandleReputationLevelChange(factionID, oldLevel, newLevel);
            }
        }
    }

    // Fonction pour obtenir la réputation actuelle
    public int GetReputation(string factionID)
    {
        if (reputationValues.ContainsKey(factionID))
        {
            return reputationValues[factionID];
        }
        return 0;
    }
    
    // Fonction pour obtenir le niveau de réputation actuel
    public ReputationLevel GetReputationLevel(string factionID)
    {
        if (reputationLevels.ContainsKey(factionID))
        {
            return reputationLevels[factionID];
        }
        return ReputationLevel.Neutral;
    }
    
    // Obtenir le pourcentage de réputation (0-100%)
    public float GetReputationPercentage(string factionID)
    {
        FactionData faction = GetFactionData(factionID);
        if (faction == null) return 50f; // Valeur par défaut
        
        int value = GetReputation(factionID);
        int range = faction.maxReputation - faction.minReputation;
        
        return (float)(value - faction.minReputation) / range * 100f;
    }
    
    // Obtenir les données d'une faction
    public FactionData GetFactionData(string factionID)
    {
        foreach (var faction in factions)
        {
            if (faction.factionID == factionID)
                return faction;
        }
        return null;
    }
    
    // Obtenir toutes les données de faction
    public List<FactionData> GetAllFactions()
    {
        return factions;
    }
    
    // Calculer le niveau de réputation en fonction de la valeur
    private ReputationLevel CalculateReputationLevel(FactionData faction, int value)
    {
        if (value < faction.hostileThreshold)
            return ReputationLevel.Hostile;
        else if (value < faction.suspiciousThreshold)
            return ReputationLevel.Suspicious;
        else if (value < faction.neutralThreshold)
            return ReputationLevel.Neutral;
        else if (value < faction.friendlyThreshold)
            return ReputationLevel.Friendly;
        else
            return ReputationLevel.Allied;
    }
    
    // Gérer les conséquences d'un changement de niveau de réputation
    private void HandleReputationLevelChange(string factionID, ReputationLevel oldLevel, ReputationLevel newLevel)
    {
        switch (factionID)
        {
            case "Police":
                HandlePoliceReputationChange(oldLevel, newLevel);
                break;
            case "Gang":
                HandleGangReputationChange(oldLevel, newLevel);
                break;
            case "Civils":
                HandleCivilianReputationChange(oldLevel, newLevel);
                break;
            default:
                // Gérer d'autres factions si nécessaire
                break;
        }
    }
    
    // Gérer les changements de réputation avec la police
    private void HandlePoliceReputationChange(ReputationLevel oldLevel, ReputationLevel newLevel)
    {
        // Si la réputation passe à hostile
        if (newLevel == ReputationLevel.Hostile && oldLevel != ReputationLevel.Hostile)
        {
            Debug.Log("La police est maintenant hostile et vous traque activement!");
            SpawnPoliceUnits(3); // Faire apparaître des unités de police
            BroadcastWantedStatus(true); // Signaler aux PNJ que le joueur est recherché
        }
        // Si la réputation passe à suspicieux
        else if (newLevel == ReputationLevel.Suspicious && oldLevel != ReputationLevel.Suspicious)
        {
            Debug.Log("La police vous surveille de près...");
            IncreasePolicePatrols();
        }
        // Si la réputation s'améliore et n'est plus hostile
        else if (oldLevel == ReputationLevel.Hostile && newLevel != ReputationLevel.Hostile)
        {
            Debug.Log("La police ne vous traque plus activement");
            BroadcastWantedStatus(false);
            StopActivePoliceChase();
        }
    }
    
    // Gérer les changements de réputation avec les gangs
    private void HandleGangReputationChange(ReputationLevel oldLevel, ReputationLevel newLevel)
    {
        // Si la réputation passe à hostile
        if (newLevel == ReputationLevel.Hostile && oldLevel != ReputationLevel.Hostile)
        {
            Debug.Log("Les gangs sont maintenant hostiles et vous attaqueront à vue!");
            MarkTerritoryAsDangerous("Gang");
        }
        // Si la réputation passe à allié
        else if (newLevel == ReputationLevel.Allied && oldLevel != ReputationLevel.Allied)
        {
            Debug.Log("Les gangs vous considèrent comme l'un des leurs!");
            UnlockGangHideouts();
            ProvideGangProtection(true);
        }
        // Si la réputation n'est plus alliée
        else if (oldLevel == ReputationLevel.Allied && newLevel != ReputationLevel.Allied)
        {
            Debug.Log("Vous n'êtes plus considéré comme un allié des gangs");
            ProvideGangProtection(false);
        }
    }
    
    // Gérer les changements de réputation avec les civils
    private void HandleCivilianReputationChange(ReputationLevel oldLevel, ReputationLevel newLevel)
    {
        // Si la réputation passe à hostile
        if (newLevel == ReputationLevel.Hostile && oldLevel != ReputationLevel.Hostile)
        {
            Debug.Log("Les civils fuient à votre approche!");
            AdjustCivilianBehavior("Fear");
            AdjustShopPrices(1.5f); // Prix augmentés de 50%
        }
        // Si la réputation passe à amical
        else if (newLevel == ReputationLevel.Friendly && oldLevel != ReputationLevel.Friendly)
        {
            Debug.Log("Les civils vous apprécient et sont heureux de vous aider!");
            AdjustCivilianBehavior("Helpful");
            AdjustShopPrices(0.9f); // Prix réduits de 10%
        }
        // Retour à la normale
        else if ((oldLevel == ReputationLevel.Hostile || oldLevel == ReputationLevel.Friendly) && 
                 newLevel == ReputationLevel.Neutral)
        {
            Debug.Log("Les civils ont une attitude neutre envers vous");
            AdjustCivilianBehavior("Normal");
            AdjustShopPrices(1.0f); // Prix normaux
        }
    }
    
    // Méthodes pour les conséquences concrètes - À implémenter selon votre jeu
    
    private void SpawnPoliceUnits(int count)
    {
        // Implémentez cette méthode pour faire apparaître des unités de police
        // En fonction de votre système d'IA et de spawn
        Debug.Log($"[À implémenter] Faire apparaître {count} unités de police pour traquer le joueur");
    }
    
    private void BroadcastWantedStatus(bool isWanted)
    {
        // Notifie tous les PNJ du statut recherché du joueur
        Debug.Log($"[À implémenter] Statut recherché: {isWanted}");
    }
    
    private void IncreasePolicePatrols()
    {
        // Augmente le nombre de patrouilles de police dans la zone
        Debug.Log("[À implémenter] Augmentation des patrouilles de police");
    }
    
    private void StopActivePoliceChase()
    {
        // Arrête toute poursuite active de la police
        Debug.Log("[À implémenter] Fin de la poursuite policière active");
    }
    
    private void MarkTerritoryAsDangerous(string factionID)
    {
        // Marque le territoire de la faction comme dangereux pour le joueur
        Debug.Log($"[À implémenter] Territoire {factionID} marqué comme dangereux");
    }
    
    private void UnlockGangHideouts()
    {
        // Déverrouille les planques de gang pour le joueur
        Debug.Log("[À implémenter] Planques de gang déverrouillées");
    }
    
    private void ProvideGangProtection(bool enabled)
    {
        // Active/désactive la protection des gangs
        Debug.Log($"[À implémenter] Protection des gangs: {enabled}");
    }
    
    private void AdjustCivilianBehavior(string behavior)
    {
        // Ajuste le comportement des civils envers le joueur
        Debug.Log($"[À implémenter] Comportement des civils ajusté: {behavior}");
    }
    
    private void AdjustShopPrices(float multiplier)
    {
        // Ajuste les prix dans les magasins
        Debug.Log($"[À implémenter] Prix des magasins ajustés: {multiplier}");
    }
}