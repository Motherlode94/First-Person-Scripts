using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;
    public string speakerName;
    public Sprite speakerPortrait;
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Mode Simple")]
    [Tooltip("Utiliser le mode avancé avec des lignes personnalisées")]
    public bool useAdvancedMode = false;
    
    [Header("Mode Simple - Contenu")]
    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Header("Mode Avancé - Contenu")]
    public DialogueLine[] advancedLines;

    [Header("Options de Choix")]
    [Tooltip("Index de la ligne qui présente un choix (-1 pour aucun choix)")]
    public int choiceIndex = -1;
    [Tooltip("Texte pour le bouton Oui")]
    public string yesButtonText = "Accepter";
    [Tooltip("Texte pour le bouton Non")]
    public string noButtonText = "Refuser";
    
    [Header("Lien Mission")]
    [Tooltip("ID de la mission à activer si le joueur accepte")]
    public string missionID = "";
    
    [Header("Déclenchement")]
    public bool triggerOnStart = false;
    public bool triggerOnceOnly = false;
    public bool triggerOnCollision = false;
    public string requiredTag = "Player";
    
    [Header("Événements")]
    public UnityEvent OnDialogueTriggered;
    public UnityEvent OnPlayerAccepted;
    public UnityEvent OnPlayerRefused;

    private bool hasTriggered = false;

    private void Start()
    {
        if (triggerOnStart)
            TriggerDialogue();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnCollision && other.CompareTag(requiredTag))
        {
            TriggerDialogue();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (triggerOnCollision && collision.gameObject.CompareTag(requiredTag))
        {
            TriggerDialogue();
        }
    }

    public void TriggerDialogue()
    {
        if (triggerOnceOnly && hasTriggered)
            return;
            
        hasTriggered = true;
        
        if (DialogueManager.instance == null)
        {
            Debug.LogWarning("Aucun DialogueManager trouvé dans la scène!");
            return;
        }
        
        // Configuration des événements
        DialogueManager.instance.OnChoiceMade.AddListener(HandleChoice);
        
        // Configurer les textes des boutons
        DialogueManager.instance.SetCustomChoiceTexts(yesButtonText, noButtonText);
        
        // Configuration du premier speaker si en mode avancé
        if (useAdvancedMode && advancedLines.Length > 0)
        {
            DialogueManager.instance.SetSpeakerInfo(
                advancedLines[0].speakerName,
                advancedLines[0].speakerPortrait
            );
            
            // Convertir en tableau simple pour compatibilité
            string[] simpleLines = new string[advancedLines.Length];
            for (int i = 0; i < advancedLines.Length; i++)
            {
                simpleLines[i] = advancedLines[i].text;
            }
            
            // Sauvegarder les lignes originales
            dialogueLines = simpleLines;
        }
        
        // Démarrer le dialogue
        DialogueManager.instance.StartDialogue(this);
        OnDialogueTriggered?.Invoke();
        
        // S'abonner à l'événement pour changer de personnage à chaque ligne
        if (useAdvancedMode)
            DialogueManager.instance.OnLineShown.AddListener(UpdateSpeakerInfo);
    }
    
    private void UpdateSpeakerInfo(int lineIndex)
    {
        if (useAdvancedMode && lineIndex < advancedLines.Length)
        {
            DialogueManager.instance.SetSpeakerInfo(
                advancedLines[lineIndex].speakerName,
                advancedLines[lineIndex].speakerPortrait
            );
        }
    }
    
    private void HandleChoice(bool accepted)
    {
        if (accepted)
            OnPlayerAccepted?.Invoke();
        else
            OnPlayerRefused?.Invoke();
            
        // Se désabonner des événements
        DialogueManager.instance.OnLineShown.RemoveListener(UpdateSpeakerInfo);
        DialogueManager.instance.OnChoiceMade.RemoveListener(HandleChoice);
    }
    
    // Pour déclencher depuis d'autres scripts
    public void ForceDialogueTrigger()
    {
        hasTriggered = false; // Permet de réutiliser même si triggerOnceOnly est activé
        TriggerDialogue();
    }
}