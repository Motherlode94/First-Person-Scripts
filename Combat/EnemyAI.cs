// EnemyAI.cs - Improved Version
using UnityEngine;
using System;
using System.Collections;

#if EMERALD_AI_2024_PRESENT
using EmeraldAI;
#endif

/// <summary>
/// Manages enemy AI behavior, difficulty scaling, and reputation interactions
/// </summary>
[DisallowMultipleComponent]
public class EnemyAI : MonoBehaviour
{
    #region Events
    /// <summary>Event triggered when an enemy is killed</summary>
    public static event Action<string> OnEnemyAIKilled;
    
    /// <summary>Event triggered when an enemy is spawned</summary>
    public static event Action<EnemyAI> OnEnemyAISpawned;
    
    /// <summary>Event triggered when enemy health changes</summary>
    public event Action<float> OnHealthChanged;
    
    /// <summary>Event triggered when enemy takes damage</summary>
    public event Action OnDamaged;
    #endregion

    #region Inspector Fields
    [Header("Identification")]
    [Tooltip("Unique identifier for this enemy")]
    [SerializeField] private string enemyID;

    [Header("Difficulty Scaling")]
    [SerializeField] private float healthMultiplier = 1.0f;
    [SerializeField] public float damageMultiplier = 1.0f;
    [SerializeField] private float speedMultiplier = 1.0f;
    [SerializeField] private int basePointValue = 10;

    [Header("Visual Indicators")]
    [SerializeField] private bool useVisualDifficultyIndicator = true;
    [SerializeField] private Color easyColor = Color.green;
    [SerializeField] private Color mediumColor = Color.yellow;
    [SerializeField] private Color hardColor = Color.red;
    [SerializeField] private GameObject difficultyVFX;

    [Header("Reputation")]
    [SerializeField] private string factionID = "Gang";
    [SerializeField] private bool reactToReputation = true;
    [SerializeField] private ReputationManager.ReputationLevel hostileThresholdLevel = ReputationManager.ReputationLevel.Suspicious;
    
    [Header("UI - Legend (optional)")]
    [SerializeField] private GameObject legendUI;
    #endregion

    #region Public Properties
    /// <summary>Gets or sets the enemy's unique identifier</summary>
    public string ID 
    { 
        get => string.IsNullOrEmpty(enemyID) ? gameObject.name : enemyID;
        set => enemyID = value;
    }
    
    /// <summary>Gets the enemy's difficulty modifier</summary>
    public float DifficultyModifier { get; private set; } = 0f;
    
    /// <summary>Gets the point value for killing this enemy</summary>
    public int PointValue { get; private set; } = 10;
    
    /// <summary>Gets whether the enemy is alive</summary>
    public bool IsAlive { get; private set; } = true;
    
    /// <summary>Gets the current health percentage (0-1)</summary>
    public float HealthPercentage
    {
        get
        {
#if EMERALD_AI_2024_PRESENT
            if (healthComp != null)
                return (float)healthComp.CurrentHealth / healthComp.StartingHealth;
#endif
            return 1.0f;
        }
    }
    #endregion

    #region Private Variables
    private static bool legendDisplayed = false;
    private bool isInitialized = false;

#if EMERALD_AI_2024_PRESENT
    private EmeraldHealth healthComp;
    private MonoBehaviour aiSystem;
    private int originalHealth;
    private float originalDamage;
    private float originalSpeed;
#endif
    #endregion

    #region Lifecycle Methods
    private void Awake()
    {
#if EMERALD_AI_2024_PRESENT
        healthComp = GetComponent<EmeraldHealth>();
        aiSystem = GetComponent<MonoBehaviour>();

        if (healthComp != null)
        {
            healthComp.OnDeath += HandleDeath;
            originalHealth = healthComp.StartingHealth;
        }
#endif

        PointValue = basePointValue;
        IsAlive = true;
        isInitialized = true;

        // Handle legend display
        if (!legendDisplayed && legendUI != null)
        {
            legendUI.SetActive(true);
            legendDisplayed = true;
            StartCoroutine(HideLegendAfterDelay(10f));
        }
    }

    private void Start()
    {
        OnEnemyAISpawned?.Invoke(this);
        
        if (DifficultyModifier > 0 && isInitialized)
        {
            ApplyDifficultyModifiers();
        }
        
        // Check initial reputation
        if (reactToReputation && ReputationManager.instance != null)
        {
            ReputationManager.ReputationLevel level = ReputationManager.instance.GetReputationLevel(factionID);
            
            // React based on reputation level
            if (level <= hostileThresholdLevel)
            {
                // Become hostile immediately
                SetHostile(true);
            }
            else
            {
                // Stay passive
                SetHostile(false);
            }
            
            // Subscribe to reputation changes
            ReputationManager.instance.OnReputationLevelChanged += HandleReputationLevelChanged;
        }
    }

    private void OnDestroy()
    {
#if EMERALD_AI_2024_PRESENT
        if (healthComp != null)
            healthComp.OnDeath -= HandleDeath;
#endif
        if (ReputationManager.instance != null)
        {
            ReputationManager.instance.OnReputationLevelChanged -= HandleReputationLevelChanged;
        }
    }
    #endregion

    #region Reputation Handling
    private void HandleReputationLevelChanged(string faction, ReputationManager.ReputationLevel oldLevel, ReputationManager.ReputationLevel newLevel)
    {
        if (!reactToReputation || faction != factionID) return;
        
        // React to reputation changes
        if (newLevel <= hostileThresholdLevel && oldLevel > hostileThresholdLevel)
        {
            // Reputation decreased below threshold -> become hostile
            SetHostile(true);
        }
        else if (newLevel > hostileThresholdLevel && oldLevel <= hostileThresholdLevel)
        {
            // Reputation increased above threshold -> become passive
            SetHostile(false);
        }
    }

    /// <summary>
    /// Sets the enemy's hostile state based on player reputation
    /// </summary>
    public void SetHostile(bool hostile)
    {
        // Modify AI behavior based on hostility
        // This implementation would depend on your specific AI system
        
        if (hostile)
        {
            Debug.Log($"[EnemyAI] {ID} became hostile toward the player (reputation)");
            // Activate attack behavior
        }
        else
        {
            Debug.Log($"[EnemyAI] {ID} became passive toward the player (reputation)");
            // Activate passive behavior
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Sets the difficulty modifier for this enemy (0-1)
    /// </summary>
    public void SetDifficultyModifier(float modifier)
    {
        DifficultyModifier = Mathf.Clamp01(modifier);
        
        Debug.Log($"[Enemy] {ID} difficulty applied: {modifier:0.00}");

        if (isInitialized)
            ApplyDifficultyModifiers();

        if (useVisualDifficultyIndicator)
            ApplyVisualDifficulty(modifier);
    }

    /// <summary>
    /// Applies damage to the enemy
    /// </summary>
    public void TakeDamage(int damage)
    {
#if EMERALD_AI_2024_PRESENT
        if (healthComp != null && IsAlive)
        {
            healthComp.CurrentHealth -= damage;
            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(HealthPercentage);
            
            if (healthComp.CurrentHealth <= 0 && IsAlive)
            {
                HandleDeath();
            }
        }
#endif
    }

    /// <summary>
    /// Resets the enemy's health to maximum
    /// </summary>
    public void ResetHealth()
    {
#if EMERALD_AI_2024_PRESENT
        if (healthComp != null)
        {
            healthComp.CurrentHealth = healthComp.StartingHealth;
            OnHealthChanged?.Invoke(HealthPercentage);
        }
#endif
    }

    /// <summary>
    /// Stuns the enemy for a duration
    /// </summary>
    public void StunEnemy(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

    /// <summary>
    /// Slows the enemy for a duration
    /// </summary>
    public void SlowEnemy(float slowFactor, float duration)
    {
        StartCoroutine(SlowCoroutine(slowFactor, duration));
    }
    #endregion

    #region Private Methods
    private void ApplyDifficultyModifiers()
    {
        if (DifficultyModifier <= 0)
            return;

#if EMERALD_AI_2024_PRESENT
        if (healthComp != null)
        {
            float healthBonus = 1f + (DifficultyModifier * healthMultiplier);
            healthComp.StartingHealth = Mathf.RoundToInt(originalHealth * healthBonus);
            healthComp.CurrentHealth = healthComp.StartingHealth;
            
            // Log information for debugging
            Debug.Log($"[Enemy] {ID} health adjusted: {originalHealth} → {healthComp.StartingHealth} (x{healthBonus:0.00})");
        }

        // Modify damage if possible through API
        try
        {
            var damageProperty = aiSystem.GetType().GetProperty("AttackDamage");
            if (damageProperty != null)
            {
                originalDamage = (float)damageProperty.GetValue(aiSystem);
                float damageBonusFactor = 1f + (DifficultyModifier * damageMultiplier);
                damageProperty.SetValue(aiSystem, originalDamage * damageBonusFactor);
                
                Debug.Log($"[Enemy] {ID} damage adjusted: {originalDamage} → {originalDamage * damageBonusFactor} (x{damageBonusFactor:0.00})");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Enemy] Unable to adjust damage for {ID}: {e.Message}");
        }

        // Modify speed if possible through API
        try
        {
            var speedProperty = aiSystem.GetType().GetProperty("WalkSpeed");
            if (speedProperty != null)
            {
                originalSpeed = (float)speedProperty.GetValue(aiSystem);
                float speedBonusFactor = 1f + (DifficultyModifier * speedMultiplier);
                speedProperty.SetValue(aiSystem, originalSpeed * speedBonusFactor);
                
                Debug.Log($"[Enemy] {ID} speed adjusted: {originalSpeed} → {originalSpeed * speedBonusFactor} (x{speedBonusFactor:0.00})");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Enemy] Unable to adjust speed for {ID}: {e.Message}");
        }
#endif

        // Adjust points based on difficulty
        PointValue = Mathf.RoundToInt(basePointValue * (1f + DifficultyModifier));
    }

    private void ApplyVisualDifficulty(float modifier)
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        if (modifier < 0.3f)
            rend.material.color = easyColor;
        else if (modifier < 0.7f)
            rend.material.color = mediumColor;
        else
            rend.material.color = hardColor;
            
        // Activate visual effect if available
        if (difficultyVFX != null)
        {
            difficultyVFX.SetActive(true);
            
            // Adjust particles or other VFX based on difficulty
            ParticleSystem ps = difficultyVFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = modifier < 0.3f ? easyColor : modifier < 0.7f ? mediumColor : hardColor;
                main.startSize = 0.5f + modifier * 0.5f;
            }
        }
    }

    private void HandleDeath()
    {
        if (!IsAlive)
            return;

        IsAlive = false;

        OnEnemyAIKilled?.Invoke(ID);

        PlayDeathEffects();

        Destroy(gameObject, 2f);
    }
    
    private void PlayDeathEffects()
    {
        // Disable collider if present
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Visual death effect
        // For example, change color, play animation, etc.
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            StartCoroutine(FadeOutRenderer(rend, 2f));
        }
    }
    
    private IEnumerator FadeOutRenderer(Renderer renderer, float duration)
    {
        float elapsed = 0;
        Material mat = renderer.material;
        Color startColor = mat.color;
        
        while (elapsed < duration)
        {
            mat.color = new Color(
                startColor.r, 
                startColor.g, 
                startColor.b, 
                Mathf.Lerp(startColor.a, 0, elapsed / duration)
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator HideLegendAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (legendUI != null)
            legendUI.SetActive(false);
    }
    
    private IEnumerator StunCoroutine(float duration)
    {
#if EMERALD_AI_2024_PRESENT
        // Save current state
        bool wasEnabled = aiSystem.enabled;
        
        // Disable AI
        aiSystem.enabled = false;
        
        // Wait for stun duration
        yield return new WaitForSeconds(duration);
        
        // Restore initial state
        aiSystem.enabled = wasEnabled;
#else
        yield return new WaitForSeconds(duration);
#endif
    }
    
    private IEnumerator SlowCoroutine(float slowFactor, float duration)
    {
#if EMERALD_AI_2024_PRESENT
        bool success = false;
        float originalSpeed = 0f;
        
        // Try to get speed
        var speedProperty = aiSystem.GetType().GetProperty("WalkSpeed");
        if (speedProperty != null)
        {
            try
            {
                originalSpeed = (float)speedProperty.GetValue(aiSystem);
                float slowedSpeed = originalSpeed * slowFactor;
                speedProperty.SetValue(aiSystem, slowedSpeed);
                success = true;
            }
            catch (Exception) { }
        }

        yield return new WaitForSeconds(duration);

        // Restore if retrieval succeeded
        if (success && speedProperty != null)
        {
            try
            {
                speedProperty.SetValue(aiSystem, originalSpeed);
            }
            catch (Exception) { }
        }
#else
        yield return new WaitForSeconds(duration);
#endif
    }
    #endregion
}