using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIReputation : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string factionID = "Police";
    [SerializeField] private bool showReputationBar = true;
    [SerializeField] private bool showReputationLevel = true;
    [SerializeField] private bool showReputationValue = true;
    
    [Header("Éléments UI")]
    [SerializeField] private Image factionIcon;
    [SerializeField] private TextMeshProUGUI factionNameText;
    [SerializeField] private TextMeshProUGUI reputationValueText;
    [SerializeField] private TextMeshProUGUI reputationLevelText;
    [SerializeField] private Slider reputationSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    
    [Header("Couleurs par niveau")]
    [SerializeField] private Color hostileColor = new Color(0.8f, 0.1f, 0.1f);
    [SerializeField] private Color suspiciousColor = new Color(0.8f, 0.6f, 0.1f);
    [SerializeField] private Color neutralColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color friendlyColor = new Color(0.1f, 0.6f, 0.1f);
    [SerializeField] private Color alliedColor = new Color(0.1f, 0.8f, 0.8f);
    
    [Header("Animations")]
    [SerializeField] private float updateSpeed = 2f;
    [SerializeField] private bool pulseOnChange = true;
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float pulseScale = 1.2f;
    
    // Variables privées
    private float targetSliderValue = 0.5f;
    private Coroutine pulseCoroutine;
    private Coroutine updateCoroutine;
    private ReputationManager.ReputationLevel currentLevel = ReputationManager.ReputationLevel.Neutral;

    private void Start()
    {
        // Initialisation
        if (ReputationManager.instance != null)
        {
            // S'abonner aux événements
            ReputationManager.instance.OnReputationChanged += HandleReputationChanged;
            ReputationManager.instance.OnReputationLevelChanged += HandleReputationLevelChanged;
            
            // Charger les données de faction
            ReputationManager.FactionData factionData = ReputationManager.instance.GetFactionData(factionID);
            if (factionData != null)
            {
                if (factionNameText != null)
                    factionNameText.text = factionData.displayName;
                    
                if (factionIcon != null && factionData.icon != null)
                    factionIcon.sprite = factionData.icon;
            }
            
            // Mettre à jour l'UI avec les valeurs initiales
            UpdateUI(false);
        }
    }
    
    private void OnDestroy()
    {
        // Se désabonner des événements
        if (ReputationManager.instance != null)
        {
            ReputationManager.instance.OnReputationChanged -= HandleReputationChanged;
            ReputationManager.instance.OnReputationLevelChanged -= HandleReputationLevelChanged;
        }
    }

    private void Update()
    {
        // Mise à jour en continu (en cas de besoin)
        if (ReputationManager.instance == null) return;
        
        // Animation douce du slider
        if (reputationSlider != null && reputationSlider.value != targetSliderValue)
        {
            reputationSlider.value = Mathf.Lerp(reputationSlider.value, targetSliderValue, Time.deltaTime * updateSpeed);
        }
    }
    
    // Gérer un changement de réputation
    private void HandleReputationChanged(string faction, int oldValue, int newValue)
    {
        if (faction != factionID) return;
        
        UpdateUI(true);
    }
    
    // Gérer un changement de niveau de réputation
    private void HandleReputationLevelChanged(string faction, ReputationManager.ReputationLevel oldLevel, ReputationManager.ReputationLevel newLevel)
    {
        if (faction != factionID) return;
        
        currentLevel = newLevel;
        UpdateUI(true);
        
        // Animation spéciale pour le changement de niveau
        if (pulseOnChange)
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
                
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }
    }
    
    // Mettre à jour l'UI - RENDU PUBLIC POUR PERMETTRE L'ACCÈS EXTERNE
    public void UpdateUI(bool animate)
    {
        if (ReputationManager.instance == null) return;
        
        // Récupérer les données actuelles
        int value = ReputationManager.instance.GetReputation(factionID);
        float percentage = ReputationManager.instance.GetReputationPercentage(factionID) / 100f;
        ReputationManager.ReputationLevel level = ReputationManager.instance.GetReputationLevel(factionID);
        
        // Mettre à jour le slider
        if (reputationSlider != null)
        {
            targetSliderValue = percentage;
            
            if (!animate)
                reputationSlider.value = targetSliderValue;
        }
        
        // Mettre à jour la valeur numérique
        if (reputationValueText != null && showReputationValue)
        {
            reputationValueText.text = value.ToString();
        }
        
        // Mettre à jour le niveau textuel
        if (reputationLevelText != null && showReputationLevel)
        {
            string levelText = "";
            switch (level)
            {
                case ReputationManager.ReputationLevel.Hostile:
                    levelText = "Hostile";
                    break;
                case ReputationManager.ReputationLevel.Suspicious:
                    levelText = "Suspicieux";
                    break;
                case ReputationManager.ReputationLevel.Neutral:
                    levelText = "Neutre";
                    break;
                case ReputationManager.ReputationLevel.Friendly:
                    levelText = "Amical";
                    break;
                case ReputationManager.ReputationLevel.Allied:
                    levelText = "Allié";
                    break;
            }
            
            reputationLevelText.text = levelText;
        }
        
        // Mise à jour des couleurs
        UpdateColors(level);
    }
    
    // Méthode pour configurer la faction de cette UI
    public void SetFaction(string newFactionID)
    {
        factionID = newFactionID;
        
        // Charger les données de faction
        if (ReputationManager.instance != null)
        {
            ReputationManager.FactionData factionData = ReputationManager.instance.GetFactionData(factionID);
            if (factionData != null)
            {
                if (factionNameText != null)
                    factionNameText.text = factionData.displayName;
                    
                if (factionIcon != null && factionData.icon != null)
                    factionIcon.sprite = factionData.icon;
            }
        }
        
        // Mettre à jour l'UI avec les valeurs initiales
        UpdateUI(false);
    }
    
    // Mettre à jour les couleurs en fonction du niveau
    private void UpdateColors(ReputationManager.ReputationLevel level)
    {
        Color targetColor = neutralColor;
        
        switch (level)
        {
            case ReputationManager.ReputationLevel.Hostile:
                targetColor = hostileColor;
                break;
            case ReputationManager.ReputationLevel.Suspicious:
                targetColor = suspiciousColor;
                break;
            case ReputationManager.ReputationLevel.Neutral:
                targetColor = neutralColor;
                break;
            case ReputationManager.ReputationLevel.Friendly:
                targetColor = friendlyColor;
                break;
            case ReputationManager.ReputationLevel.Allied:
                targetColor = alliedColor;
                break;
        }
        
        // Appliquer la couleur
        if (fillImage != null)
            fillImage.color = targetColor;
            
        if (reputationLevelText != null)
            reputationLevelText.color = targetColor;
    }
    
    // Animation de pulsation lors d'un changement de niveau
    private IEnumerator PulseAnimation()
    {
        Transform target = reputationLevelText != null ? reputationLevelText.transform : transform;
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * pulseScale;
        
        float elapsed = 0f;
        
        while (elapsed < pulseDuration)
        {
            float t = elapsed / pulseDuration;
            // Animation avec rebond
            float scale = Mathf.Sin(t * Mathf.PI) * (pulseScale - 1f) + 1f;
            target.localScale = originalScale * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
}