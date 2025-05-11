using UnityEngine;
using TMPro;
using System.Collections;

public class HoldPromptUI : MonoBehaviour
{
    // Singleton pattern
    public static HoldPromptUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text holdTimeText;
    [SerializeField] private UnityEngine.UI.Image progressBar;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UnityEngine.UI.Image backgroundPanel; // Fond derrière le texte

    [Header("Visibilité du texte")]
    [SerializeField] private float textOutlineWidth = 0.3f;
    [SerializeField] private Color textOutlineColor = Color.black;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private bool useBoldText = true;
    [SerializeField] private float backgroundAlpha = 0.7f; // Opacité du fond

    [Header("Fade Settings")]
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 5f;
    [SerializeField] private float minAlpha = 0.8f; // Augmenté pour être plus visible

    private bool isDestroyed = false; // Flag to track if this object has been destroyed
    private Coroutine fadeCoroutine;
    private float maxHoldTime = 2f;

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

    // Initially HIDE the panel completely
    if (promptPanel != null)
        promptPanel.SetActive(false);

    if (canvasGroup != null)
        canvasGroup.alpha = 0f;
        
    // Don't call ConfigureTextVisibility directly - use coroutine instead
    StartCoroutine(DelayedInitialization());
}

private IEnumerator DelayedInitialization()
{
    // Wait for end of frame to ensure TextMeshPro components are properly initialized
    yield return new WaitForEndOfFrame();
    
    // Now safely configure text and background
    ConfigureTextVisibility();
    ConfigureBackground();
}

private void ConfigureTextVisibility()
{
    try
    {
        // Configurer le texte principal
        if (promptText != null)
        {
            // Set safe properties first
            promptText.color = textColor;
            promptText.enableWordWrapping = true;
            promptText.overflowMode = TextOverflowModes.Ellipsis;
            
            if (useBoldText)
                promptText.fontStyle = FontStyles.Bold;
            
            // Set material-dependent properties if material is available
            if (promptText.materialForRendering != null)
            {
                promptText.outlineWidth = textOutlineWidth;
                promptText.outlineColor = textOutlineColor;
            }
        }
        
        // Configurer le texte du temps
        if (holdTimeText != null)
        {
            // Set safe properties first
            holdTimeText.color = textColor;
            
            if (useBoldText)
                holdTimeText.fontStyle = FontStyles.Bold;
            
            // Set material-dependent properties if material is available
            if (holdTimeText.materialForRendering != null)
            {
                holdTimeText.outlineWidth = textOutlineWidth;
                holdTimeText.outlineColor = textOutlineColor;
            }
        }
    }
    catch (System.Exception e)
    {
        Debug.LogWarning($"HoldPromptUI: Exception during text configuration: {e.Message}");
    }
}
    
    private void ConfigureBackground()
    {
        // Si un fond est spécifié, ajuster son opacité
        if (backgroundPanel != null)
        {
            Color bgColor = backgroundPanel.color;
            bgColor.a = backgroundAlpha;
            backgroundPanel.color = bgColor;
        }
    }

    private void OnDestroy()
    {
        // Set the destroyed flag
        isDestroyed = true;
        
        // Clear the singleton instance if it's this one
        if (Instance == this)
            Instance = null;
            
        // Stop any running coroutines
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
    }

public void ShowPrompt(string text, float holdTime = 0f)
{
    // Safety check - don't try to use a destroyed object
    if (isDestroyed)
        return;
            
    // Store max hold time for progress calculations
    maxHoldTime = holdTime > 0 ? holdTime : 2f;
            
    // Stop any existing fade coroutine
    if (fadeCoroutine != null)
        StopCoroutine(fadeCoroutine);

    // Set the text
    if (promptText != null)
        promptText.text = text;

    // Set the hold time if provided
    if (holdTimeText != null && holdTime > 0)
    {
        holdTimeText.text = holdTime.ToString("F1") + "s";
        holdTimeText.gameObject.SetActive(true);
    }
    else if (holdTimeText != null)
    {
        holdTimeText.gameObject.SetActive(false);
    }

    // Reset progress bar if available
    if (progressBar != null)
    {
        progressBar.fillAmount = 0f;
    }

    // Ensure panel is active before fading in
    if (promptPanel != null)
        promptPanel.SetActive(true);

    // Fade in
    fadeCoroutine = StartCoroutine(FadeIn());
}

public void HidePrompt()
{
    // Safety check - don't try to use a destroyed object
    if (isDestroyed)
        return;
            
    // Stop any existing fade coroutine
    if (fadeCoroutine != null)
        StopCoroutine(fadeCoroutine);

    // Start fade out
    fadeCoroutine = StartCoroutine(FadeOut());
    
    // Important: Désactiver complètement après le fade out
    StartCoroutine(DisableAfterFadeOut());
}

private IEnumerator DisableAfterFadeOut()
{
    yield return new WaitForSeconds(0.3f); // Durée du fade out
    
    if (promptPanel != null && !isDestroyed)
        promptPanel.SetActive(false);
}

    public void UpdateHoldTime(float remainingTime)
    {
        // Safety check - don't try to use a destroyed object
        if (isDestroyed)
            return;
            
        if (holdTimeText != null)
            holdTimeText.text = remainingTime.ToString("F1") + "s";
            
        // Update progress bar if available
        UpdateProgress(maxHoldTime - remainingTime);
    }
    
    public void UpdateProgress(float currentHoldTime)
    {
        // Safety check - don't try to use a destroyed object
        if (isDestroyed)
            return;
            
        // Update progress bar if available
        if (progressBar != null && maxHoldTime > 0)
        {
            progressBar.fillAmount = Mathf.Clamp01(currentHoldTime / maxHoldTime);
        }
    }

    private IEnumerator FadeIn()
    {
        // Safety check - don't try to use a destroyed object
        if (isDestroyed)
            yield break;
            
        float duration = 0.3f;
        float elapsed = 0f;

        if (canvasGroup != null)
        {
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < duration)
            {
                // Safety check in case the object gets destroyed during the coroutine
                if (isDestroyed || canvasGroup == null)
                    yield break;
                    
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }
    }

    private IEnumerator FadeOut()
    {
        // Safety check - don't try to use a destroyed object
        if (isDestroyed)
            yield break;
            
        float duration = 0.3f;
        float elapsed = 0f;

        if (canvasGroup != null)
        {
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < duration)
            {
                // Safety check in case the object gets destroyed during the coroutine
                if (isDestroyed || canvasGroup == null)
                    yield break;
                    
                canvasGroup.alpha = Mathf.Lerp(startAlpha, minAlpha, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = minAlpha;
        }
    }
}