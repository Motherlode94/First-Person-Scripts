using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using TMPro;
public class SequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class SequenceStep
    {
        public string switchID;
        public GameObject visualHint;
        public AudioClip activationSound;
    }
    
    [Header("Sequence")]
    [SerializeField] private List<SequenceStep> correctSequence = new List<SequenceStep>();
    [SerializeField] private string sequenceCompletedObjectiveID = "PowerSequence";
    [SerializeField] private float resetDelay = 5f;
    [SerializeField] private AudioClip failureSound;
    [SerializeField] private AudioClip successSound;

    [Header("Interface UI")]
    [SerializeField] private GameObject stepCompletedMessagePrefab;
    [SerializeField] private float messageDisplayDuration = 2f;

    private List<string> currentSequence = new List<string>();
    public int CurrentStepIndex => currentSequence.Count;

    private bool sequenceCompleted = false;
    private float resetTimer = 0f;
    private bool needsReset = false;
    
    [Header("Événements")]
    public UnityEvent onSequenceCompleted = new UnityEvent();
    public UnityEvent<string> onStepCompleted = new UnityEvent<string>();

    private void Start()
    {
        // Désactiver tous les indices visuels au départ
        foreach (var step in correctSequence)
        {
            if (step.visualHint != null)
                step.visualHint.SetActive(false);
        }
        
        // Afficher l'indice pour la première étape
        if (correctSequence.Count > 0 && correctSequence[0].visualHint != null)
            correctSequence[0].visualHint.SetActive(true);
            
        // S'abonner à l'événement OnMissionActivated
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionActivated += OnMissionActivated;
        }
        else
        {
            // Attendre un court instant puis tenter de s'abonner à nouveau
            StartCoroutine(TrySubscribeToMissionManager());
        }
    }

    private void OnMissionActivated(Mission mission)
    {
        // Mettre à jour la visibilité des objectifs
        UpdateObjectivesVisibility();
    }

    private IEnumerator TrySubscribeToMissionManager()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionActivated += OnMissionActivated;
        }
    }

    private void OnDestroy()
    {
        // Se désabonner de l'événement
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionActivated -= OnMissionActivated;
        }
    }

    private void Update()
    {
        // Réinitialiser la séquence en cas de timeout
        if (needsReset)
        {
            resetTimer += Time.deltaTime;
            if (resetTimer >= resetDelay)
            {
                ResetSequence();
                needsReset = false;
                resetTimer = 0f;
            }
        }
    }

    // Méthode revue pour améliorer l'affichage séquentiel
public void UpdateObjectivesVisibility()
{
    // Vérifier d'abord si une mission active existe
    Mission activeMission = MissionManager.Instance?.ActiveMission;
    if (activeMission == null)
    {
        Debug.LogWarning("SequenceManager: Aucune mission active pour mettre à jour la visibilité des objectifs");
        return;
    }
    
    // Déterminer le SwitchID actuel pour le mode progressif
    string currentSwitchID = GetCurrentSwitchID();
    
    // Notifier UIManager du nouveau switchID
    if (UIManager.Instance != null)
    {
        UIManager.Instance.UpdateCurrentSwitchID(currentSwitchID);
    }
    
    // Pour la compatibilité avec le code existant, continuer à régler la visibilité des objectifs
    if (activeMission.revealObjectivesProgressively)
    {
        foreach (var objective in activeMission.objectives)
        {
            objective.visible = false;
        }
        
        // Toujours rendre visibles les objectifs déjà complétés
        foreach (var objective in activeMission.objectives)
        {
            if (objective.IsCompleted)
            {
                objective.visible = true;
                continue;
            }
            
            // Vérifier si cet objectif correspond au SwitchID actuel
            if (objective.targetID == currentSwitchID)
            {
                objective.visible = true;
                break; // N'afficher que l'objectif actuel
            }
        }
        
        // Si la séquence est complétée, tous les objectifs sont visibles
        if (sequenceCompleted)
        {
            foreach (var objective in activeMission.objectives)
            {
                objective.visible = true;
            }
        }
    }
    
    // Mettre à jour l'interface utilisateur
    if (UIManager.Instance != null)
    {
        UIManager.Instance.RefreshMissionUI(activeMission);
    }
}

// Simplifier ShowStepCompletedMessage
private void ShowStepCompletedMessage(int stepIndex)
{
    if (UIManager.Instance != null)
    {
        UIManager.Instance.ShowTemporaryMessage($"Étape {stepIndex + 1}/{correctSequence.Count} complétée!", messageDisplayDuration);
    }
}
    public void NotifySwitchActivated(string switchID)
    {
        if (sequenceCompleted) return;

        currentSequence.Add(switchID);
        int currentStep = currentSequence.Count - 1;
        
        // Vérifier si cette étape est correcte
        if (currentStep < correctSequence.Count && correctSequence[currentStep].switchID == switchID)
        {
            // Étape correcte
            if (correctSequence[currentStep].activationSound != null)
                AudioSource.PlayClipAtPoint(correctSequence[currentStep].activationSound, Camera.main.transform.position);
            
            // Notifier de l'étape complétée
            onStepCompleted?.Invoke(switchID);
            
            // Afficher un message de succès pour cette étape
            ShowStepCompletedMessage(currentStep);
            
            // Montrer l'indice pour l'étape suivante si disponible
            if (currentStep + 1 < correctSequence.Count)
            {
                if (correctSequence[currentStep].visualHint != null)
                    correctSequence[currentStep].visualHint.SetActive(false);
                    
                if (correctSequence[currentStep + 1].visualHint != null)
                    correctSequence[currentStep + 1].visualHint.SetActive(true);
            }
            
            // Vérifier si la séquence est complète
            if (currentStep == correctSequence.Count - 1)
            {
                SequenceCompleted();
            }
        }
        else
        {
            // Étape incorrecte - démarrer le timer de réinitialisation
            if (failureSound != null)
                AudioSource.PlayClipAtPoint(failureSound, Camera.main.transform.position);
                
            needsReset = true;
            resetTimer = 0f;
        }
        
        // Mettre à jour l'affichage des objectifs
        UpdateObjectivesVisibility();
    }

    public string GetCurrentSwitchID()
    {
        int step = currentSequence.Count;
        if (step < correctSequence.Count)
            return correctSequence[step].switchID;
        return null;
    }

    private void SequenceCompleted()
    {
        sequenceCompleted = true;
        
        if (successSound != null)
            AudioSource.PlayClipAtPoint(successSound, Camera.main.transform.position);
            
        // Désactiver tous les indices visuels
        foreach (var step in correctSequence)
        {
            if (step.visualHint != null)
                step.visualHint.SetActive(false);
        }
        
        // Notifier le système de mission
        if (MissionManager.Instance != null)
        {
            // Utiliser le type approprié selon votre configuration
            if (System.Enum.IsDefined(typeof(ObjectiveType), "CompleteSequence"))
                MissionManager.Instance.NotifyObjectives(ObjectiveType.CompleteSequence, id: sequenceCompletedObjectiveID);
            else
                MissionManager.Instance.NotifyObjectives(ObjectiveType.ActivateSwitch, id: sequenceCompletedObjectiveID);
        }
        
        // Déclencher l'événement de complétion
        onSequenceCompleted?.Invoke();
        
        // Mettre à jour la visibilité une dernière fois
        UpdateObjectivesVisibility();
            
        Debug.Log("[SequenceManager] Sequence completed successfully!");
    }

    private void ResetSequence()
    {
        currentSequence.Clear();
        
        // Désactiver tous les indices sauf le premier
        for (int i = 0; i < correctSequence.Count; i++)
        {
            if (correctSequence[i].visualHint != null)
            {
                correctSequence[i].visualHint.SetActive(i == 0);
            }
        }
        
        // Mettre à jour la visibilité des objectifs après réinitialisation
        UpdateObjectivesVisibility();
        
        Debug.Log("[SequenceManager] Sequence reset!");
    }

    public IReadOnlyList<SequenceStep> CorrectSequence => correctSequence;
}