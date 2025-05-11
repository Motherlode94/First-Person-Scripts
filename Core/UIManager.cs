using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public static UIManager Instance => instance; // Ajout compatibilité MissionManager
    
    [Header("Kill Popup")]
    [SerializeField] private GameObject killPopupPrefab;
    [SerializeField] private Canvas killPopupCanvas; // optional separate canvas

    [Header("ObjectiveEntry Template")]
    [Tooltip("Le child 'ObjectiveEntry' sous UIManager, désactivé au départ")]
    [SerializeField] private RectTransform entryTemplate;

    [Tooltip("Le parent sous lequel on instancie tous les ObjectiveEntry clones")]
    [SerializeField] private Transform entriesParent;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;  
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private float resultDisplayDuration = 2f;
    public float ResultDisplayDuration => resultDisplayDuration;
    [SerializeField] private GameObject continueWavesMessage;

    [Header("Player Stats (toujours visibles)")]
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text levelText;
    
    [Header("Mission Panel")] 
    [SerializeField] private RectTransform missionPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text listText;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    private Coroutine fadeCoroutine;

    [Header("Timed Missions")]
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color urgentColor = Color.red;
    [SerializeField] private float flashSpeed = 4f;

    private float countdownRemaining = 0f;
    private bool countdownActive = false;
    private bool flashing = false;
    private float flashTimer = 0f;
    private Coroutine xpFlashCoroutine;
    private bool isInitialized = false;
    [Header("Affichage Séquentiel")]
[SerializeField] private bool useSequentialDisplay = true;  // Activer/désactiver l'affichage séquentiel
[SerializeField] private int maxVisibleObjectives = 1;      // Nombre max d'objectifs visibles en même temps
[SerializeField] private float objectiveHeight = 60f;       // Hauteur de chaque entrée
[SerializeField] private float objectiveSpacing = 5f;       // Espacement vertical
private string currentSwitchID = null;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        // Vérification des références essentielles
        if (entryTemplate == null)
        {
            Debug.LogError("UIManager: entryTemplate n'est pas assigné dans l'inspecteur!");
            return;
        }

        entryTemplate.gameObject.SetActive(false);
        
        if (entriesParent == null)
        {
            Debug.LogWarning("UIManager: entriesParent n'est pas assigné, utilisation du parent de entryTemplate");
            entriesParent = entryTemplate.parent;
            
            if (entriesParent == null)
            {
                Debug.LogError("UIManager: Impossible de trouver un parent pour les entrées d'objectifs!");
                return;
            }
        }
        
        if (missionPanel != null)
            missionPanel.gameObject.SetActive(false);
        else
            Debug.LogError("UIManager: missionPanel n'est pas assigné dans l'inspecteur!");

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;
        else
            Debug.LogWarning("UIManager: panelCanvasGroup n'est pas assigné, les animations de fade ne fonctionneront pas");

        if (resultPanel != null)
            resultPanel.SetActive(false);
        else
            Debug.LogWarning("UIManager: resultPanel n'est pas assigné, les résultats de mission ne s'afficheront pas");

        if (timerPanel != null)
            timerPanel.SetActive(false);
        else
            Debug.LogWarning("UIManager: timerPanel n'est pas assigné, les missions chronométrées n'afficheront pas de timer");
            
        isInitialized = true;
        Debug.Log("UIManager initialisé avec succès");
    }
    
    private void Start()
    {
        // S'abonner aux événements du MissionManager si disponible
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionActivated += OnMissionActivated;
            Debug.Log("UIManager: Abonnement aux événements du MissionManager");
        }
        else
        {
            Debug.LogWarning("UIManager: MissionManager.Instance est null au démarrage!");
            // Tenter de trouver le MissionManager dans la scène
            MissionManager manager = FindObjectOfType<MissionManager>();
            if (manager != null)
            {
                Debug.Log("UIManager: MissionManager trouvé dans la scène");
                manager.OnMissionActivated += OnMissionActivated;
            }
        }
    }
    
    private void OnMissionActivated(Mission mission)
    {
        Debug.Log($"UIManager: Mission activée - {mission.missionTitle}");
        RefreshMissionUI(mission);
    }
// Correction pour UIManager.cs - Partie responsable de l'affichage des missions
// Remplacer la méthode RefreshMissionUI dans la classe UIManager

public void RefreshMissionUI(Mission mission)
{
    if (!isInitialized)
    {
        Debug.LogError("UIManager: Tentative de rafraîchir l'UI avant l'initialisation!");
        return;
    }
    
    // Nettoyer les entrées existantes
    var toDestroy = new List<Transform>();
    foreach (Transform child in entriesParent)
    {
        if (child != entryTemplate.transform)
            toDestroy.Add(child);
    }
    
    foreach (var c in toDestroy)
    {
        Destroy(c.gameObject);
    }

    // Si pas de mission active, nettoyer l'UI et sortir
    if (mission == null)
    {
        SetTimerVisible(false);
        
        if (titleText != null) titleText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (listText != null) listText.text = "";
        
        if (missionPanel != null)
        {
            missionPanel.gameObject.SetActive(true);
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = 0.5f; // Visible mais en semi-transparence quand inactif
        }
            
        Debug.Log("UIManager: UI nettoyée (aucune mission active)");
        return;
    }

    // IMPORTANT: Activer explicitement le panneau de mission
    if (missionPanel != null)
    {
        missionPanel.gameObject.SetActive(true);
        Debug.Log("UIManager: Panneau de mission activé");
    }
    
    // Configuration du panneau de mission - TOUJOURS afficher le titre et la description
    if (titleText != null) 
    {
        titleText.text = mission.missionTitle;
        Debug.Log($"UIManager: Titre défini: {mission.missionTitle}");
    }
    
    if (descriptionText != null) 
    {
        descriptionText.text = mission.missionDescription;
        Debug.Log($"UIManager: Description définie: {mission.missionDescription}");
    }
    
    if (listText != null) 
    {
        listText.text = "";
    }

    // Définir tous les objectifs comme potentiellement pertinents au départ
    List<Objective> objectivesToShow = new List<Objective>();
    
    // Si la mission a des objectifs, essayer de les afficher
    if (mission.objectives != null && mission.objectives.Count > 0)
    {
        // Récupérer le SequenceManager
        SequenceManager seqMgr = FindObjectOfType<SequenceManager>();
        string currentSwitchID = seqMgr?.GetCurrentSwitchID();
        
        Debug.Log($"UIManager: SwitchID actuel: {currentSwitchID ?? "aucun"}");
        
        // Pour affichage séquentiel amélioré
        if (mission.revealObjectivesProgressively && currentSwitchID != null)
        {
            // Priorité 1: Objectifs complétés
            foreach (var obj in mission.objectives)
            {
                if (obj.IsCompleted)
                {
                    objectivesToShow.Add(obj);
                }
            }
            
            // Priorité 2: Objectif actuel (correspondant au switchID)
            Objective currentObjective = mission.objectives.Find(o => o.targetID == currentSwitchID && !o.IsCompleted);
            if (currentObjective != null)
            {
                // S'assurer que l'objectif actuel est visible
                currentObjective.visible = true;
                
                // Ne l'ajouter que s'il n'est pas déjà là
                if (!objectivesToShow.Contains(currentObjective))
                {
                    objectivesToShow.Add(currentObjective);
                }
            }
            
            // Si aucun objectif n'est sélectionné, prendre le premier objectif visible
            if (objectivesToShow.Count == 0)
            {
                Objective firstVisible = mission.objectives.Find(o => o.visible);
                if (firstVisible != null)
                {
                    objectivesToShow.Add(firstVisible);
                }
                else if (mission.objectives.Count > 0)
                {
                    // Si vraiment aucun objectif n'est visible, rendre le premier visible
                    mission.objectives[0].visible = true;
                    objectivesToShow.Add(mission.objectives[0]);
                }
            }
        }
        else
        {
            // Mode normal: afficher tous les objectifs visibles
            foreach (var obj in mission.objectives)
            {
                if (obj.visible)
                {
                    objectivesToShow.Add(obj);
                }
            }
            
            // Si aucun objectif visible, rendre le premier objectif visible
            if (objectivesToShow.Count == 0 && mission.objectives.Count > 0)
            {
                mission.objectives[0].visible = true;
                objectivesToShow.Add(mission.objectives[0]);
                Debug.Log("UIManager: Aucun objectif visible, affichage du premier objectif");
            }
        }
        
        // Afficher les objectifs sélectionnés
        int index = 0;
        foreach (var objective in objectivesToShow)
        {
            try
            {
                // Créer un clone du template
                GameObject clone = Instantiate(entryTemplate.gameObject, entriesParent);
                clone.name = $"ObjectiveEntry_{objective.targetID}";
                clone.SetActive(true);
                
                Debug.Log($"UIManager: Clone créé pour l'objectif: {objective.description}");
                
                // Configurer le texte - UTILISER LA STRUCTURE EXACTE DU PREFAB
                Transform titleTransform = clone.transform.Find("Title");
                Transform descTransform = clone.transform.Find("Desc");
                
                TMP_Text title = titleTransform?.GetComponent<TMP_Text>();
                TMP_Text desc = descTransform?.GetComponent<TMP_Text>();
                
                // Vérifier si le prefab est correctement configuré
                if (title == null && titleTransform != null)
                {
                    Debug.LogError("UIManager: Composant TMP_Text manquant sur 'Title'");
                }
                
                if (desc == null && descTransform != null)
                {
                    Debug.LogError("UIManager: Composant TMP_Text manquant sur 'Desc'");
                }
                
                // Configurer les textes selon l'état
                bool isCurrentObjective = (objective.targetID == currentSwitchID);
                
                // Mettre à jour les composants texte
                if (title != null)
                {
                    title.text = objective.description;
                    if (objective.IsCompleted)
                        title.color = Color.green;
                    else if (isCurrentObjective)
                        title.color = Color.yellow;
                    else
                        title.color = Color.white;
                    
                    // Amélioration: configurer le texte avec de meilleurs paramètres
                    title.enableWordWrapping = true;
                    title.overflowMode = TextOverflowModes.Ellipsis;
                    title.alignment = TextAlignmentOptions.Left;
                }
                
                if (desc != null)
                {
                    if (objective.IsCompleted)
                    {
                        desc.text = "✓";
                    }
                    else if (objective.targetCount > 1)
                    {
                        desc.text = $"{objective.currentCount}/{objective.targetCount}";
                    }
                    else
                    {
                        desc.text = "0/1";
                    }
                    
                    desc.alignment = TextAlignmentOptions.Center;
                    desc.fontStyle = FontStyles.Bold;
                }
                
                // Amélioration: configurer le RectTransform avec un positionnement vertical précis
                RectTransform rectTransform = clone.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float entryHeight = objectiveHeight; // Hauteur standard pour chaque entrée
                    float spacing = objectiveSpacing;    // Espacement entre les entrées
                    
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    
                    // Positionnement vertical en fonction de l'index
                    float yPosition = -index * (entryHeight + spacing);
                    rectTransform.anchoredPosition = new Vector2(0, yPosition);
                    rectTransform.sizeDelta = new Vector2(0, entryHeight);
                    
                    // Appliquer la configuration aux éléments enfants
                    ConfigureObjectiveLayout(clone);
                }
                
                // Incrémenter l'index pour le prochain objectif
                index++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UIManager: Erreur lors de la création du clone d'objectif: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // Mettre à jour la liste récapitulative si nécessaire
        if (listText != null && objectivesToShow.Count > 0)
        {
            listText.text = ""; // Réinitialiser le texte
            
            foreach (var obj in objectivesToShow)
            {
                string color = obj.IsCompleted ? "green" : 
                              (obj.targetID == currentSwitchID ? "yellow" : "white");
                
                listText.text += $"• <color={color}>{obj.description}</color>";
                
                if (obj.targetCount > 1)
                {
                    listText.text += $" ({obj.currentCount}/{obj.targetCount})";
                }
                else if (obj.IsCompleted)
                {
                    listText.text += " (✓)";
                }
                else
                {
                    listText.text += " (0/1)";
                }
                
                listText.text += "\n";
            }
        }
    }
    else
    {
        Debug.LogWarning("UIManager: La mission n'a pas d'objectifs!");
    }

    // Configurer le timer si nécessaire
    if (mission.IsTimed)
    {
        countdownRemaining = mission.timeLimitSeconds;
        countdownActive = true;
        SetTimerVisible(true);
        if (timerText != null) timerText.color = normalColor;
    }
    
    // TOUJOURS s'assurer que le panneau de mission est visible
    if (panelCanvasGroup != null)
    {
        panelCanvasGroup.alpha = 1f;
    }
        
    Debug.Log($"UIManager: RefreshMissionUI terminé - {objectivesToShow.Count} objectifs affichés");
}

// Ajouter cette méthode pour configurer la mise en page des objectifs
private void ConfigureObjectiveLayout(GameObject objectiveEntry)
{
    Transform titleTransform = objectiveEntry.transform.Find("Title");
    Transform descTransform = objectiveEntry.transform.Find("Desc");
    
    if (titleTransform != null)
    {
        RectTransform titleRect = titleTransform.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.8f, 1);
            titleRect.offsetMin = new Vector2(10, 5);
            titleRect.offsetMax = new Vector2(-5, -5);
        }
        
        TMP_Text titleText = titleTransform.GetComponent<TMP_Text>();
        if (titleText != null)
        {
            titleText.enableWordWrapping = true;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.alignment = TextAlignmentOptions.Left;
        }
    }
    
    if (descTransform != null)
    {
        RectTransform descRect = descTransform.GetComponent<RectTransform>();
        if (descRect != null)
        {
            descRect.anchorMin = new Vector2(0.8f, 0);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.offsetMin = new Vector2(5, 5);
            descRect.offsetMax = new Vector2(-10, -5);
        }
        
        TMP_Text descText = descTransform.GetComponent<TMP_Text>();
        if (descText != null)
        {
            descText.alignment = TextAlignmentOptions.Center;
            descText.fontStyle = FontStyles.Bold;
        }
    }
}

public void ShowXPGain(int amount)
{
    Debug.Log($"XP Gained: {amount}");
    UpdateXP(amount); // Use your existing method if appropriate
    FlashXP(); // Use your existing method
    
    // Create a floating XP text if needed
    // Similar to your existing ShowKillPopup method
}

public void ShowRewardPopup(RewardItem reward)
{
    if (resultPanel != null && resultText != null)
    {
        resultText.text = $"Récompense: {reward.name}\n{reward.description}";
        resultPanel.SetActive(true);
        StartCoroutine(AnimatePop(resultText.transform));
        StartCoroutine(AutoHideResult());
    }
}

public void ShowLevelUpEffect(int level)
{
    UpdateLevel(level); // Use your existing method
    
    if (resultPanel != null && resultText != null)
    {
        resultText.text = $"Niveau {level} atteint!";
        resultPanel.SetActive(true);
        StartCoroutine(AnimatePop(resultText.transform));
        StartCoroutine(AutoHideResult());
    }
}

public void ShowMilestoneUnlocked(string description)
{
    if (resultPanel != null && resultText != null)
    {
        resultText.text = $"Objectif débloqué:\n{description}";
        resultPanel.SetActive(true);
        StartCoroutine(AnimatePop(resultText.transform));
        StartCoroutine(AutoHideResult());
    }
}

// Ajouter cette méthode pour mettre à jour le switchID actuel
public void UpdateCurrentSwitchID(string switchID)
{
    currentSwitchID = switchID;
    
    // Mettre à jour l'UI immédiatement
    if (MissionManager.Instance?.ActiveMission != null)
    {
        RefreshMissionUI(MissionManager.Instance.ActiveMission);
    }
}

// Ajouter une méthode pour afficher des messages temporaires
public void ShowTemporaryMessage(string message, float duration = 2f)
{
    if (resultPanel != null && resultText != null)
    {
        // Utiliser le panneau de résultat pour afficher des messages temporaires
        resultText.text = message;
        resultPanel.SetActive(true);
        
        // Animation
        StartCoroutine(AnimatePop(resultText.transform));
        
        // Auto-masquage après délai
        StartCoroutine(HideMessageAfterDelay(resultPanel, duration));
    }
}
    void Update()
    {
        if (!countdownActive) return;

        countdownRemaining -= Time.deltaTime;
        if (countdownRemaining <= 0f)
        {
            countdownRemaining = 0f;
            countdownActive = false;
        }

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(countdownRemaining).ToString() + "s";

            if (countdownRemaining <= 10f)
            {
                flashing = true;
                flashTimer += Time.deltaTime * flashSpeed;
                float t = Mathf.PingPong(flashTimer, 1f);
                timerText.color = Color.Lerp(normalColor, urgentColor, t);
            }
            else
            {
                flashing = false;
                flashTimer = 0f;
                timerText.color = normalColor;
            }
        }
    }
    
    public void UpdateMissionPanel(List<Mission> missions)
    {
        if (missions == null || missions.Count == 0)
        {
            HideMissionPanel();
            return;
        }

        Mission currentMission = missions[0];

        if (titleText != null) titleText.text = currentMission.missionTitle;
        if (descriptionText != null) descriptionText.text = currentMission.missionDescription;

        if (listText != null)
        {
            if (missions.Count > 1)
            {
                listText.text = "Autres missions:\n";
                for (int i = 1; i < missions.Count; i++)
                {
                    listText.text += $"- {missions[i].missionTitle}\n";
                }
            }
            else
            {
                listText.text = "";
            }
        }

        ShowMissionPanel();
    }
    
    private void ShowMissionPanel()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (missionPanel != null)
            missionPanel.gameObject.SetActive(true);
            
        if (panelCanvasGroup != null)
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(panelCanvasGroup, 0f, 1f, 0.4f));
    }

    private void HideMissionPanel()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutAndHide());
    }

    private IEnumerator FadeOutAndHide()
    {
        yield return FadeCanvasGroup(panelCanvasGroup, 1f, 0f, 0.4f);
        
        //if (missionPanel != null)
        //    missionPanel.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = to;
    }

private void SetTimerVisible(bool visible)
{
    if (timerPanel != null)
    {
        // Au lieu de désactiver, réduire l'opacité
        CanvasGroup cg = timerPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = timerPanel.AddComponent<CanvasGroup>();
        
        // Commencer la transition
        StartCoroutine(FadeCanvasGroup(cg, visible ? 0f : 1f, visible ? 1f : 0.2f, 0.3f));
        
        // Garder l'objet actif
        timerPanel.SetActive(true);
    }

    flashing = false;
    flashTimer = 0f;
    
    if (timerText != null)
        timerText.color = normalColor;
}

    public void ClearMissionUI()
    {
        RefreshMissionUI(null);
    }

    public void ShowMissionResult(bool success)
    {
        StopAllCoroutines(); // Arrêter toutes les coroutines en cours
        
        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("UIManager: resultPanel ou resultText est null lors de l'affichage du résultat!");
            return;
        }

        resultText.text = success ? "Mission réussie !" : "Mission échouée…";
        resultPanel.SetActive(true);
        StartCoroutine(AnimatePop(resultText.transform));

        StartCoroutine(AutoHideResult());
    }
    
public void AnimateEnemyAICount()
{
    if (WaveSpawner.Instance?.EnemyAICountText != null)
        StartCoroutine(AnimatePop(WaveSpawner.Instance.EnemyAICountText.transform));
}
    
public void HideMissionResult()
{
    if (resultPanel == null)
    {
        Debug.LogWarning("UIManager: resultPanel est null lors de la tentative de masquage!");
        return;
    }
    
    StopCoroutine(nameof(AutoHideResult));
    
    // Au lieu de désactiver, réduire l'opacité
    CanvasGroup cg = resultPanel.GetComponent<CanvasGroup>();
    if (cg == null) cg = resultPanel.AddComponent<CanvasGroup>();
    
    // Commencer la transition
    StartCoroutine(FadeCanvasGroup(cg, 1f, 0.1f, 0.3f));
}

    private IEnumerator AutoHideResult()
    {
        yield return new WaitForSeconds(resultDisplayDuration);
        
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void UpdateXP(int xp)
    {
        if (xpText == null)
        {
            Debug.LogWarning("UIManager: xpText est null lors de la mise à jour XP!");
            return;
        }
        
        xpText.text = $"XP : {xp}";
        StartCoroutine(AnimatePop(xpText.transform));
    }

    public void UpdateLevel(int level)
    {
        if (levelText == null)
        {
            Debug.LogWarning("UIManager: levelText est null lors de la mise à jour du niveau!");
            return;
        }
        
        levelText.text = $"Niveau : {level}";
        StartCoroutine(AnimatePop(levelText.transform));
    }

    public void ShowContinueWavesMessage()
    {
        if (continueWavesMessage != null)
        {
            continueWavesMessage.SetActive(true);
            StartCoroutine(HideMessageAfterDelay(continueWavesMessage, 3f));
        }
    }

    public void UpdateMissionTimer(float remainingTime)
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(remainingTime).ToString() + "s";
    }
    
    public void ResetWaveTimer(float duration = 0f)
    {
        countdownRemaining = duration;
        countdownActive = duration > 0f;
        SetTimerVisible(countdownActive);
    }
    
    public void ShowRestartingMessage(int currentAttempt, int maxAttempts)
    {
        if (resultPanel != null && resultText != null)
        {
            resultText.text = $"Nouvelle tentative ({currentAttempt}/{maxAttempts})...";
            resultPanel.SetActive(true);
        }
    }

private IEnumerator HideMessageAfterDelay(GameObject message, float delay)
{
    if (message == null) yield break;
    
    yield return new WaitForSeconds(delay);
    
    if (message != null)
    {
        // Au lieu de désactiver, réduire l'opacité
        CanvasGroup cg = message.GetComponent<CanvasGroup>();
        if (cg == null) cg = message.AddComponent<CanvasGroup>();
        
        // Commencer la transition
        StartCoroutine(FadeCanvasGroup(cg, 1f, 0.1f, 0.3f));
    }
}
    
    public void FlashXP()
    {
        if (xpText == null) return;

        if (xpFlashCoroutine != null)
            StopCoroutine(xpFlashCoroutine);

        xpFlashCoroutine = StartCoroutine(FlashText(xpText, Color.white, 0.3f));
    }

    private IEnumerator FlashText(TMP_Text textComponent, Color flashColor, float duration)
    {
        if (textComponent == null) yield break;
        
        Color originalColor = textComponent.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            textComponent.color = Color.Lerp(originalColor, flashColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (textComponent != null)
            textComponent.color = originalColor;
    }
    
    public void ShowKillPopup()
    {
        if (killPopupPrefab == null || killPopupCanvas == null)
        {
            Debug.LogWarning("[UIManager] KillPopupPrefab ou killPopupCanvas non assigné!");
            return;
        }

        Vector2 basePosition = new Vector2(Screen.width / 2f, Screen.height * 0.8f);
        Vector2 randomOffset = new Vector2(Random.Range(-50f, 50f), Random.Range(-20f, 20f));
        Vector2 finalPosition = basePosition + randomOffset;

        Vector2 spawnPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            killPopupCanvas.transform as RectTransform, finalPosition, killPopupCanvas.worldCamera, out spawnPosition);

        GameObject popup = Instantiate(killPopupPrefab, killPopupCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        
        if (popupRect != null)
            popupRect.anchoredPosition = spawnPosition;

        TMP_Text popupText = popup.GetComponent<TMP_Text>();
        if (popupText != null)
            popupText.text = "+1 Kill";

        StartCoroutine(AnimateKillPopup(popup));
    }

    private IEnumerator AnimateKillPopup(GameObject popup)
    {
        if (popup == null) yield break;
        
        RectTransform rect = popup.GetComponent<RectTransform>();
        TMP_Text text = popup.GetComponent<TMP_Text>();

        if (rect == null || text == null) 
        {
            Destroy(popup);
            yield break;
        }

        float duration = 0.8f;
        float elapsed = 0f;

        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, 20f);

        Color startColor = text.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(popup);
    }

    private IEnumerator AnimatePop(Transform t)
    {
        if (t == null) yield break;
        
        Vector3 orig = t.localScale;
        t.localScale = Vector3.zero;
        float dur = 0.4f, elapsed = 0f;
        
        while (elapsed < dur)
        {
            float p = elapsed / dur;
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, orig, Mathf.SmoothStep(0f, 1.2f, p));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (t != null)
            t.localScale = orig;
    }
    
    private void OnDestroy()
    {
        // Se désabonner des événements du MissionManager
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionActivated -= OnMissionActivated;
        }
    }
}