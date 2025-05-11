using UnityEngine;
using TMPro;
using NeoFPS;
using NeoFPS.SinglePlayer;

public class InteractionManager : MonoBehaviour, IPlayerCharacterSubscriber
{
    [Header("Paramètres")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableMask;
    
    [Header("UI")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Effets")]
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 5f;
    
    private IInteractable currentInteractable;
    private bool isPlayerControlsEnabled = true;
    private SoloPlayerCharacterEventWatcher m_CharacterWatcher = null;
    
    private void Awake()
    {
        // Initialiser le Canvas Group si nécessaire
        if (canvasGroup == null && interactionUI != null)
            canvasGroup = interactionUI.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null && interactionUI != null)
            canvasGroup = interactionUI.AddComponent<CanvasGroup>();
            
        // S'abonner au changement de personnage
        m_CharacterWatcher = FindObjectOfType<SoloPlayerCharacterEventWatcher>();
        if (m_CharacterWatcher != null)
            m_CharacterWatcher.AttachSubscriber(this);
            
        // Initialiser l'UI
        HideInteractionUI();
    }
    
    private void OnDestroy()
    {
        if (m_CharacterWatcher != null)
            m_CharacterWatcher.ReleaseSubscriber(this);
    }
    
// Correction pour InteractionManager.cs - Méthode Update()
private void Update()
{
    if (!isPlayerControlsEnabled || FpsSoloCharacter.localPlayerCharacter == null)
        return;
            
    // Raycast depuis la caméra du personnage
    Camera fpsCam = FpsSoloCharacter.localPlayerCharacter.gameObject.GetComponentInChildren<Camera>();
    if (fpsCam == null)
        return;
            
    // Ajout de debug pour vérifier la distance
    Debug.DrawRay(fpsCam.transform.position, fpsCam.transform.forward * interactionDistance, Color.yellow);
            
    if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, interactionDistance, interactableMask))
    {
        // Sans filtrer par layer
        IInteractable interactable = hit.collider.GetComponent<IInteractable>();
        if (interactable == null)
            interactable = hit.collider.GetComponentInParent<IInteractable>();
                
        if (interactable != null)
        {
            // Debug pour confirmer que l'objet interactable est détecté
            Debug.Log($"Interactable détecté: {hit.collider.name}, distance: {hit.distance}");
                
            // Afficher l'UI d'interaction
            ShowInteractionUI(interactable.GetInteractionText());
            currentInteractable = interactable;
                
            // Interagir si le joueur utilise l'action d'interaction
            if (IsInteractionTriggered())
            {
                interactable.Interact(FpsSoloCharacter.localPlayerCharacter.gameObject);
            }
                
            return;
        }
    }
        
    // Aucun objet interactif trouvé
    currentInteractable = null;
    HideInteractionUI();
}

// Assurez-vous que ShowInteractionUI fonctionne correctement
private void ShowInteractionUI(string text)
{
    // Vérifier si InteractionPromptManager.Instance existe
    if (InteractionPromptManager.Instance != null)
    {
        InteractionPromptManager.Instance.ShowPrompt(text);
        Debug.Log($"Affichage du prompt via manager: {text}");
    }
    else
    {
        // Utiliser une méthode alternative si le manager n'existe pas
        if (interactionUI != null)
        {
            interactionUI.SetActive(true);
            if (interactionText != null)
                interactionText.text = text;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            Debug.Log($"Affichage du prompt direct: {text}");
        }
        else
        {
            Debug.LogError("InteractionUI n'est pas assigné dans l'inspecteur!");
        }
    }
}
    
    private void HideInteractionUI()
    {
        // Vérifier si InteractionPromptManager.Instance existe
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.HidePrompt();
        }
        else
        {
            // Utiliser une méthode alternative si le manager n'existe pas
            if (interactionUI != null)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
                interactionUI.SetActive(false);
            }
        }
    }
private bool IsInteractionTriggered()
{
    return Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Interact");
}
    
    public void EnableControls(bool enable)
    {
        isPlayerControlsEnabled = enable;
        
        // Désactiver/réactiver les contrôles du joueur si nécessaire
        if (FpsSoloCharacter.localPlayerCharacter != null)
        {
            // Approche alternative pour désactiver les contrôles sans dépendre de MotionController
            // Désactiver les composants de mouvement par leur nom de type
            var components = FpsSoloCharacter.localPlayerCharacter.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                // Vérifier si le nom du type contient des mots-clés liés au mouvement
                string typeName = comp.GetType().Name.ToLower();
                if (typeName.Contains("motion") || typeName.Contains("controller") || 
                    typeName.Contains("input") || typeName.Contains("movement"))
                {
                    comp.enabled = enable;
                }
            }
                
            // Désactiver le look controller et les caméras
            var fpsCameras = FpsSoloCharacter.localPlayerCharacter.GetComponentsInChildren<Camera>();
            foreach (var cam in fpsCameras)
            {
                // Désactiver les scripts de caméra mais pas la caméra elle-même
                var camComponents = cam.GetComponents<MonoBehaviour>();
                foreach (var camComp in camComponents)
                {
                    string typeName = camComp.GetType().Name.ToLower();
                    if (typeName.Contains("look") || typeName.Contains("camera") || 
                        typeName.Contains("firstperson"))
                    {
                        camComp.enabled = enable;
                    }
                }
            }
        }
    }
    
    // Implémentation de IPlayerCharacterSubscriber
    public void OnPlayerCharacterChanged(ICharacter character)
    {
        // Réinitialiser l'UI quand le personnage change
        HideInteractionUI();
        currentInteractable = null;
    }
}