// Assets/Scripts/Missions/Objective.cs
using UnityEngine;
using System;

/// <summary>
/// Types d'objectifs possibles dans le jeu
/// </summary>
public enum ObjectiveType {
    Kill,
    ReachZone,
    Collect,
    EquipWeapon,
    InspectObject,
    Escort,
    Activate,
    Scan,
    Disarm,
    Deliver,
    ActivateSwitch,
    CompleteSequence,
    Defend,
    Survive,
    Hack,
    Talk,
    TakePhoto,
    PlaceItem
}

/// <summary>
/// Représente un objectif individuel au sein d'une mission
/// </summary>
[Serializable]
public class Objective
{
    [Tooltip("Type d'objectif à accomplir")]
    public ObjectiveType type;
    
    [Tooltip("Ex. 'Rifle' pour EquipWeapon, ou ID de zone, ou ID d'item…")]
    public string targetID;

    [TextArea(2, 5)]
    [Tooltip("Description de l'objectif affichée au joueur")]
    public string description;
    
    [Tooltip("Nombre d'occurrences nécessaires pour valider l'objectif")]
    public int targetCount = 1;

    [HideInInspector] 
    public int currentCount;
    
    [Tooltip("Faction affected by this objective")]
    public string affectedFaction;
    
    [Tooltip("Reputation change when completed")]
    public int reputationChange;
    
    [Tooltip("Poids de cet objectif dans le calcul du pourcentage global")]
    [Range(1, 10)]
    public int weight = 1;
    
    [Tooltip("Couleur de l'indicateur d'objectif")]
    public Color markerColor = Color.yellow;
    
    [Tooltip("Distance à laquelle l'objectif est considéré comme 'proche'")]
    public float proximityThreshold = 5f;
    
    [Tooltip("Si activé, l'objectif sera toujours visible sur la carte")]
    public bool alwaysShowOnMap = false;
    
    [Tooltip("Son joué quand l'objectif est complété")]
    public AudioClip completionSound;
    
    [Tooltip("Effet visuel à jouer quand l'objectif est complété")]
    public string completionEffectPrefab;
    
    [Tooltip("Récompense XP additionnelle pour cet objectif spécifique")]
    public int objectiveXP = 0;

    [HideInInspector]
    public bool visible = false;
    
    [HideInInspector]
    public float lastUpdateTime = 0f;
    
    [HideInInspector]
    public Vector3 worldPosition = Vector3.zero;
    
    [HideInInspector]
    public bool isOptional = false;
    
    [HideInInspector]
    public bool hasBeenSeen = false;
    
    [HideInInspector]
    public bool isHighlighted = false;
    public ObjectiveType objectiveType;
    [Header("Delivery Settings")]
[Tooltip("ID de la zone de livraison cible (pour objectifs de type Deliver)")]
public string deliveryZoneID = "";

[Tooltip("ID de l'objet livrable à déposer dans cette zone")]
public string deliverableID = "";


    /// <summary>
    /// Indique si l'objectif est complété
    /// </summary>
    public bool IsCompleted => currentCount >= targetCount;
    
    /// <summary>
    /// Pourcentage de progression (0-1)
    /// </summary>
    public float Progress => targetCount > 0 ? Mathf.Clamp01((float)currentCount / targetCount) : 0f;

    /// <summary>
    /// Notifie l'objectif d'un événement correspondant à son type
    /// </summary>
    /// <param name="evtType">Type d'événement survenu</param>
    /// <param name="id">Identifiant associé à l'événement (cible, zone, etc.)</param>
    /// <param name="amount">Quantité à ajouter au progrès actuel</param>
    /// <returns>Vrai si la notification a fait progresser l'objectif</returns>
    public bool NotifyEvent(ObjectiveType evtType, string id = null, int amount = 1)
    {
        // Vérification du type d'événement
        if (evtType != type) 
            return false;
        
        // Vérification de l'ID cible si nécessaire
        if (!string.IsNullOrEmpty(targetID) && id != targetID) 
            return false;
        
        // Mise à jour du compteur avec vérification de dépassement
        int oldCount = currentCount;
        currentCount = Mathf.Min(currentCount + amount, targetCount);
        
        // Mise à jour du temps de dernière mise à jour
        lastUpdateTime = Time.time;
        
        // Enregistrement du statut "vu"
        if (!hasBeenSeen)
            hasBeenSeen = true;
        
        // Vérifie si nous venons juste de compléter l'objectif
        bool justCompleted = oldCount < targetCount && currentCount >= targetCount;
        
        return currentCount > oldCount || justCompleted;
    }

    /// <summary>
    /// Réinitialise le progrès de cet objectif
    /// </summary>
    public void ResetProgress()
    {
        currentCount = 0;
        hasBeenSeen = false;
        isHighlighted = false;
        lastUpdateTime = 0f;
    }

    /// <summary>
    /// Retourne une description formatée de cet objectif avec son progrès actuel
    /// </summary>
    public string GetProgressText()
    {
        if (targetCount <= 1)
            return description;
        else
            return $"{description} ({currentCount}/{targetCount})";
    }
    
    /// <summary>
    /// Indique si cet objectif est proche du joueur
    /// </summary>
    public bool IsNearPlayer(Vector3 playerPosition)
    {
        if (worldPosition == Vector3.zero)
            return false;
            
        return Vector3.Distance(playerPosition, worldPosition) <= proximityThreshold;
    }
    
    /// <summary>
    /// Highlight this objective
    /// </summary>
    public void Highlight(bool highlight = true)
    {
        isHighlighted = highlight;
    }
    
    /// <summary>
    /// Format text with colored highlight if needed
    /// </summary>
    public string GetFormattedDescription()
    {
        if (isHighlighted)
            return $"<color=yellow>{description}</color>";
        else if (IsCompleted)
            return $"<color=green>{description}</color>";
        else if (isOptional)
            return $"<color=gray>{description} (optionnel)</color>";
            
        return description;
    }
}