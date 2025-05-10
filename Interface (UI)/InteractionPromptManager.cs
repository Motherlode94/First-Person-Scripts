using UnityEngine;
using TMPro;

public class InteractionPromptManager : MonoBehaviour
{
    // Singleton instance
    public static InteractionPromptManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UnityEngine.UI.Image backgroundPanel; // Fond de panneau
    
    [Header("Style de texte")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float outlineWidth = 0.3f;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float fontSize = 24f; // Taille de police plus grande
    [SerializeField] private float backgroundOpacity = 0.7f; // Opacité du fond
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 5f;
    [SerializeField] private float minVisibleAlpha = 0.8f; // Valeur minimale quand visible
    
    private bool isVisible = false;
    private float targetAlpha = 0f;

private void Awake()
{
    // Singleton pattern
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    
    Instance = this;
    
    // Ensure we have a canvas group
    if (canvasGroup == null && promptPanel != null)
    {
        canvasGroup = promptPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = promptPanel.AddComponent<CanvasGroup>();
    }
    
    // Configure le texte pour une meilleure visibilité
    ConfigureTextVisibility();
    
    // Configure le fond si présent
    ConfigureBackground();
    
    // Désactiver complètement le prompt au départ
    if (promptPanel != null)
    {
        promptPanel.SetActive(false);
    }
    if (canvasGroup != null)
    {
        canvasGroup.alpha = 0f;
    }
}

    private void ConfigureTextVisibility()
    {
        if (promptText != null)
        {
            // Couleur et taille
            promptText.color = textColor;
            promptText.fontSize = fontSize;
            
            // Contour
            promptText.outlineWidth = outlineWidth;
            promptText.outlineColor = outlineColor;
            
            // Style et lisibilité
            promptText.fontStyle = FontStyles.Bold;
            promptText.enableWordWrapping = true;
            promptText.alignment = TextAlignmentOptions.Center;
        }
    }
    
    private void ConfigureBackground()
    {
        if (backgroundPanel != null)
        {
            // Définir une couleur sombre pour le fond
            Color bgColor = backgroundPanel.color;
            bgColor.a = backgroundOpacity;
            backgroundPanel.color = bgColor;
        }
    }

private void Update()
{
    // Handle fading in/out
    if (canvasGroup != null && promptPanel.activeSelf)
    {
        if (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
        {
            float speed = targetAlpha > 0.5f ? fadeInSpeed : fadeOutSpeed;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * speed);
            
            // Disable the game object when fully faded out
            if (targetAlpha < 0.01f && canvasGroup.alpha < 0.01f && promptPanel.activeSelf)
            {
                promptPanel.SetActive(false);
            }
        }
    }
}

// Correction pour ShowPrompt
public void ShowPrompt(string text)
{
    if (promptText != null)
        promptText.text = text;
            
    if (promptPanel != null && !promptPanel.activeSelf)
        promptPanel.SetActive(true);
            
    targetAlpha = 1f; // 100% opaque
    isVisible = true;
}

    public void HidePrompt()
    {
        targetAlpha = 0f;
        isVisible = false;
    }
    
    // Added method to update the prompt text without changing visibility
    public void UpdatePromptText(string text)
    {
        if (promptText != null)
            promptText.text = text;
        
        // If the prompt isn't already visible, make it visible
        if (!isVisible && promptPanel != null)
        {
            promptPanel.SetActive(true);
            targetAlpha = 1f;
            isVisible = true;
        }
    }
    
    // Méthode pour faire clignoter le texte (attirer l'attention)
    public void FlashPrompt(float duration = 0.5f)
    {
        if (promptText != null && gameObject.activeSelf)
        {
            StartCoroutine(FlashTextCoroutine(duration));
        }
    }
    
    private System.Collections.IEnumerator FlashTextCoroutine(float duration)
    {
        Color originalColor = promptText.color;
        Color flashColor = Color.yellow;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * 5f, 1f);
            promptText.color = Color.Lerp(originalColor, flashColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        promptText.color = originalColor;
    }
}