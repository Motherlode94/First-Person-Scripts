using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    #region Wave Definition
    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public int EnemyAICount;
        public float spawnInterval;
        public GameObject[] possibleEnemies;
        [SerializeField] private TMP_Text countdownText;
        [Range(0f, 1f)] public float difficultyModifier = 0f;
        [Tooltip("Délai avant le démarrage de cette vague spécifique")]
        public float delayBeforeWave = 0f;
        [Tooltip("Positions de spawn spécifiques pour cette vague (si vide, utilise les positions par défaut)")]
        public Transform[] waveSpecificSpawnPoints;
        [Tooltip("Bonus score pour compléter cette vague")]
        public int waveCompletionBonus = 100;
    }
    #endregion

    #region Inspector Fields
    [Header("Spawner Settings")]
    [SerializeField] private GameObject[] EnemyAIPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private bool useCustomWaves = false;
    [SerializeField] private Wave[] customWaves;

    [Header("Auto-Generate Waves")]
    [SerializeField] private int baseEnemiesPerWave = 3;
    [SerializeField] private int maxWaves = 5;
    [SerializeField] private float EnemyAICountMultiplier = 1.2f;
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Survival Link")] 
    [SerializeField] private string survivalZoneID = "";
    [SerializeField] private bool autoStart = false;
    [SerializeField] private float autoStartDelay = 3f;
    [SerializeField] private bool infiniteWaves = false;
    [SerializeField] private int maxEnemiesAtOnce = 15;
    
    [Header("Gameplay Options")]
    [SerializeField] private bool pauseBetweenWaves = true;
    [SerializeField] private bool spawnEnemiesRandomly = true;
    [SerializeField] private bool scaleEnemyAIStats = true;
    [SerializeField] private bool enableBossWaves = false;
    [SerializeField] private int bossWaveInterval = 5;
    [SerializeField] private GameObject[] bossPrefabs;

    [Header("UI")]
[SerializeField] private TMP_Text waveText;
[SerializeField] private GameObject waveDisplay;
[SerializeField] public TMP_Text EnemyAICountText;
[SerializeField] private GameObject waveCompleteMessage;
[SerializeField] private TMP_Text waveCompleteText;
[SerializeField] private TMP_Text countdownText; // ← AJOUTER ICI
[SerializeField] private float messageDisplayTime = 3f;
[SerializeField] private CanvasGroup waveInfoCanvasGroup;
    #endregion

    #region Events
    public delegate void WaveEvent(int waveNumber);
    public static event WaveEvent OnWaveStart;
    public static event WaveEvent OnWaveComplete;
    public static event WaveEvent OnAllWavesComplete;
    public delegate void EnemyAISpawnedEvent(GameObject EnemyAI, int waveNumber, float difficultyModifier);
    public static event EnemyAISpawnedEvent OnEnemyAISpawned;
    #endregion

    #region Private Variables
    private int currentWave = 0;
    private bool isSpawning = false;
    private bool isPaused = false;
    private int totalEnemiesSpawned = 0;
    private int totalEnemiesInWave = 0;
    private int enemiesKilled = 0;
    private int totalScore = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Wave[] generatedWaves;
    private Coroutine spawnCoroutine;
    private Coroutine waveCoroutine;
    #endregion

    #region Singleton
    public static WaveSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeSpawner();
    }
    #endregion

    #region Initialization
    private void InitializeSpawner()
    {
        if (waveDisplay != null)
            waveDisplay.SetActive(false);

        if (waveCompleteMessage != null)
            waveCompleteMessage.SetActive(false);

        ValidateParameters();
        GenerateWaves();
    }

    private void ValidateParameters()
    {
        if (EnemyAIPrefabs.Length == 0)
        {
            Debug.LogError("WaveSpawner: No EnemyAI prefabs assigned!");
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("WaveSpawner: No spawn points assigned! Creating a default spawn point.");
            GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
            spawnPoint.transform.position = transform.position + Vector3.forward * 5f;
            spawnPoint.transform.SetParent(transform);
            spawnPoints = new Transform[] { spawnPoint.transform };
        }

        if (useCustomWaves && customWaves.Length == 0)
        {
            Debug.LogWarning("WaveSpawner: Custom waves enabled but no waves defined! Switching to auto-generated waves.");
            useCustomWaves = false;
        }

        if (enableBossWaves && (bossPrefabs == null || bossPrefabs.Length == 0))
        {
            Debug.LogWarning("WaveSpawner: Boss waves enabled but no boss prefabs assigned! Disabling boss waves.");
            enableBossWaves = false;
        }
    }

    private void GenerateWaves()
    {
        if (!useCustomWaves)
        {
            generatedWaves = new Wave[maxWaves];
            for (int i = 0; i < maxWaves; i++)
            {
                bool isBossWave = enableBossWaves && ((i + 1) % bossWaveInterval == 0);
                
                Wave wave = new Wave
                {
                    waveName = isBossWave ? $"Boss Vague {i + 1}" : $"Vague {i + 1}",
                    EnemyAICount = isBossWave 
                        ? Mathf.Max(1, Mathf.RoundToInt(baseEnemiesPerWave * 0.3f))
                        : Mathf.RoundToInt(baseEnemiesPerWave * Mathf.Pow(EnemyAICountMultiplier, i)),
                    spawnInterval = isBossWave 
                        ? Mathf.Max(3f, baseSpawnInterval * 2)
                        : Mathf.Max(0.5f, baseSpawnInterval - (i * 0.2f)),
                    possibleEnemies = isBossWave ? bossPrefabs : EnemyAIPrefabs,
                    difficultyModifier = difficultyCurve.Evaluate(i / (float)(maxWaves - 1)),
                    waveCompletionBonus = Mathf.RoundToInt(100 * (1 + i * 0.5f))
                };
                
                generatedWaves[i] = wave;
            }
        }
    }
    #endregion

    #region Lifecycle
    private void Start()
    {
        if (waveDisplay != null)
            waveDisplay.SetActive(false);

        if (autoStart)
            StartCoroutine(AutoStartWaves());
    }

    private void OnEnable()
    {
        ZoneDiscoveryNotifier.OnZoneEntered += OnZoneEntered;
        EnemyAI.OnEnemyAIKilled += OnEnemyAIKilled;
    }

    private void OnDisable()
    {
        ZoneDiscoveryNotifier.OnZoneEntered -= OnZoneEntered;
        EnemyAI.OnEnemyAIKilled -= OnEnemyAIKilled;
        
        StopAllCoroutines();
    }

    private void Update()
    {
        // Nettoyer la liste des ennemis actifs (supprime les références nulles)
        if (Time.frameCount % 60 == 0) // Exécute toutes les ~60 frames pour éviter de le faire chaque frame
        {
            CleanupActiveEnemiesList();
        }
    }
    #endregion

    #region Event Handlers
    private void OnZoneEntered(string zoneID)
    {
        if (!isSpawning && zoneID == survivalZoneID)
            StartWavesManually();
    }

private void OnEnemyAIKilled(string id)
{
    enemiesKilled++;
    UpdateEnemyAICountUI();
    CleanupActiveEnemiesList();

    for (int i = activeEnemies.Count - 1; i >= 0; i--)
    {
        EnemyAI enemyAI = activeEnemies[i]?.GetComponent<EnemyAI>();
        if (enemyAI != null && enemyAI.ID == id)
        {
            activeEnemies.RemoveAt(i);
            break;
        }
    }

    if (MissionManager.Instance != null)
    {
        MissionManager.Instance.NotifyObjectives(ObjectiveType.Kill, id: id);
    }
}
    #endregion

    #region Public Methods
    public void StartWavesManually()
    {
        if (!isSpawning)
        {
            StopAllCoroutines();
            waveCoroutine = StartCoroutine(SpawnWaves());
        }
    }

    public void StopWaves()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        isSpawning = false;
        
        if (waveDisplay != null)
            waveDisplay.SetActive(false);
    }

    public void PauseWaves()
    {
        isPaused = true;
    }

    public void ResumeWaves()
    {
        isPaused = false;
    }

    public void SkipToNextWave()
    {
        if (!isSpawning) return;
        
        // Tue tous les ennemis restants
        foreach (GameObject EnemyAI in activeEnemies)
        {
            if (EnemyAI != null)
            {
                Destroy(EnemyAI);
            }
        }
        
        activeEnemies.Clear();
        enemiesKilled = totalEnemiesInWave;
    }

    public bool IsSpawning() => isSpawning;
    
    public int GetCurrentWave() => currentWave;
    
    public int GetTotalWaves() => useCustomWaves ? customWaves.Length : 
                                infiniteWaves ? -1 : maxWaves;
    
    public int GetRemainingEnemies() => totalEnemiesInWave - enemiesKilled;
    
    public int GetTotalEnemiesKilled() => enemiesKilled;
    
    public int GetTotalScore() => totalScore;
    
    public float GetWaveProgress() => totalEnemiesInWave > 0 ? (float)enemiesKilled / totalEnemiesInWave : 0f;

    public Wave GetCurrentWaveData()
    {
        Wave[] waves = useCustomWaves ? customWaves : generatedWaves;
        if (currentWave < 0 || currentWave >= waves.Length)
            return null;
        
        return waves[currentWave];
    }

    public void AddEnemyAIToWave(GameObject EnemyAIPrefab, int count = 1)
    {
        if (!isSpawning) return;
        
        Wave currentWaveData = GetCurrentWaveData();
        if (currentWaveData == null) return;
        
        totalEnemiesInWave += count;
        UpdateEnemyAICountUI();
        
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(SpawnSingleEnemyAI(EnemyAIPrefab, currentWaveData.difficultyModifier));
        }
    }
    #endregion

    #region Private Methods
    private IEnumerator AutoStartWaves()
    {
        yield return new WaitForSeconds(autoStartDelay);
        if (!isSpawning)
            StartWavesManually();
    }

    private IEnumerator SpawnWaves()
    {
        if (isSpawning) yield break;

        isSpawning = true;
        totalEnemiesSpawned = 0;
        enemiesKilled = 0;
        currentWave = 0;
        
        Wave[] wavesToUse = useCustomWaves ? customWaves : generatedWaves;

        if (waveDisplay != null)
            waveDisplay.SetActive(true);
        
        FadeIn(waveInfoCanvasGroup);

        do {
            Wave wave = GetWaveData(currentWave, wavesToUse);
            totalEnemiesInWave = wave.EnemyAICount;
            enemiesKilled = 0;

            // Attend le délai spécifique à cette vague avant de commencer
            if (wave.delayBeforeWave > 0)
                yield return new WaitForSeconds(wave.delayBeforeWave);

            UpdateWaveUI();
OnWaveStart?.Invoke(currentWave + 1);
MissionManager.Instance?.ResetKillObjectives();
UIManager.Instance?.ResetWaveTimer(timeBetweenWaves);
UIManager.Instance?.AnimateEnemyAICount();
FindObjectOfType<EnemyAIKillDisplay>()?.ResetKills(wave.EnemyAICount);

ResetWaveData(); // <-- à simplifier après


            spawnCoroutine = StartCoroutine(SpawnEnemiesInWave(wave));
            
            // Attend jusqu'à ce que tous les ennemis soient tués
            yield return new WaitUntil(() => {
                if (isPaused) return false;
                return enemiesKilled >= totalEnemiesInWave;
            });

            // Ajoute le bonus de score pour la vague terminée
            totalScore += wave.waveCompletionBonus;
            
            // Vague terminée
            OnWaveComplete?.Invoke(currentWave + 1); 
            ShowWaveCompleteMessage(wave.waveCompletionBonus);

            if (pauseBetweenWaves && currentWave < wavesToUse.Length - 1)
{
    StartCoroutine(CountdownBeforeNextWave(timeBetweenWaves));
    yield return new WaitForSeconds(timeBetweenWaves);
}


            currentWave++;

        } while (ShouldContinueSpawning(wavesToUse));

        isSpawning = false;
        
        FadeOut(waveInfoCanvasGroup);

        OnAllWavesComplete?.Invoke(currentWave);
    }

    private bool ShouldContinueSpawning(Wave[] wavesToUse)
    {
        if (infiniteWaves) return true;
        return currentWave < wavesToUse.Length;
    }

    private Wave GetWaveData(int waveIndex, Wave[] wavesToUse)
    {
        if (infiniteWaves && waveIndex >= wavesToUse.Length)
        {
            // Pour les vagues infinies, on crée une nouvelle vague basée sur la dernière
            Wave lastWave = wavesToUse[wavesToUse.Length - 1];
            
            bool isBossWave = enableBossWaves && ((waveIndex + 1) % bossWaveInterval == 0);
            
            return new Wave 
            {
                waveName = isBossWave ? $"Boss Vague {waveIndex + 1}" : $"Vague {waveIndex + 1}",
                EnemyAICount = isBossWave 
                    ? Mathf.Max(1, Mathf.RoundToInt(lastWave.EnemyAICount * 0.3f))
                    : Mathf.RoundToInt(lastWave.EnemyAICount * 1.2f),
                spawnInterval = Mathf.Max(0.3f, lastWave.spawnInterval * 0.9f), 
                possibleEnemies = isBossWave ? bossPrefabs : EnemyAIPrefabs,
                difficultyModifier = Mathf.Min(1f, lastWave.difficultyModifier + 0.05f),
                waveCompletionBonus = Mathf.RoundToInt(lastWave.waveCompletionBonus * 1.2f)
            };
        }
        
        return wavesToUse[waveIndex];
    }

    private IEnumerator SpawnEnemiesInWave(Wave wave)
    {
        for (int i = 0; i < wave.EnemyAICount; i++)
        {
            // Vérifie s'il y a déjà trop d'ennemis actifs
            while (activeEnemies.Count >= maxEnemiesAtOnce)
            {
                yield return new WaitForSeconds(0.5f);
                CleanupActiveEnemiesList();
            }
            
            // Pause entre les vagues
            while (isPaused)
            {
                yield return new WaitForSeconds(0.2f);
            }
            
            if (wave.possibleEnemies.Length == 0)
                break;
            
            // Utilise les points de spawn spécifiques à la vague si disponibles
            Transform[] currentSpawnPoints = (wave.waveSpecificSpawnPoints != null && wave.waveSpecificSpawnPoints.Length > 0) 
                ? wave.waveSpecificSpawnPoints 
                : spawnPoints;
                
            if (currentSpawnPoints.Length == 0)
                break;

            // Choisit un point de spawn aléatoire ou séquentiel
            Transform spawnPoint = spawnEnemiesRandomly 
                ? currentSpawnPoints[UnityEngine.Random.Range(0, currentSpawnPoints.Length)]
                : currentSpawnPoints[i % currentSpawnPoints.Length];

            // Choisit un ennemi aléatoire dans les ennemis possibles pour cette vague
            GameObject EnemyAIPrefab = wave.possibleEnemies[UnityEngine.Random.Range(0, wave.possibleEnemies.Length)];

            yield return StartCoroutine(SpawnSingleEnemyAI(EnemyAIPrefab, wave.difficultyModifier, spawnPoint));
            
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }
private void ResetWaveData()
{
    UIManager.Instance?.ResetWaveTimer();
}

private IEnumerator SpawnSingleEnemyAI(GameObject EnemyAIPrefab, float difficultyModifier, Transform spawnPoint = null)
{
    if (EnemyAIPrefab == null)
    {
        Debug.LogWarning("[WaveSpawner] EnemyAI prefab was null, skipping spawn.");
        yield break;
    }

    if (spawnPoint == null)
    {
        if (spawnPoints.Length == 0) yield break;
        spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
    }

    GameObject EnemyAI = Instantiate(EnemyAIPrefab, spawnPoint.position, spawnPoint.rotation);
    activeEnemies.Add(EnemyAI);
    totalEnemiesSpawned++;

    // Applique le modificateur de difficulté
    EnemyAI EnemyAIComponent = EnemyAI.GetComponent<EnemyAI>();
    if (EnemyAIComponent != null)
    {
        if (scaleEnemyAIStats)
        {
            EnemyAIComponent.SetDifficultyModifier(difficultyModifier);
        }

        if (string.IsNullOrEmpty(EnemyAIComponent.ID))
        {
            EnemyAIComponent.ID = $"EnemyAI_{totalEnemiesSpawned}_{UnityEngine.Random.Range(1000, 9999)}";
        }
    }

    UpdateEnemyAICountUI();
    OnEnemyAISpawned?.Invoke(EnemyAI, currentWave + 1, difficultyModifier);

    yield return null;
}
private void UpdateWaveUI()
{
    if (waveText == null) return;
    if (EnemyAICountText == null) return;

    string maxWaveDisplay = infiniteWaves ? "∞" : 
                            (useCustomWaves ? customWaves.Length.ToString() : maxWaves.ToString());

    waveText.text = $"Vague {currentWave + 1}/{maxWaveDisplay}";
    EnemyAICountText.text = $"Ennemis : {enemiesKilled}/{totalEnemiesInWave}";

    StartCoroutine(AnimatePop(waveText.transform));
    StartCoroutine(AnimatePop(EnemyAICountText.transform));
}
    private void UpdateEnemyAICountUI()
    {
        if (EnemyAICountText == null) return;

        EnemyAICountText.text = $"Ennemis : {enemiesKilled}/{totalEnemiesInWave}";
    }

    private void ShowWaveCompleteMessage(int bonus = 0)
{
    if (waveCompleteMessage == null) return;

    if (waveCompleteText != null)
    {
        if (bonus > 0)
            waveCompleteText.text = $"Vague {currentWave + 1} terminée !\nPréparez-vous pour la prochaine vague...";
        else
            waveCompleteText.text = $"Vague {currentWave + 1} terminée !\nPréparez-vous pour la prochaine vague...";
    }

    waveCompleteMessage.SetActive(true);
    StartCoroutine(HideMessageAfterDelay(waveCompleteMessage, messageDisplayTime));
}

private IEnumerator CountdownBeforeNextWave(float delay)
{
    if (countdownText == null) yield break;

    countdownText.gameObject.SetActive(true);

    float remainingTime = delay;

    while (remainingTime > 0f)
    {
        countdownText.text = $"Nouvelle vague dans {Mathf.CeilToInt(remainingTime)}...";
        yield return new WaitForSeconds(1f);
        remainingTime -= 1f;
    }

    countdownText.gameObject.SetActive(false);
}

    private void CleanupActiveEnemiesList()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }

    private void FadeIn(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, 0.5f));
    }

    private void FadeOut(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        
        StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, 0.5f, () => {
            canvasGroup.gameObject.SetActive(false);
        }));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration, Action onComplete = null)
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        canvasGroup.alpha = startAlpha;
        
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }

    private IEnumerator HideMessageAfterDelay(GameObject message, float delay)
    {
        yield return new WaitForSeconds(delay);
        message.SetActive(false);
    }
private IEnumerator AnimatePop(Transform t)
{
    if (t == null) yield break;

    Vector3 originalScale = t.localScale;
    Vector3 targetScale = originalScale * 1.2f;

    float duration = 0.4f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float tNorm = elapsed / duration;
        t.localScale = Vector3.LerpUnclamped(originalScale, targetScale, Mathf.Sin(tNorm * Mathf.PI));
        elapsed += Time.deltaTime;
        yield return null;
    }

    t.localScale = originalScale;
}

    #endregion
}