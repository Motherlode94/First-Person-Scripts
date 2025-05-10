using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère l'affichage d'un prompt d'interaction pour accepter ou refuser une mission
/// lorsque le joueur est à proximité.
/// </summary>
public class MissionInteractionPrompt : MonoBehaviour
{
    [Header("Configuration de la Mission")]
    [SerializeField] private Mission mission;
    [SerializeField] private string missionID; // Alternatif: ID de mission à charger depuis MissionManager
    [SerializeField] private float interactionDistance = 3f; // Distance d'interaction

    [Header("Interface Utilisateur")]
    [SerializeField] private GameObject promptCanvas; // Canvas contenant l'UI d'interaction
    [SerializeField] private TextMeshProUGUI promptText; // Texte "Interagissez pour..."
    [SerializeField] private GameObject missionDetailsPanel; // Panel des détails de mission
    [SerializeField] private TextMeshProUGUI missionTitleText; // Titre de la mission
    [SerializeField] private TextMeshProUGUI missionDescriptionText; // Description
    [SerializeField] private TextMeshProUGUI missionRewardsText; // Récompenses
    [SerializeField] private TextMeshProUGUI difficultyText; // Difficulté
    [SerializeField] private Button acceptButton; // Bouton Accepter
    [SerializeField] private Button declineButton; // Bouton Refuser

    [Header("Opacité")]
    [SerializeField] private float minPromptAlpha = 0.1f; // Opacité minimale au lieu de masquer
    [SerializeField] private float minDetailsAlpha = 0.05f; // Opacité minimale pour les détails

    [Header("Audio")]
    [SerializeField] private AudioClip promptSound; // Son quand le prompt apparaît
    [SerializeField] private AudioClip acceptSound; // Son quand mission acceptée
    [SerializeField] private AudioClip declineSound; // Son quand mission refusée

    // Variables privées
    private Transform playerTransform;
    private bool inRange = false;
    private bool panelOpen = false;
    private AudioSource audioSource;
    private CanvasGroup promptCanvasGroup;
    private CanvasGroup detailsCanvasGroup;
    private Coroutine promptFadeCoroutine;
    private Coroutine detailsFadeCoroutine;

    // Événements pour les autres systèmes
    public delegate void MissionInteractionHandler(Mission mission, bool accepted);
    public event MissionInteractionHandler OnMissionResponse;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Son 3D
            audioSource.minDistance = interactionDistance;
            audioSource.maxDistance = interactionDistance * 3;
        }

        // Configurer les CanvasGroups
        if (promptCanvas != null)
        {
            promptCanvasGroup = promptCanvas.GetComponent<CanvasGroup>();
            if (promptCanvasGroup == null)
                promptCanvasGroup = promptCanvas.AddComponent<CanvasGroup>();
                
            // Garder l'objet actif mais avec une faible opacité
            promptCanvas.SetActive(true);
            promptCanvasGroup.alpha = minPromptAlpha;
        }
        
        if (missionDetailsPanel != null)
        {
            detailsCanvasGroup = missionDetailsPanel.GetComponent<CanvasGroup>();
            if (detailsCanvasGroup == null)
                detailsCanvasGroup = missionDetailsPanel.AddComponent<CanvasGroup>();
                
            // Garder l'objet actif mais avec une faible opacité
            missionDetailsPanel.SetActive(true);
            detailsCanvasGroup.alpha = minDetailsAlpha;
            
            // Désactiver les interactions lorsque non visible
            detailsCanvasGroup.interactable = false;
            detailsCanvasGroup.blocksRaycasts = false;
        }

        // Configurer les boutons
        if (acceptButton)
            acceptButton.onClick.AddListener(AcceptMission);
        if (declineButton)
            declineButton.onClick.AddListener(DeclineMission);
    }

    private void Start()
    {
        // Trouver le joueur
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Chercher la mission par ID si non assignée directement
        if (mission == null && !string.IsNullOrEmpty(missionID) && MissionManager.Instance != null)
        {
            foreach (var m in MissionManager.Instance.AllMissions)
            {
                if (m.missionID == missionID)
                {
                    mission = m;
                    break;
                }
            }
        }
        
        if (mission == null)
        {
            Debug.LogError("MissionInteractionPrompt: Aucune mission assignée !");
        }
    }

    private void Update()
    {
        if (playerTransform == null || mission == null)
            return;

        // Vérifier si le joueur est à portée
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool playerInRange = distance <= interactionDistance;
        
        // Si le statut de la portée a changé
        if (playerInRange != inRange)
        {
            inRange = playerInRange;
            
            // Afficher ou masquer le prompt
            if (inRange && !panelOpen)
            {
                ShowPrompt();
            }
            else if (!inRange)
            {
                HidePrompt();
                HideMissionDetails();
            }
        }
        
        // Si le joueur est à portée et utilise l'interaction principale
        if (inRange && IsInteractionTriggered() && !panelOpen)
        {
            ShowMissionDetails();
        }
        
        // Si le joueur appuie sur Escape et le panneau est ouvert
        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            HideMissionDetails();
        }
    }
    
    // Nouvelle méthode pour déterminer si l'interaction est déclenchée
    private bool IsInteractionTriggered()
    {
        // Utiliser le système d'input standard
        return Input.GetButtonDown("Submit") || Input.GetButtonDown("Fire1");
        
        // Vous pouvez remplacer cette méthode par votre propre système
        // d'interaction, par exemple en utilisant l'API Input System
    }

    private void ShowPrompt()
    {
        if (promptCanvasGroup != null)
        {
            // Arrêter toute transition en cours
            if (promptFadeCoroutine != null)
                StopCoroutine(promptFadeCoroutine);
                
            // Démarrer la transition d'opacité
            promptFadeCoroutine = StartCoroutine(FadeCanvasGroup(promptCanvasGroup, promptCanvasGroup.alpha, 1f, 0.3f));
            
            if (promptText != null)
            {
                promptText.text = "Interagissez pour discuter de la mission";
            }
            
            if (promptSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(promptSound);
            }
        }
    }

    private void HidePrompt()
    {
        if (promptCanvasGroup != null)
        {
            // Arrêter toute transition en cours
            if (promptFadeCoroutine != null)
                StopCoroutine(promptFadeCoroutine);
                
            // Démarrer la transition d'opacité
            promptFadeCoroutine = StartCoroutine(FadeCanvasGroup(promptCanvasGroup, promptCanvasGroup.alpha, minPromptAlpha, 0.3f));
        }
    }

    private void ShowMissionDetails()
    {
        HidePrompt();
        panelOpen = true;
        
        if (detailsCanvasGroup != null)
        {
            // Arrêter toute transition en cours
            if (detailsFadeCoroutine != null)
                StopCoroutine(detailsFadeCoroutine);
                
            // Activer les interactions
            detailsCanvasGroup.interactable = true;
            detailsCanvasGroup.blocksRaycasts = true;
                
            // Démarrer la transition d'opacité
            detailsFadeCoroutine = StartCoroutine(FadeCanvasGroup(detailsCanvasGroup, detailsCanvasGroup.alpha, 1f, 0.3f));
            
            // Remplir les informations de la mission
            if (missionTitleText != null)
                missionTitleText.text = mission.missionTitle;
                
            if (missionDescriptionText != null)
                missionDescriptionText.text = mission.missionDescription;
                
            if (missionRewardsText != null)
            {
                string rewardText = $"Récompenses: {mission.rewardXP} XP";
                
                // Ajouter les récompenses de réputation si présentes
                if (mission.reputationRewards != null && mission.reputationRewards.Count > 0)
                {
                    rewardText += "\nRéputation: ";
                    foreach (var rep in mission.reputationRewards)
                    {
                        string sign = rep.reputationChange > 0 ? "+" : "";
                        rewardText += $"\n{rep.factionName}: {sign}{rep.reputationChange}";
                    }
                }
                
                missionRewardsText.text = rewardText;
            }
            
            if (difficultyText != null)
            {
                // Vous pouvez adapter cette partie selon votre système de difficulté
                string difficulty = "Normal";
                if (mission.rewardXP < 100) difficulty = "Facile";
                else if (mission.rewardXP > 300) difficulty = "Difficile";
                
                difficultyText.text = $"Difficulté: {difficulty}";
            }
        }
        
        // Activer le curseur de la souris si nécessaire
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideMissionDetails()
    {
        panelOpen = false;
        
        if (detailsCanvasGroup != null)
        {
            // Arrêter toute transition en cours
            if (detailsFadeCoroutine != null)
                StopCoroutine(detailsFadeCoroutine);
                
            // Désactiver les interactions
            detailsCanvasGroup.interactable = false;
            detailsCanvasGroup.blocksRaycasts = false;
                
            // Démarrer la transition d'opacité
            detailsFadeCoroutine = StartCoroutine(FadeCanvasGroup(detailsCanvasGroup, detailsCanvasGroup.alpha, minDetailsAlpha, 0.3f));
        }
        
        // Si le joueur est toujours à portée, afficher à nouveau le prompt
        if (inRange)
        {
            ShowPrompt();
        }
        
        // Remettre le curseur comme avant si nécessaire
        // Vous devrez adapter cela selon votre système de contrôle
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void AcceptMission()
    {
        if (mission != null)
        {
            // Jouer le son d'acceptation
            if (acceptSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(acceptSound);
            }
            
            // Activer la mission si MissionManager existe
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ActivateMission(mission);
            }
            
            // Déclencher l'événement
            OnMissionResponse?.Invoke(mission, true);
            
            // Message de debug
            Debug.Log($"Mission acceptée: {mission.missionTitle}");
            
            // Fermer le panneau
            HideMissionDetails();
            
            // Désactiver ce composant pour éviter de redemander la mission
            this.enabled = false;
            
            // Réduire l'opacité du prompt au minimum plutôt que le désactiver
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = minPromptAlpha;
            }
        }
    }

    private void DeclineMission()
    {
        // Jouer le son de refus
        if (declineSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(declineSound);
        }
        
        // Déclencher l'événement
        OnMissionResponse?.Invoke(mission, false);
        
        // Message de debug
        Debug.Log($"Mission refusée: {mission.missionTitle}");
        
        // Fermer le panneau
        HideMissionDetails();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cg.alpha = targetAlpha;
    }

    // Visualisation de la zone d'interaction dans l'éditeur
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}