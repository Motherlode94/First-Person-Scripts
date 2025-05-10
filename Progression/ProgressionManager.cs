using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Pour TextMeshProUGUI
using UnityEngine.UI; // Pour ProgressBar
using UnityEngine.Events; // Pour UnityEvent

// Définition de la classe RewardItem qui manquait
[System.Serializable]
public class RewardItem
{
    public string id;
    public string name;
    public string description;
    public Sprite icon;
    public int value;
    public RewardType type;
    
    public enum RewardType
    {
        Currency,
        Item,
        Weapon,
        Ability,
        Skin
    }
    
    public void Grant()
    {
        // Logique pour accorder la récompense selon son type
        switch (type)
        {
            case RewardType.Currency:
                // Ajouter de la monnaie
                break;
            case RewardType.Item:
                // Ajouter l'item à l'inventaire
                break;
            case RewardType.Weapon:
                // Débloquer une arme
                break;
            case RewardType.Ability:
                // Débloquer une capacité
                break;
            case RewardType.Skin:
                // Débloquer un skin
                break;
        }
        
        // Notification ou effet
        Debug.Log($"Récompense accordée: {name}");
    }
}

public class ProgressionManager : MonoBehaviour 
{
    [System.Serializable]
    public class Milestone
    {
        public int level;
        public int requiredXP;
        public string unlockDescription;
        public UnityEvent onUnlocked;
        public bool hasBeenUnlocked;
    }
    
    [Header("Progression")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private Milestone[] milestones;
    
    [Header("Surprise Rewards")]
    [SerializeField] private float surpriseChance = 0.1f; // 10% de chance
    [SerializeField] private RewardItem[] possibleRewards;
    
    [Header("UI")]
    [SerializeField] private Slider xpBar; // Changé pour Slider, qui est standard dans Unity
    [SerializeField] private TextMeshProUGUI levelText;
    
    // Événements
    public UnityEvent<int> OnLevelUp;
    public UnityEvent<int, int> OnXpGained;
    public UnityEvent<RewardItem> OnRewardGranted;
    
    // Singleton pour accès facile
    public static ProgressionManager Instance { get; private set; }
    
    private void Awake()
    {
        // Configuration du singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Initialisation
        UpdateProgressUI();
    }
    
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        
        // Animation d'XP flottante
        if (UIManager.Instance != null)
            UIManager.Instance.ShowXPGain(amount);
        
        int oldXP = currentXP;
        currentXP += amount;
        
        // Vérifier level-up et milestones
        CheckLevelUpAndMilestones(oldXP);
        
        // Chance de récompense surprise
        TryGrantSurpriseReward();
        
        // Mise à jour UI
        UpdateProgressUI();
        
        OnXpGained?.Invoke(amount, currentXP);
    }
    
    private void TryGrantSurpriseReward()
    {
        if (Random.value < surpriseChance && possibleRewards.Length > 0)
        {
            RewardItem reward = possibleRewards[Random.Range(0, possibleRewards.Length)];
            reward.Grant();
            
            // Feedback de récompense
            if (UIManager.Instance != null)
                UIManager.Instance.ShowRewardPopup(reward);
                
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySurpriseSound();
            
            OnRewardGranted?.Invoke(reward);
        }
    }
    
    private void CheckLevelUpAndMilestones(int oldXP)
    {
        // Calculer l'XP requis pour le prochain niveau
        int requiredXPForNextLevel = GetRequiredXPForLevel(currentLevel + 1);
        
        // Vérifier si level up
        while (currentXP >= requiredXPForNextLevel)
        {
            currentLevel++;
            
            // Notifier le level up
            OnLevelUp?.Invoke(currentLevel);
            
            // Feedback visuel et sonore
            if (UIManager.Instance != null)
                UIManager.Instance.ShowLevelUpEffect(currentLevel);
                
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayLevelUpSound();
            
            // Vérifier les milestones débloqués
            CheckMilestones();
            
            // Recalculer pour le prochain niveau
            requiredXPForNextLevel = GetRequiredXPForLevel(currentLevel + 1);
        }
    }
    
    private void CheckMilestones()
    {
        foreach (var milestone in milestones)
        {
            if (!milestone.hasBeenUnlocked && currentLevel >= milestone.level)
            {
                // Débloquer le milestone
                milestone.hasBeenUnlocked = true;
                milestone.onUnlocked?.Invoke();
                
                // Notification
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowMilestoneUnlocked(milestone.unlockDescription);
                    
                Debug.Log($"Milestone débloqué: {milestone.unlockDescription}");
            }
        }
    }
    
    private int GetRequiredXPForLevel(int level)
    {
        // Formule simple: chaque niveau demande 100 * niveau XP
        return 100 * level;
    }
    
    private void UpdateProgressUI()
    {
        // Mettre à jour la barre d'XP
        if (xpBar != null)
        {
            int requiredXPForCurrentLevel = GetRequiredXPForLevel(currentLevel);
            int requiredXPForNextLevel = GetRequiredXPForLevel(currentLevel + 1);
            
            float progress = (float)(currentXP - requiredXPForCurrentLevel) / 
                            (requiredXPForNextLevel - requiredXPForCurrentLevel);
            
            xpBar.value = progress;
        }
        
        // Mettre à jour le texte de niveau
        if (levelText != null)
        {
            levelText.text = $"Niveau {currentLevel}";
        }
    }
    
    // Méthodes publiques pour interagir avec le système de progression
    
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    public int GetCurrentXP()
    {
        return currentXP;
    }
    
    public float GetLevelProgress()
    {
        int requiredXPForCurrentLevel = GetRequiredXPForLevel(currentLevel);
        int requiredXPForNextLevel = GetRequiredXPForLevel(currentLevel + 1);
        
        return (float)(currentXP - requiredXPForCurrentLevel) / 
               (requiredXPForNextLevel - requiredXPForCurrentLevel);
    }
}