using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Script pour gérer des coffres interactifs contenant des récompenses aléatoires (armes ou munitions)
/// </summary>
public class Chest : MonoBehaviour, IInteractable
{
    [Header("Identification")]
    [Tooltip("ID unique du coffre")]
    [SerializeField] private string chestID;
    
    [Tooltip("Texte affiché lors de l'interaction")]
    [SerializeField] private string interactionText = "Appuyez sur E pour ouvrir";
    
    [Header("Animation")]
    [Tooltip("Transform du couvercle du coffre pour l'animation")]
    [SerializeField] private Transform lid;
    
    [Tooltip("Angle d'ouverture du couvercle")]
    [SerializeField] private float openAngle = 80f;
    
    [Tooltip("Vitesse d'ouverture")]
    [SerializeField] private float openSpeed = 2f;
    
    [Header("Récompenses")]
    [Tooltip("Mode de récompense du coffre")]
    [SerializeField] private ChestRewardMode rewardMode = ChestRewardMode.Random;
    
    [Tooltip("Liste des armes possibles")]
    [SerializeField] private List<ChestReward> possibleWeapons = new List<ChestReward>();
    
    [Tooltip("Liste des munitions possibles")]
    [SerializeField] private List<ChestReward> possibleAmmo = new List<ChestReward>();
    
    [Tooltip("Récompense garantie pour ce coffre spécifique (si mode FixedReward)")]
    [SerializeField] private ChestReward fixedReward;
    
    [Tooltip("Pourcentage de chance d'obtenir une arme (vs munitions) si mode Random")]
    [Range(0, 100)]
    [SerializeField] private int weaponChance = 30;
    
    [Header("Effets")]
    [Tooltip("Effet à jouer lors de l'ouverture")]
    [SerializeField] private GameObject openEffect;
    
    [Tooltip("Son joué lors de l'ouverture")]
    [SerializeField] private AudioClip openSound;
    
    [Tooltip("Son joué lors de la collecte de la récompense")]
    [SerializeField] private AudioClip rewardSound;
    
    // État du coffre
    private bool isOpen = false;
    private bool isAnimating = false;
    private bool hasBeenOpened = false;
    private ChestReward currentReward;
    
    // Composants
    private AudioSource audioSource;
    
    // Modes de récompense possibles
    public enum ChestRewardMode
    {
        Random,     // Récompense aléatoire selon les listes et probabilités
        FixedReward // Récompense fixe prédéfinie dans l'inspecteur
    }
    
    [System.Serializable]
    public class ChestReward
    {
        public string rewardID;
        public string displayName;
        [TextArea(1, 2)]
        public string description;
        public GameObject prefab;
        public Sprite icon;
        public RewardType type;
        public int quantity = 1;
        
        public enum RewardType
        {
            Weapon,
            Ammo,
            InventoryItem
        }
    }

    private void Awake()
    {
        // Initialiser l'AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Vérifier que le coffre a un ID unique
        if (string.IsNullOrEmpty(chestID))
        {
            // Générer un ID unique basé sur sa position dans la scène si non défini
            chestID = $"chest_{transform.position.x}_{transform.position.y}_{transform.position.z}";
            Debug.LogWarning($"Chest: ID non défini. ID généré automatiquement: {chestID}");
        }
    }
    
    private void Start()
    {
        // Vérifier si ce coffre a déjà été ouvert dans une session précédente
        if (PlayerPrefs.HasKey("Chest_" + chestID))
        {
            hasBeenOpened = PlayerPrefs.GetInt("Chest_" + chestID) == 1;
            
            if (hasBeenOpened)
            {
                // Ouvrir immédiatement le coffre sans animation ni récompense
                if (lid != null)
                    lid.localRotation = Quaternion.Euler(-openAngle, 0, 0);
                    
                // Désactiver le collider pour empêcher l'interaction
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    // Implémentation de IInteractable
    public string GetInteractionText()
    {
        return isOpen ? "Appuyez sur E pour collecter" : interactionText;
    }

    public void Interact(GameObject interactor)
    {
        if (isAnimating)
            return;
            
        if (!isOpen)
        {
            // Ouvrir le coffre
            StartCoroutine(OpenChest());
        }
        else
        {
            // Collecter la récompense
            CollectReward(interactor);
        }
    }
    
    private IEnumerator OpenChest()
    {
        isAnimating = true;
        
        // Jouer le son d'ouverture
        if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound);
            
        // Démarrer l'effet d'ouverture
        if (openEffect != null)
            Instantiate(openEffect, transform.position, Quaternion.identity);
            
        // Animation d'ouverture du couvercle
        if (lid != null)
        {
            Quaternion startRot = lid.localRotation;
            Quaternion endRot = Quaternion.Euler(-openAngle, 0, 0);
            
            float elapsed = 0f;
            float duration = 1f / openSpeed;
            
            while (elapsed < duration)
            {
                lid.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            lid.localRotation = endRot;
        }
        
        // Déterminer la récompense
        DetermineReward();
        
        // Marquer comme ouvert
        isOpen = true;
        isAnimating = false;
        
        // Si c'est la première ouverture, l'enregistrer
        if (!hasBeenOpened)
        {
            hasBeenOpened = true;
            PlayerPrefs.SetInt("Chest_" + chestID, 1);
            PlayerPrefs.Save();
        }
    }
    
    private void DetermineReward()
    {
        // Selon le mode de récompense configuré
        switch (rewardMode)
        {
            case ChestRewardMode.FixedReward:
                // Récompense fixe prédéfinie
                if (fixedReward != null)
                {
                    currentReward = fixedReward;
                }
                else
                {
                    Debug.LogError($"Chest {chestID}: Mode FixedReward sélectionné mais aucune récompense fixe définie!");
                    FallbackToRandomReward(); // Utiliser la sélection aléatoire comme fallback
                }
                break;
                
            case ChestRewardMode.Random:
            default:
                // Récompense aléatoire
                FallbackToRandomReward();
                break;
        }
    }
    
    private void FallbackToRandomReward()
    {
        // Déterminer si on donne une arme ou des munitions
        bool giveWeapon = (UnityEngine.Random.Range(0, 100) < weaponChance);
        
        if (giveWeapon && possibleWeapons.Count > 0)
        {
            currentReward = possibleWeapons[UnityEngine.Random.Range(0, possibleWeapons.Count)];
        }
        else if (possibleAmmo.Count > 0)
        {
            currentReward = possibleAmmo[UnityEngine.Random.Range(0, possibleAmmo.Count)];
        }
        else
        {
            Debug.LogWarning($"Chest {chestID}: Aucune récompense disponible dans les listes!");
        }
    }
    
    private void CollectReward(GameObject player)
    {
        if (currentReward == null) return;
        
        // Jouer le son de collecte
        if (rewardSound != null && audioSource != null)
            audioSource.PlayOneShot(rewardSound);
            
        // Ajouter la récompense au joueur selon son type
        switch (currentReward.type)
        {
            case ChestReward.RewardType.Weapon:
                AddWeaponToPlayer(currentReward.rewardID);
                break;
                
            case ChestReward.RewardType.Ammo:
                AddAmmoToPlayer(currentReward.rewardID, currentReward.quantity);
                break;
                
            case ChestReward.RewardType.InventoryItem:
                AddItemToInventory(currentReward);
                break;
        }
        
        // Notifier le système de mission
        if (MissionManager.Instance != null)
            MissionManager.Instance.NotifyObjectives(ObjectiveType.Collect, id: currentReward.rewardID);
            
        // Afficher un message de récompense
        ShowRewardMessage();
        
        // Désactiver le coffre une fois la récompense collectée
        GetComponent<Collider>().enabled = false;
        
        // Marquer comme collecté
        currentReward = null;
    }
    
    private void AddWeaponToPlayer(string weaponID)
    {
        // Utiliser directement la méthode statique pour notifier l'équipement
        WeaponManager.NotifyWeaponEquipped(weaponID);
        
        Debug.Log($"Chest {chestID}: Arme {weaponID} ajoutée au joueur");
    }
    
    private void AddAmmoToPlayer(string ammoID, int quantity)
    {
        // Méthode utilisant l'inventaire
        if (InventoryManager.Instance != null)
        {
            // Trouver l'item d'inventaire correspondant aux munitions
            InventoryItem ammoItem = Resources.Load<InventoryItem>($"Items/Ammo/{ammoID}");
            
            if (ammoItem != null)
            {
                InventoryManager.Instance.AddItem(ammoItem, quantity);
                Debug.Log($"Chest {chestID}: {quantity} munitions {ammoID} ajoutées à l'inventaire");
            }
            else
            {
                Debug.LogError($"Chest {chestID}: Item de munitions {ammoID} introuvable dans Resources/Items/Ammo/");
            }
        }
    }
    
    private void AddItemToInventory(ChestReward reward)
    {
        if (InventoryManager.Instance != null)
        {
            // Trouver l'item d'inventaire
            InventoryItem item = Resources.Load<InventoryItem>($"Items/{reward.rewardID}");
            
            if (item != null)
            {
                InventoryManager.Instance.AddItem(item, reward.quantity);
                Debug.Log($"Chest {chestID}: {reward.quantity}× {reward.displayName} ajouté à l'inventaire");
            }
            else
            {
                Debug.LogError($"Chest {chestID}: Item {reward.rewardID} introuvable dans Resources/Items/");
            }
        }
    }
    
    private void ShowRewardMessage()
    {
        if (currentReward == null) return;
        
        // Si vous avez un système de notification
        if (UIManager.Instance != null)
        {
            string message = $"Obtenu: {currentReward.displayName}";
            
            if (currentReward.quantity > 1)
                message += $" ×{currentReward.quantity}";
                
            UIManager.Instance.ShowTemporaryMessage(message, 3f);
        }
    }
    
    // Pour déboguer et voir la zone d'interaction
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        if (GetComponent<Collider>() is BoxCollider boxCollider)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        
        // Afficher le chestID au-dessus du coffre
        Gizmos.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, chestID);
    }
}