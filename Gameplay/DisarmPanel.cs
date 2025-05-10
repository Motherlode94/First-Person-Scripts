// DisarmPanel.cs - Version améliorée pour garder l'UI active
using UnityEngine;
using TMPro;
using System.Collections;

public class DisarmPanel : MonoBehaviour
{
    [Header("Batterie")]
    [Tooltip("ID de batterie requise pour activer le panneau")]
    public string requiredBatteryID = "";

    [Header("UI")]
    [Tooltip("Texte affiché à l'écran")]
    public TMP_Text statusText;
    [Tooltip("CanvasGroup principal pour l'opacité")]
    public CanvasGroup mainCanvasGroup;
    [Tooltip("Opacité minimale quand inactif")]
    public float minAlpha = 0.2f;

    [Tooltip("Couleur de texte: non alimenté")]
    public Color offlineColor = Color.red;
    [Tooltip("Couleur de texte: prêt")]
    public Color readyColor = Color.yellow;
    [Tooltip("Couleur de texte: désamorcé")]
    public Color disarmedColor = Color.cyan;

    [Header("Effets")]
    [SerializeField] private ParticleSystem activationEffect;
    [SerializeField] private ParticleSystem disarmedEffect;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip disarmedSound;
    [SerializeField] private AudioClip countdownSound;

    [Header("Objets")]
    [Tooltip("Objet désamorçable à activer (1ère étape)")]
    public GameObject disarmTrigger;

    [Header("Compte à rebours")]
    [Tooltip("Durée du compte à rebours en secondes")]
    public float countdownTime = 30f;
    private float countdown;
    private bool isCountingDown = false;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private bool isActivated = false;
    private bool hasDisarmObjective = false;
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Initialiser le CanvasGroup si nécessaire
        if (mainCanvasGroup == null)
        {
            mainCanvasGroup = GetComponent<CanvasGroup>();
            if (mainCanvasGroup == null)
                mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Définir l'opacité minimale au début
        mainCanvasGroup.alpha = minAlpha;
    }

    void Start()
    {
        // Forcer l'activation en mode debug ou si la mission est correcte
        #if UNITY_EDITOR
        hasDisarmObjective = true;
        #else
        hasDisarmObjective = HasDisarmObjective();
        #endif

        if (!hasDisarmObjective)
        {
            Debug.Log("[DisarmPanel] Aucune mission de désamorçage active. Réduction de l'opacité.");
            FadeToMinimum();
            return;
        }

        UpdatePanelUI();
    }

    void Update()
    {
        // Débogage de l'état de la batterie
        if (!isActivated && showDebug)
            Debug.Log($"[DisarmPanel] Statut de batterie: {requiredBatteryID} = {BatteryManager.HasBattery(requiredBatteryID)}");

        // Si la mission n'est pas active ou si le panneau est déjà activé, ne pas vérifier la batterie
        if (!hasDisarmObjective) return;
        
        // Vérification de la batterie (uniquement si pas encore activé)
        if (!isActivated && BatteryManager.HasBattery(requiredBatteryID))
        {
            Debug.Log("[DisarmPanel] Batterie détectée. Panneau activé.");
            ActivatePanel();
        }

        // Gestion du compte à rebours (peut s'exécuter même si le panneau est activé)
        if (isCountingDown && countdown > 0)
        {
            countdown -= Time.deltaTime;
            Debug.Log($"[DisarmPanel] Compte à rebours: {countdown}"); // Log pour debug
            
            if (statusText != null)
                statusText.text = $"Bombe amorcée ⚠️ ({Mathf.CeilToInt(countdown)}s)";
                
            // Jouer un son de tic-tac plus rapide à mesure que le temps s'écoule
            if (countdown < 10f && audioSource != null && countdownSound != null)
            {
                if (!audioSource.isPlaying)
                    audioSource.PlayOneShot(countdownSound, 0.5f + (1f - countdown / 10f) * 0.5f);
            }
        }
    }

    void ActivatePanel()
    {
        isActivated = true;
        countdown = countdownTime;
        isCountingDown = true;

        // Augmenter l'opacité
        FadeToFull();

        // Effets visuels et sonores
        if (activationEffect != null)
            activationEffect.Play();
            
        if (audioSource != null && activationSound != null)
            audioSource.PlayOneShot(activationSound);

        if (statusText != null)
        {
            statusText.text = $"Bombe amorcée ⚠️ ({countdownTime}s)";
            statusText.color = readyColor;
            
            // Animation de pulsation du texte
            StartCoroutine(PulseText());
        }

        // Ne pas désactiver complètement le trigger, juste ajuster son opacité
        if (disarmTrigger != null)
        {
            // Obtenir le CanvasGroup du trigger
            CanvasGroup triggerCG = disarmTrigger.GetComponent<CanvasGroup>();
            if (triggerCG == null)
                triggerCG = disarmTrigger.AddComponent<CanvasGroup>();
                
            // Commencer à pleine opacité
            triggerCG.alpha = 1f;
        }
            
        Debug.Log("[DisarmPanel] Panneau activé, compte à rebours démarré: " + countdown);
    }

    private IEnumerator PulseText()
    {
        while (isCountingDown)
        {
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.3f + 0.7f;
            if (statusText != null && statusText.fontMaterial != null)
                statusText.fontMaterial.SetFloat("_FaceDilate", pulse * 0.25f);
            yield return null;
        }
    }

    public void NotifyDisarmed()
    {
        isCountingDown = false;
        
        // Effets visuels et sonores
        if (disarmedEffect != null)
            disarmedEffect.Play();
            
        if (audioSource != null && disarmedSound != null)
            audioSource.PlayOneShot(disarmedSound);
            
        if (statusText != null)
        {
            statusText.text = "Bombe désamorcée ✔";
            statusText.color = disarmedColor;
            if (statusText.fontMaterial != null)
                statusText.fontMaterial.SetFloat("_FaceDilate", 0);
        }
        
        // Trouver la zone de désamorçage associée et la notifier
        DisarmZone disarmZone = GetComponentInParent<DisarmZone>();
        if (disarmZone != null)
        {
            disarmZone.NotifyDisarmed();
        }
        
        Debug.Log("[DisarmPanel] Bombe désamorcée avec succès!");
    }

    public void CheckBatteryStatus()
    {
        if (!hasDisarmObjective || isActivated) return;

        Debug.Log($"[DisarmPanel] Vérification batterie: {requiredBatteryID}, résultat: {BatteryManager.HasBattery(requiredBatteryID)}");
        
        if (BatteryManager.HasBattery(requiredBatteryID))
        {
            Debug.Log("[DisarmPanel] Batterie détectée. Panneau activé.");
            ActivatePanel();
        }
    }
    
    public void ForceRefresh()
    {
        // Vérifier l'état actuel et forcer la mise à jour de l'affichage
        if (isActivated && isCountingDown)
        {
            if (statusText != null)
            {
                statusText.text = $"Bombe amorcée ⚠️ ({Mathf.CeilToInt(countdown)}s)";
                statusText.color = readyColor;
            }
            
            // Faire apparaître le panneau
            FadeToFull();
        }
        else if (isActivated && !isCountingDown)
        {
            if (statusText != null)
            {
                statusText.text = "Bombe désamorcée ✔";
                statusText.color = disarmedColor;
            }
            
            // Faire apparaître le panneau
            FadeToFull();
        }
        else
        {
            UpdatePanelUI(); // Retour à l'état initial si nécessaire
        }
    }

    void UpdatePanelUI()
    {
        if (statusText != null)
        {
            statusText.text = "Batterie requise...";
            statusText.color = offlineColor;
        }

        // Ajuster l'opacité du trigger
        if (disarmTrigger != null)
        {
            CanvasGroup triggerCG = disarmTrigger.GetComponent<CanvasGroup>();
            if (triggerCG == null)
                triggerCG = disarmTrigger.AddComponent<CanvasGroup>();
                
            // Réduire l'opacité au lieu de désactiver
            triggerCG.alpha = minAlpha;
            triggerCG.interactable = false;
            triggerCG.blocksRaycasts = false;
        }
        
        // Réduire l'opacité du panneau principal
        FadeToMinimum();
    }
    
    // Méthodes pour gérer les transitions d'opacité
    private void FadeToFull()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        fadeCoroutine = StartCoroutine(FadeCanvas(mainCanvasGroup, mainCanvasGroup.alpha, 1f, 0.3f));
    }
    
    private void FadeToMinimum()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        fadeCoroutine = StartCoroutine(FadeCanvas(mainCanvasGroup, mainCanvasGroup.alpha, minAlpha, 0.3f));
    }
    
    private IEnumerator FadeCanvas(CanvasGroup cg, float startAlpha, float targetAlpha, float duration)
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

    bool HasDisarmObjective()
    {
        if (MissionManager.Instance == null || MissionManager.Instance.ActiveMission == null)
            return false;

        foreach (var o in MissionManager.Instance.ActiveMission.objectives)
            if (o.type == ObjectiveType.Disarm)
                return true;

        return false;
    }
}