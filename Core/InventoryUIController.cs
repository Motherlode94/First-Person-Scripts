using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private KeyCode inventoryToggleKey = KeyCode.I;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private bool pauseGameWhenOpen = true;
    
    [Header("Animation")]
    [SerializeField] private float fadeInSpeed = 0.3f;
    [SerializeField] private float fadeOutSpeed = 0.2f;
    
    private CanvasGroup canvasGroup;
    private bool isInventoryOpen = false;
    private float originalTimeScale;
    
    private void Awake()
    {
        // Obtenir ou ajouter le CanvasGroup
        canvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            
        // Cacher l'inventaire au démarrage
        inventoryPanel.SetActive(false);
        canvasGroup.alpha = 0f;
    }
    
    private void Update()
    {
        // Vérifier si la touche d'inventaire est appuyée
        if (Input.GetKeyDown(inventoryToggleKey))
        {
            ToggleInventory();
        }
        
        // Fermer l'inventaire avec Échap
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.E))
        {
            CloseInventory();
        }
    }
    
    public void ToggleInventory()
    {
        if (!isInventoryOpen)
            OpenInventory();
        else
            CloseInventory();
    }
    
    public void OpenInventory()
    {
        if (isInventoryOpen) return;
        
        // Activer le panneau
        inventoryPanel.SetActive(true);
        isInventoryOpen = true;
        
        // Animation d'ouverture
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(canvasGroup, 0f, 1f, fadeInSpeed));
        
        // Mettre en pause le jeu si configuré
        if (pauseGameWhenOpen)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        
        // Activer le curseur de la souris
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Désactiver les contrôles du joueur
        DisablePlayerControls();
        
        // Rafraîchir l'affichage de l'inventaire
        RefreshInventoryDisplay();
    }
    
    public void CloseInventory()
    {
        if (!isInventoryOpen) return;
        
        isInventoryOpen = false;
        
        // Animation de fermeture
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(canvasGroup, canvasGroup.alpha, 0f, fadeOutSpeed, () => {
            inventoryPanel.SetActive(false);
        }));
        
        // Rétablir le timeScale
        if (pauseGameWhenOpen)
        {
            Time.timeScale = originalTimeScale;
        }
        
        // Restaurer l'état du curseur (si en mode FPS)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Réactiver les contrôles du joueur
        EnablePlayerControls();
    }
    
    private void RefreshInventoryDisplay()
    {
        // Actualiser l'affichage des objets
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.RefreshUI();
        }
    }
    
    private void DisablePlayerControls()
    {
        // Désactiver les contrôles du joueur selon votre implémentation
        // Par exemple, avec votre InteractionManager
        var interactionManager = FindObjectOfType<InteractionManager>();
        if (interactionManager != null)
        {
            interactionManager.EnableControls(false);
        }
    }
    
    private void EnablePlayerControls()
    {
        // Réactiver les contrôles du joueur
        var interactionManager = FindObjectOfType<InteractionManager>();
        if (interactionManager != null)
        {
            interactionManager.EnableControls(true);
        }
    }
    
    private System.Collections.IEnumerator FadeCanvas(CanvasGroup cg, float startAlpha, float targetAlpha, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            elapsed += Time.unscaledDeltaTime; // Utiliser Time.unscaledDeltaTime pour que l'animation fonctionne même si le jeu est en pause
            yield return null;
        }
        
        cg.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}