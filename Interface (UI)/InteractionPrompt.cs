using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float fadeSpeed = 0f;
    [SerializeField] private float textOutlineWidth = 0.2f; // Ajout d'un contour
    [SerializeField] private Color textOutlineColor = Color.black; // Couleur du contour
    [SerializeField] private Color textColor = Color.white; // Couleur du texte
    [SerializeField] private float minAlpha = 1f; // Alpha minimum augmenté
    
    private static InteractionPrompt _instance;
    public static InteractionPrompt Instance => _instance;
    
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    
    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
            Destroy(gameObject);
            
        // Récupérer ou ajouter un CanvasGroup
        canvasGroup = promptPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = promptPanel.AddComponent<CanvasGroup>();
            
        // Initialiser l'UI comme visible mais avec alpha minimum
        promptPanel.SetActive(true);
        canvasGroup.alpha = minAlpha;
        
        // Améliorer la visibilité du texte
        if (promptText != null)
        {
            // Ajouter un contour au texte
            promptText.outlineWidth = textOutlineWidth;
            promptText.outlineColor = textOutlineColor;
            
            // Mettre le texte en gras et augmenter la taille
            promptText.fontStyle = FontStyles.Bold;
            promptText.fontSize += 2;
            
            // Définir la couleur du texte
            promptText.color = textColor;
        }
    }
    
    public void ShowPrompt(string message)
    {
        // Arrêter toute transition en cours
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        // Activer le panneau
        promptPanel.SetActive(true);
            
        // Mettre à jour le texte
        if (promptText != null)
            promptText.text = message;
            
        // Faire apparaître le prompt avec une transition
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f));
    }
    
    public void HidePrompt()
    {
        // Arrêter toute transition en cours
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        // Masquer le prompt avec une transition
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(minAlpha));
    }
    
    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        if (canvasGroup == null) yield break;
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = Mathf.Abs(targetAlpha - startAlpha) / fadeSpeed;
        
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
    }
}