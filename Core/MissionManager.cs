using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class MissionManager : MonoBehaviour
{
    #region Singleton
    public static MissionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else 
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Configuration
    [Header("Configuration des Missions")]
   public List<Mission> allMissions = new List<Mission>();

    [SerializeField] private bool autoAdvanceToNextMission = true;
    [SerializeField] private float advanceDelay = 3f;
    [SerializeField] private bool restartFailedMissions = false;
    [SerializeField] private int maxFailedAttempts = 3;
    
    [Header("Événements Audio")]
    [SerializeField] private AudioClip missionCompletedSound;
    [SerializeField] private AudioClip missionFailedSound;
    [SerializeField] private AudioClip objectiveCompletedSound;
    
    [Header("Débogage")]
    [SerializeField] private bool logEvents = true;
    #endregion

    #region État du Système
    private Mission activeMission;
    private int currentMissionIndex;
    private int completedMissions;
    private int failedMissions;
    private int currentAttempt = 1;

    private Dictionary<string, Mission> missionsByID = new Dictionary<string, Mission>();
    private Dictionary<Mission, bool> missionCompletionStatus = new Dictionary<Mission, bool>();
    private bool waitingForWavesToComplete = false;
    #endregion

    #region Propriétés Publiques
    /// <summary>Indique si une mission est en cours</summary>
    public bool HasActiveMission => activeMission != null;

    /// <summary>Mission active actuellement</summary>
    public Mission ActiveMission => activeMission;

    /// <summary>Index de la mission actuelle</summary>
    public int CurrentMissionIndex 
    {
        get => currentMissionIndex;
        private set => currentMissionIndex = value;
    }

    /// <summary>Indique si toutes les missions sont terminées</summary>
    public bool AllMissionsCompleted => currentMissionIndex >= allMissions.Count;

    /// <summary>Nombre de missions complétées</summary>
    public int CompletedMissions
    {
        get => completedMissions;
        private set => completedMissions = value;
    }

    /// <summary>Nombre de missions échouées</summary>
    public int FailedMissions
    {
        get => failedMissions;
        private set => failedMissions = value;
    }

    /// <summary>Tentative actuelle sur la mission courante</summary>
    public int CurrentAttempt
    {
        get => currentAttempt;
        private set => currentAttempt = value;
    }

    /// <summary>Liste de toutes les missions disponibles</summary>
    public List<Mission> AllMissions => allMissions;
    #endregion

    #region Coroutines
    private Coroutine countdownCoroutine;
    private Coroutine resultCoroutine;
    #endregion

    #region Événements Publics
    /// <summary>Déclenché quand une mission est activée</summary>
    public event Action<Mission> OnMissionActivated;
    
    /// <summary>Déclenché quand une mission est complétée ou échouée</summary>
    public event Action<Mission, bool> OnMissionCompleted;
    
    /// <summary>Déclenché quand un objectif est progressé</summary>
    public event Action<Mission, Objective> OnObjectiveProgressed;
    
    /// <summary>Déclenché quand un objectif est complété</summary>
    public event Action<Mission, Objective> OnObjectiveCompleted;
    
    /// <summary>Déclenché quand toutes les missions sont terminées</summary>
    public event Action OnAllMissionsCompleted;
    /// <summary>Déclenché quand un objectif est mis à jour</summary>
public event Action<Objective> OnObjectiveUpdated;
    #endregion

    #region Initialisation
    /// <summary>
    /// Initialise le gestionnaire de mission au démarrage
    /// </summary>
    private void InitializeManager()
    {
        // Initialisation du dictionnaire de missions par ID
        missionsByID.Clear();
        foreach (var mission in allMissions)
        {
            if (!string.IsNullOrEmpty(mission.missionID))
            {
                if (missionsByID.ContainsKey(mission.missionID))
                {
                    Debug.LogError($"MissionManager: ID de mission en double détecté: {mission.missionID}");
                }
                else
                {
                    missionsByID[mission.missionID] = mission;
                }
            }
        }
        
        if (logEvents)
            Debug.Log($"MissionManager: {allMissions.Count} mission(s) initialisée(s)");
    }

    private void Start()
    {
        // Initialisation de l'interface utilisateur
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateXP(XPSystem.CurrentXP);
            UIManager.Instance.UpdateLevel(XPSystem.CurrentLevel);
        }

        // Souscription aux événements
        SubscribeToEvents();

        // Activation de la première mission si disponible
        if (allMissions.Count > 0)
        {
            ActivateMission(allMissions[0]);
        }
        else if (logEvents)
        {
            Debug.LogWarning("MissionManager: Aucune mission définie dans l'inspecteur !");
        }
    }

    private void OnDestroy()
    {
        // Désinscription des événements
        UnsubscribeFromEvents();
    }
    #endregion

    #region Gestion des Événements
    private void SubscribeToEvents()
    {
        // Souscription aux événements du monde de jeu
        EnemyAI.OnEnemyAIKilled += OnEnemyAIKilled;
        ZoneDiscoveryNotifier.OnZoneEntered += OnZoneEntered;
        Collectible.OnItemCollected += OnItemCollected;
        WeaponManager.OnWeaponEquipped += OnWeaponEquipped;
        WaveSpawner.OnAllWavesComplete += OnAllWavesFinished;
    }

    private void UnsubscribeFromEvents()
    {
        // Désinscription des événements
        EnemyAI.OnEnemyAIKilled -= OnEnemyAIKilled;
        ZoneDiscoveryNotifier.OnZoneEntered -= OnZoneEntered;
        Collectible.OnItemCollected -= OnItemCollected;
        WeaponManager.OnWeaponEquipped -= OnWeaponEquipped;
        WaveSpawner.OnAllWavesComplete -= OnAllWavesFinished;
    }

    // Méthodes de gestion des événements
    private void OnEnemyAIKilled(string id) => NotifyObjectives(ObjectiveType.Kill, id);
    private void OnZoneEntered(string id) => NotifyObjectives(ObjectiveType.ReachZone, id);
    private void OnItemCollected(string id) => NotifyObjectives(ObjectiveType.Collect, id);
    private void OnWeaponEquipped(string id) => NotifyObjectives(ObjectiveType.EquipWeapon, id);

    private void OnAllWavesFinished(int finalWave)
    {
        if (logEvents)
            Debug.Log($"MissionManager: Toutes les vagues terminées. Vague finale : {finalWave}");

        if (activeMission != null && activeMission.IsCompleted && waitingForWavesToComplete)
        {
            if (logEvents)
                Debug.Log("MissionManager: La mission est validée après toutes les vagues !");
                
            waitingForWavesToComplete = false;
            CompleteMission();
        }
    }
    #endregion

    #region Méthodes Publiques
    /// <summary>
    /// Active une mission particulière
    /// </summary>
    /// <param name="mission">Mission à activer</param>
    public void ActivateMission(Mission mission)
    {
        if (mission == null)
        {
            Debug.LogError("MissionManager: Tentative d'activation d'une mission null!");
            return;
        }

        // Arrêt des coroutines en cours si nécessaires
        StopAllCoroutines();
        countdownCoroutine = null;
        resultCoroutine = null;

        // Masquer les résultats de la mission précédente
        if (UIManager.Instance != null)
            UIManager.Instance.HideMissionResult();

        // Initialisation de la nouvelle mission
        activeMission = mission;
        CurrentAttempt = 1;
        waitingForWavesToComplete = false;
        
        // Réinitialisation des objectifs
        foreach (var objective in mission.objectives)
        {
            objective.ResetProgress();
        }

        // Réinitialisation des zones si nécessaire
        if (ZoneStateManager.Instance != null)
            ZoneStateManager.Instance.ResetForMission(mission);

        // Démarrage du compte à rebours si mission chronométrée
        if (mission.IsTimed)
            countdownCoroutine = StartCoroutine(MissionCountdown(mission.timeLimitSeconds));

        // Jouer le son de démarrage
        if (mission.missionStartSound != null)
            AudioSource.PlayClipAtPoint(mission.missionStartSound, Camera.main.transform.position);

        // Mise à jour de l'interface utilisateur
        if (UIManager.Instance != null)
            UIManager.Instance.RefreshMissionUI(mission);

        // Déclenchement de l'événement OnMissionActivated
        OnMissionActivated?.Invoke(mission);

        if (logEvents)
            Debug.Log($"MissionManager: Mission activée - {mission.missionTitle}");
    }
    // Dans votre classe MissionManager

/// <summary>
/// Récupère une mission par son ID unique
/// </summary>
public Mission GetMissionByID(string missionID)
{
    return allMissions.Find(m => m.missionID == missionID);
}

/// <summary>
/// Vérifie si l'ID de mission est unique dans le système
/// </summary>
public bool IsMissionIDUnique(string missionID)
{
    if (string.IsNullOrEmpty(missionID))
        return false;
        
    int count = allMissions.Count(m => m.missionID == missionID);
    return count <= 1;
}

/// <summary>
/// Récupère toutes les missions d'un type spécifique
/// </summary>
public List<Mission> GetMissionsByType(MissionType type)
{
    return allMissions.Where(m => m.missionType == type).ToList();
}

/// <summary>
/// Récupère toutes les missions dont les objectifs contiennent un type spécifique
/// </summary>
public List<Mission> GetMissionsByObjectiveType(ObjectiveType objectiveType)
{
    return allMissions.Where(m => m.objectives.Any(o => o.type == objectiveType)).ToList();
}

/// <summary>
/// Vérifie l'unicité de tous les IDs de mission au chargement
/// </summary>
private void ValidateMissionIDs()
{
    var duplicateIDs = allMissions
        .GroupBy(m => m.missionID)
        .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
        .Select(g => g.Key)
        .ToList();
        
    if (duplicateIDs.Count > 0)
    {
        Debug.LogError($"ERREUR CRITIQUE: {duplicateIDs.Count} IDs de mission en doublon détectés:");
        foreach (var id in duplicateIDs)
        {
            var missions = allMissions.Where(m => m.missionID == id).ToList();
            string namesStr = string.Join(", ", missions.Select(m => m.name));
            Debug.LogError($"ID '{id}' utilisé par: {namesStr}");
        }
    }
}

    /// <summary>
    /// Active une mission par son ID
    /// </summary>
    /// <param name="missionID">ID unique de la mission</param>
    /// <returns>Vrai si la mission a été trouvée et activée</returns>
    public bool ActivateMissionByID(string missionID)
    {
        if (string.IsNullOrEmpty(missionID))
            return false;

        if (missionsByID.TryGetValue(missionID, out Mission mission))
        {
            ActivateMission(mission);
            return true;
        }
        
        Debug.LogWarning($"MissionManager: Mission avec ID '{missionID}' non trouvée!");
        return false;
    }

    /// <summary>
    /// Notifie les objectifs de la mission active d'un événement
    /// </summary>
    /// <param name="type">Type d'objectif concerné</param>
    /// <param name="id">ID de la cible concernée</param>
    /// <param name="amount">Quantité à ajouter au progrès</param>
    public void NotifyObjectives(ObjectiveType type, string id = null, int amount = 1)
    {
        if (activeMission == null) return;

        bool anyObjectiveProgressed = false;
        bool anyObjectiveCompleted = false;

        // Notification à tous les objectifs de la mission
        foreach (var objective in activeMission.objectives)
        {
            if (objective.type == type)
            {
                bool matchesTarget = true;
                

                // La cible correspond à l'objectif
                bool wasCompleted = objective.IsCompleted;
                bool progressed = objective.NotifyEvent(type, id, amount);
                
                if (progressed)
                {
                    anyObjectiveProgressed = true;
                    OnObjectiveProgressed?.Invoke(activeMission, objective);
                    OnObjectiveUpdated?.Invoke(objective);

                    if (!wasCompleted && objective.IsCompleted)
                    {
                        anyObjectiveCompleted = true;
                        OnObjectiveCompleted?.Invoke(activeMission, objective);

                        if (objectiveCompletedSound != null)
                            AudioSource.PlayClipAtPoint(objectiveCompletedSound, Camera.main.transform.position);
                    }

                    // Gestion de la réputation (pas dépendant de l'objectif complété)
                    if (!string.IsNullOrEmpty(objective.affectedFaction) && objective.reputationChange != 0)
                    {
                        ReputationManager.instance?.ChangeReputation(
                            objective.affectedFaction, 
                            objective.reputationChange
                        );
                    }
                }
            }
        }

        // Mise à jour de l'interface utilisateur si un objectif a progressé
        if (anyObjectiveProgressed && UIManager.Instance != null)
            UIManager.Instance.RefreshMissionUI(activeMission);

        // Affichage du log si configuré
        if (anyObjectiveProgressed && logEvents)
            Debug.Log($"MissionManager: Progression d'objectif - {type} / {id}");

        // Vérification de l'achèvement de la mission
        if (activeMission.IsCompleted)
        {
            // Vérification si on doit attendre la fin des vagues
            if (WaveSpawner.Instance != null && WaveSpawner.Instance.IsSpawning())
            {
                if (logEvents)
                    Debug.Log("MissionManager: Objectifs remplis mais vagues encore actives.");
                    
                waitingForWavesToComplete = true;
                
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowContinueWavesMessage();
                
                return;
            }

            CompleteMission();
        }
    }

    /// <summary>
    /// Marque la mission actuelle comme échouée
    /// </summary>
    /// <param name="reason">Raison de l'échec (optionnel)</param>
    public void FailMission(string reason = null)
    {
        if (activeMission == null) return;

        // Arrêt du compte à rebours
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        // Incrémentation du compteur d'échecs
        FailedMissions++;

        // Jouer le son d'échec
        if (missionFailedSound != null)
            AudioSource.PlayClipAtPoint(missionFailedSound, Camera.main.transform.position);

        // Affichage du résultat négatif
        if (UIManager.Instance != null)
            UIManager.Instance.ShowMissionResult(false);

        // Enregistrement du statut de la mission
        missionCompletionStatus[activeMission] = false;

        // Log de l'échec
        if (logEvents)
            Debug.Log($"MissionManager: Mission échouée - {activeMission.missionTitle}{(reason != null ? $" ({reason})" : "")}");

        // Déclenchement de l'événement OnMissionCompleted
        OnMissionCompleted?.Invoke(activeMission, false);

        // Passage à la mission suivante ou réessai
        if (restartFailedMissions && CurrentAttempt < maxFailedAttempts)
        {
            CurrentAttempt++;
            resultCoroutine = StartCoroutine(RestartMissionAfterDelay());
        }
        else
        {
            resultCoroutine = StartCoroutine(AdvanceAfterResult());
        }
    }
    
    public void ResetObjectivesProgress()
    {
        if (activeMission == null) return;

        foreach (var objective in activeMission.objectives)
        {
            objective.ResetProgress();
        }

        UIManager.Instance?.RefreshMissionUI(activeMission);
    }
    
    public void ResetKillObjectives()
    {
        if (activeMission == null) return;
        foreach (var o in activeMission.objectives)
        {
            if (o.type == ObjectiveType.Kill)
                o.currentCount = 0;
        }
        UIManager.Instance?.RefreshMissionUI(activeMission);
    }

    public void SkipCurrentMission()
    {
        if (activeMission == null) return;

        // Arrêt du compte à rebours
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        // Passage à la mission suivante
        CurrentMissionIndex++;
        
        if (CurrentMissionIndex < allMissions.Count)
            ActivateMission(allMissions[CurrentMissionIndex]);
        else
        {
            activeMission = null;
            
            if (UIManager.Instance != null)
                UIManager.Instance.ClearMissionUI();
            
            Debug.Log("Toutes les missions ont été terminées ou ignorées !");
            OnAllMissionsCompleted?.Invoke();
        }
    }

    /// <summary>
    /// Retourne toutes les missions complétées.
    /// </summary>
    public List<Mission> GetCompletedMissions()
    {
        var completed = new List<Mission>(missionCompletionStatus.Count);

        foreach (var (mission, isCompleted) in missionCompletionStatus)
        {
            if (isCompleted)
                completed.Add(mission);
        }

        return completed;
    }

    public void ResetProgress()
    {
        CurrentMissionIndex = 0;
        CompletedMissions = 0;
        FailedMissions = 0;
        CurrentAttempt = 1;
        missionCompletionStatus.Clear();
        waitingForWavesToComplete = false;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
            resultCoroutine = null;
        }

        if (allMissions.Count > 0)
            ActivateMission(allMissions[0]);
        else
            activeMission = null;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideMissionResult();
            if (activeMission != null)
                UIManager.Instance.RefreshMissionUI(activeMission);
            else
                UIManager.Instance.ClearMissionUI();
        }
    }
    #endregion

    #region Private Methods
    private void CompleteMission()
    {
        // Arrêt du compte à rebours
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        // Attribution des récompenses
        XPSystem.AddXP(activeMission.rewardXP);
        
        // Apply reputation rewards
        foreach (var reward in activeMission.reputationRewards)
        {
            if (ReputationManager.instance != null)
            ReputationManager.instance.ChangeReputation(reward.factionName, reward.reputationChange);
        }
        // Mise à jour de l'interface utilisateur
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateXP(XPSystem.CurrentXP);
            UIManager.Instance.UpdateLevel(XPSystem.CurrentLevel);
            UIManager.Instance.ShowMissionResult(true);
        }

        // Incrémentation du compteur de missions complétées
        CompletedMissions++;

        // Enregistrement du statut de la mission
        missionCompletionStatus[activeMission] = true;

        // Déclenchement de l'événement OnMissionCompleted
        OnMissionCompleted?.Invoke(activeMission, true);

        // Passage à la mission suivante
        resultCoroutine = StartCoroutine(AdvanceAfterResult());
    }

    private IEnumerator AdvanceAfterResult()
    {
        float delay = UIManager.Instance != null ? UIManager.Instance.ResultDisplayDuration : advanceDelay;
        yield return new WaitForSeconds(delay);

        if (UIManager.Instance != null)
            UIManager.Instance.HideMissionResult();

        if (autoAdvanceToNextMission)
        {
            CurrentMissionIndex++;
            if (CurrentMissionIndex < allMissions.Count)
            {
                ActivateMission(allMissions[CurrentMissionIndex]);
            }
            else
            {
                activeMission = null;
                
                if (UIManager.Instance != null)
                    UIManager.Instance.ClearMissionUI();
                
                Debug.Log("Toutes les missions ont été terminées !");
                OnAllMissionsCompleted?.Invoke();
            }
        }
    }

    private IEnumerator RestartMissionAfterDelay()
    {
        float delay = UIManager.Instance != null ? UIManager.Instance.ResultDisplayDuration : advanceDelay;
        yield return new WaitForSeconds(delay);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideMissionResult();
            UIManager.Instance.ShowRestartingMessage(CurrentAttempt, maxFailedAttempts);
        }

        yield return new WaitForSeconds(1.5f);

        if (activeMission != null)
            ActivateMission(activeMission); // Redémarrage de la même mission
    }

    private IEnumerator MissionCountdown(float duration)
    {
        float remainingTime = duration;
        
        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;

            // Mise à jour du temps restant dans l'interface utilisateur
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateMissionTimer(remainingTime);
        }

        // Échec de la mission si le temps est écoulé
        if (activeMission != null && !activeMission.IsCompleted)
            FailMission("Temps écoulé");
    }
    #endregion
}