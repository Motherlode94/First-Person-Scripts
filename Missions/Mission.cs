// Assets/Scripts/Missions/Mission.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Définit les différents types de missions disponibles
/// </summary>
public enum MissionType
{
    Main,
    Side,
    Timed,
    Stealth,
    Exploration,
    Defense,
    Escort,
    Collection,
    Puzzle
}

/// <summary>
/// Représente une mission complète avec ses objectifs et récompenses
/// </summary>
[CreateAssetMenu(fileName = "NewMission", menuName = "Missions/Mission", order = 1)]
public class Mission : ScriptableObject
{
    [Header("Conditions")]
    public List<MissionCondition> conditions = new List<MissionCondition>();
    [Header("Identification")]
    [Tooltip("Identifiant unique de la mission pour les références de dialogue")]
    public string missionID;

    [Tooltip("Titre affiché de la mission")]
    public string missionTitle;

    [Header("Description")]
    [TextArea(3, 5)]
    [Tooltip("Description détaillée de la mission")]
    public string missionDescription;

    [TextArea(2, 3)]
    [Tooltip("Liste récapitulative des objectifs (texte formaté)")]
    public string objectivesList;

    [Header("Configuration")]
    [Tooltip("Type de mission affectant son comportement")]
    public MissionType missionType = MissionType.Main;

    [Tooltip("Icône affichée dans l'interface")]
    public Sprite missionIcon;

    [Tooltip("Son joué au début de la mission")]
    public AudioClip missionStartSound;
    
    [Tooltip("Environnement de la mission (pour ajuster l'ambiance)")]
    public string environmentType;

    [Header("Objectifs et Récompenses")]
    [Tooltip("Liste des objectifs à accomplir")]
    public List<Objective> objectives = new List<Objective>();

    [Min(0)]
    [Tooltip("Points d'expérience gagnés à l'achèvement")]
    public int rewardXP;

    [Min(0)]
    [Tooltip("Limite de temps en secondes (si mission chronométrée)")]
    public float timeLimitSeconds;

    [Tooltip("Niveau minimum recommandé pour cette mission")]
    public int recommendedLevel = 1;

    [Tooltip("Changements de réputation à appliquer lors de l'achèvement")]
    public List<ReputationReward> reputationRewards = new List<ReputationReward>();
    
    [Header("Progression et Assistance")]
    [Tooltip("Si activé, les objectifs sont révélés progressivement")]
    public bool revealObjectivesProgressively = false;
    
    [Tooltip("Liste d'indices pour chaque objectif (dans le même ordre)")]
    public List<string> hints = new List<string>();
    
    [Tooltip("Délai en secondes avant affichage auto d'un indice (0 = désactivé)")]
    public float autoHintDelay = 0f;
    
    [Header("Effets Visuels et Audio")]
    [Tooltip("Couleur de thème pour cette mission")]
    public Color themeColor = Color.white;
    
    [Tooltip("Son d'ambiance de la mission")]
    public AudioClip ambientSound;
    
    [Tooltip("Volume du son d'ambiance")]
    [Range(0f, 1f)]
    public float ambientVolume = 0.5f;
    
    [Tooltip("Intensité de la vibration du contrôleur lors des événements")]
    [Range(0f, 1f)]
    public float vibrationIntensity = 0.2f;
    
    [Header("Récompenses Avancées")]
    [Tooltip("Objets débloqués à la fin de la mission")]
    public List<string> unlockedItems = new List<string>();
    
    [Tooltip("Compétences débloquées à la fin de la mission")]
    public List<string> unlockedSkills = new List<string>();
    [Header("Delivery Mission Settings")]
[Tooltip("Pour les missions de livraison uniquement: ID spécifique à cette livraison")]
public string deliveryMissionID = "";

[Tooltip("Zone de destination pour les objets livrables")]
public string targetDeliveryZoneID = "";
    
    [Tooltip("Monnaie/devise donnée en récompense")]
        public void CheckCondition(MissionCondition.ConditionType type, string param = "", float value = 0)
    {
        foreach (var condition in conditions)
        {
            if (condition.type == type && !condition.isFailed)
            {
                bool failed = false;
                
                switch (type)
                {
                    case MissionCondition.ConditionType.StealthOnly:
                        failed = param == "Detected";
                        break;
                    case MissionCondition.ConditionType.TimeLimit:
                        failed = value >= condition.value;
                        break;
                    case MissionCondition.ConditionType.NoWeapons:
                        failed = param == "WeaponEquipped";
                        break;
                    // Autres vérifications...
                }
                
                if (failed)
                {
                    condition.isFailed = true;
                    OnConditionFailed(condition);
                }
            }
        }
    }
        /// <summary>
    /// Vérifie la validité des données de la mission
    /// </summary>
    private void OnValidate()
    {
        // Validation plus stricte de l'ID de mission
        if (string.IsNullOrWhiteSpace(missionID))
        {
            Debug.LogError($"Mission {name}: l'ID de mission est vide! Un ID unique est OBLIGATOIRE.");
            
            // Suggestion d'un ID basé sur le nom de l'asset
            string suggestedId = name.Replace(" ", "_").ToUpper();
            Debug.LogWarning($"Suggestion d'ID: {suggestedId}");
        }
        
        // Vérifications existantes
        if (objectives == null || objectives.Count == 0)
        {
            Debug.LogWarning($"Mission {name}: aucun objectif défini!");
        }

        if (missionType == MissionType.Timed && timeLimitSeconds <= 0f)
        {
            Debug.LogWarning($"Mission {name}: une mission chronométrée doit avoir une limite de temps positive!");
        }
        
        // Vérifier que nous avons suffisamment d'indices
        if (hints.Count > 0 && hints.Count < objectives.Count)
        {
            Debug.LogWarning($"Mission {name}: il manque des indices pour certains objectifs!");
        }
    }
    
    protected virtual void OnConditionFailed(MissionCondition condition)
    {
        // Logique lorsqu'une condition échoue
        Debug.Log($"Mission condition failed: {condition.description}");
        
        // Notifier le système de mission
        if (MissionManager.Instance != null)
        {
            // Option 1: Échec immédiat de la mission
            // MissionManager.Instance.FailMission($"Condition échouée: {condition.description}");
            
            // Option 2: Marquer comme échec mais continuer (pour une récompense réduite)
            // Implémenter la logique appropriée ici
        }
    }
    public int currencyReward = 0;

    /// <summary>
    /// Vérifie si tous les objectifs de la mission sont complétés
    /// </summary>
    public bool IsCompleted => objectives != null && objectives.Count > 0 && objectives.TrueForAll(o => o.IsCompleted);

    /// <summary>
    /// Indique si la mission est chronométrée
    /// </summary>
    public bool IsTimed => missionType == MissionType.Timed && timeLimitSeconds > 0f;

    /// <summary>
    /// Pourcentage de complétion de la mission (0 à 1)
    /// </summary>
    public float CompletionPercentage
    {
        get
        {
            if (objectives == null || objectives.Count == 0)
                return 0f;

            float totalProgress = 0f;
            float totalWeight = 0f;
            
            foreach (var objective in objectives)
            {
                float weight = Mathf.Max(1f, objective.weight);
                totalWeight += weight;
                totalProgress += (float)objective.currentCount / objective.targetCount * weight;
            }

            return totalProgress / totalWeight;
        }
    }
    
    /// <summary>
    /// Nombre d'objectifs complétés
    /// </summary>
    public int CompletedObjectivesCount => objectives.Where(o => o.IsCompleted).Count();
    
    /// <summary>
    /// Nombre total d'objectifs visibles
    /// </summary>
    public int VisibleObjectivesCount => objectives.Where(o => o.visible).Count();

    /// <summary>
    /// Réinitialise le progrès de tous les objectifs de la mission
    /// </summary>
    public void ResetAllObjectives()
    {
        if (objectives == null) return;

        foreach (var objective in objectives)
        {
            objective.ResetProgress();
        }
    }
    
    /// <summary>
    /// Obtient un indice pour un objectif spécifique
    /// </summary>
    /// <param name="objectiveIndex">Index de l'objectif</param>
    /// <returns>Texte de l'indice ou chaîne vide si non disponible</returns>
    public string GetHintForObjective(int objectiveIndex)
    {
        if (hints.Count > objectiveIndex && objectiveIndex >= 0)
            return hints[objectiveIndex];
        else if (hints.Count > 0)
            return hints[0]; // Indice par défaut
        
        return string.Empty;
    }
    
    /// <summary>
    /// Retourne le prochain objectif non complété
    /// </summary>
    public Objective GetNextIncompleteObjective()
    {
        foreach (var objective in objectives)
        {
            if (objective.visible && !objective.IsCompleted)
                return objective;
        }
        
        return null;
    }
    
    /// <summary>
    /// Vérifie si tous les objectifs visibles sont complétés
    /// </summary>
    public bool AreAllVisibleObjectivesCompleted()
    {
        return objectives.Where(o => o.visible).All(o => o.IsCompleted);
    }
}

/// <summary>
/// Classe de récompense de réputation utilisée pour les missions
/// </summary>
[System.Serializable]
public class ReputationReward
{
    public string factionName;
    public int reputationChange;
    
    [Tooltip("Description textuelle de l'effet sur cette faction")]
    public string description;
    
    [Tooltip("Icône représentant cette faction")]
    public Sprite factionIcon;
}