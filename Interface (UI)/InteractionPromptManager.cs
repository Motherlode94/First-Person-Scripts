using UnityEngine;
using TMPro;
using System.Collections;
using System;

/// <summary>
/// Manages interaction prompts throughout the game with proper visibility and transitions
/// </summary>
public class InteractionPromptManager : MonoBehaviour
{
    // Singleton instance
    public static InteractionPromptManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UnityEngine.UI.Image backgroundPanel; // Background panel
    
    [Header("Text Style")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float outlineWidth = 0.3f;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float fontSize = 24f; // Larger font size
    [SerializeField] private float backgroundOpacity = 0.7f; // Background opacity
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 5f;
    [SerializeField] private float minVisibleAlpha = 0.8f; // Minimum alpha when visible
    
    private bool isVisible = false;
    private float targetAlpha = 0f;
    private Coroutine fadeCoroutine;

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
        
        // Configure text for better visibility
        ConfigureTextVisibility();
        
        // Configure background if present
        ConfigureBackground();
        
        // Initialize in hidden state
        if (promptPanel != null)
        {
            promptPanel.SetActive(true); // Keep active but set alpha to 0
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }

private void ConfigureTextVisibility()
{
    if (promptText != null)
    {
        // Attendre une frame pour garantir que les matériaux sont initialisés
        StartCoroutine(DelayedTextConfig());
        
        // Ces propriétés sont sûres même sans le material
        promptText.color = textColor;
        promptText.fontSize = fontSize;
        promptText.fontStyle = FontStyles.Bold;
        promptText.enableWordWrapping = true;
        promptText.alignment = TextAlignmentOptions.Center;
    }
    else
    {
        Debug.LogWarning("InteractionPromptManager: promptText n'est pas assigné dans l'inspecteur");
    }
}

private IEnumerator DelayedTextConfig()
{
    // Attendre que le canvas ait eu le temps de s'initialiser complètement
    yield return new WaitForEndOfFrame();
    
    if (promptText != null && promptText.materialForRendering != null)
    {
        try
        {
            promptText.outlineWidth = outlineWidth;
            promptText.outlineColor = outlineColor;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Impossible de configurer l'outline: {e.Message}");
        }
    }
}
    
    private void ConfigureBackground()
    {
        if (backgroundPanel != null)
        {
            // Set a dark color for the background
            Color bgColor = backgroundPanel.color;
            bgColor.a = backgroundOpacity;
            backgroundPanel.color = bgColor;
        }
    }

    private void Update()
    {
        // Handle fading in/out when not using coroutines
        if (canvasGroup != null && promptPanel.activeSelf && fadeCoroutine == null)
        {
            if (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
            {
                float speed = targetAlpha > 0.5f ? fadeInSpeed : fadeOutSpeed;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * speed);
            }
        }
    }

    /// <summary>
    /// Shows the interaction prompt with the specified text
    /// </summary>
    public void ShowPrompt(string text)
    {
        if (promptText != null)
            promptText.text = text;
            
        if (promptPanel != null && !promptPanel.activeSelf)
            promptPanel.SetActive(true);
            
        isVisible = true;
        
        // Stop any running fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        // Start new fade coroutine
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f));
    }

    /// <summary>
    /// Hides the interaction prompt
    /// </summary>
    public void HidePrompt()
    {
        isVisible = false;
        
        // Stop any running fade coroutine
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        // Start new fade coroutine
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f));
    }
    
    /// <summary>
    /// Updates the prompt text without changing visibility
    /// </summary>
    public void UpdatePromptText(string text)
    {
        if (promptText != null)
            promptText.text = text;
        
        // If the prompt isn't already visible, make it visible
        if (!isVisible)
            ShowPrompt(text);
    }
    
    /// <summary>
    /// Makes the text flash to attract attention
    /// </summary>
    public void FlashPrompt(float duration = 0.5f)
    {
        if (promptText != null && gameObject.activeSelf)
        {
            StartCoroutine(FlashTextCoroutine(duration));
        }
    }
    
    /// <summary>
    /// Fades the canvas group to the target alpha
    /// </summary>
    private IEnumerator FadeCanvasGroup(float targetValue)
    {
        if (canvasGroup == null || promptPanel == null)
        {
            Debug.LogWarning("InteractionPromptManager: CanvasGroup or PromptPanel is null!");
            yield break;
        }
        
        float startAlpha = canvasGroup.alpha;
        float duration = targetValue > 0.5f ? (1f / fadeInSpeed) : (1f / fadeOutSpeed);
        float elapsed = 0f;
        
        // Enable/disable interaction based on visibility
        canvasGroup.interactable = targetValue > 0.5f;
        canvasGroup.blocksRaycasts = targetValue > 0.5f;
        
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetValue, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = targetValue;
        
        // If fully hidden, panel can be deactivated for performance
        if (targetValue <= 0.01f && promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
        
        fadeCoroutine = null;
    }
    
    /// <summary>
    /// Creates a flashing effect on the text
    /// </summary>
    private IEnumerator FlashTextCoroutine(float duration)
    {
        if (promptText == null) yield break;
        
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