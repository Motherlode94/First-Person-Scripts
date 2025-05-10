// Version modifiée de DeliveryZone.cs sans les Key Binds
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class DeliveryZone : MonoBehaviour
{
    [Header("Paramètres")]
    [Tooltip("ID unique de la zone de livraison")]
    public string deliveryZoneID = "medic_zone";
    
    [Tooltip("ID de l'objet à livrer dans cette zone")]
    public string deliverID = "package_01";
    
    [Header("Livraisons multiples")]
    [Tooltip("IDs alternatifs acceptés par cette zone (séparés par des virgules)")]
    public string alternativeDeliverIDs = "";
    
    [Header("UI")]
    [Tooltip("Texte affiché lorsqu'un objet livrable est dans la zone")]
    public TMP_Text promptText;
    
    [Tooltip("GameObject contenant l'UI d'interaction")]
    public GameObject interactionPrompt;
    
    [Header("Effets")]
    [Tooltip("Effet à jouer lors de la livraison")]
    public ParticleSystem deliveryEffect;
    
    [Tooltip("Son à jouer lors de la livraison")]
    public AudioClip deliverySound;
    
    [Header("Suppression")]
    [Tooltip("Faire disparaître l'objet après livraison")]
    public bool removeAfterDelivery = true;
    
    [Tooltip("Délai avant suppression de l'objet (secondes)")]
    public float removalDelay = 1.0f;

    [Header("Position de livraison")]
    [SerializeField] private Transform deliveryPoint;
    
    [Header("Détection")]
    [Tooltip("Tag du joueur pour meilleure détection")]
    public string playerTag = "Player";
    
    [Tooltip("Taille de la zone de détection")]
    [SerializeField] private Vector3 detectionSize = new Vector3(5, 5, 5);
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Variables d'état
    private bool isDelivered = false;
    private AudioSource audioSource;
    private GameObject pendingDeliverable = null;
    private bool playerIsInRange = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Configurer le collider pour une meilleure détection
        SetupCollider();
    }
    
    private void Start()
    {
        // Initialiser l'UI
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
    
    private void SetupCollider()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null)
        {
            // Créer un BoxCollider si aucun n'existe
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = detectionSize;
            boxCollider.center = new Vector3(0, detectionSize.y/2, 0);
        }
        else
        {
            // Configurer le collider existant
            zoneCollider.isTrigger = true;
            
            if (zoneCollider is BoxCollider)
            {
                BoxCollider boxCollider = (BoxCollider)zoneCollider;
                boxCollider.size = detectionSize;
                boxCollider.center = new Vector3(0, detectionSize.y/2, 0);
            }
        }
        
        // Tag pour la détection
        gameObject.tag = "DeliveryZone";
    }

    private void Update()
    {
        // Vérifier l'input de livraison chaque frame
        CheckDeliveryInput();
    }
    
    // Méthode pour vérifier si un ID est accepté par cette zone
    private bool IsAcceptedDeliverID(string id)
    {
        // Vérifier l'ID principal
        if (id == deliverID)
            return true;
            
        // Vérifier les IDs alternatifs
        if (!string.IsNullOrEmpty(alternativeDeliverIDs))
        {
            string[] alternativeIDs = alternativeDeliverIDs.Split(',');
            foreach (string altID in alternativeIDs)
            {
                if (id == altID.Trim())
                    return true;
            }
        }
        
        return false;
    }
    
    private void CheckDeliveryInput()
    {
        if (!playerIsInRange || pendingDeliverable == null || isDelivered)
            return;
            
        // Vérifier si l'objet est toujours valide
        Deliverable deliverable = pendingDeliverable.GetComponent<Deliverable>();
        if (deliverable == null || !deliverable.isHeld)
        {
            pendingDeliverable = null;
            SafeUpdateUIText("Zone de livraison - Apportez l'objet ici");
            return;
        }
        
        // Trouver le composant PickupDeliverable
        PickupDeliverable pickup = pendingDeliverable.GetComponent<PickupDeliverable>();
        if (pickup == null)
            pickup = pendingDeliverable.GetComponentInParent<PickupDeliverable>();
            
        if (pickup != null)
        {
            // Vérifier si un bouton d'interaction est utilisé
            // Remplacé par une interaction directe ou un système personnalisé
            if (IsInteractionTriggered())
            {
                if (debugMode)
                    Debug.Log($"[DeliveryZone] Interaction de livraison déclenchée");
                    
                pickup.DeliverToZone(this);
                pendingDeliverable = null;
            }
        }
    }
    
    // Nouvelle méthode pour déterminer si l'interaction est déclenchée
    // À personnaliser selon votre système d'input
    private bool IsInteractionTriggered()
    {
        // Exemple: utiliser le bouton d'action principale
        return Input.GetButtonDown("Fire1") || Input.GetButtonDown("Submit");
        
        // Vous pouvez remplacer cette méthode par votre propre système
        // d'interaction, par exemple en utilisant l'API Input System
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugMode)
            Debug.Log($"[DeliveryZone] OnTriggerEnter: {other.name}, Tag: {other.tag}");
            
        // Vérifier si c'est un objet qui peut être livré
        Deliverable deliverable = other.GetComponent<Deliverable>();
        if (deliverable == null)
            deliverable = other.GetComponentInParent<Deliverable>();
            
        if (deliverable != null)
        {
            // Vérifier si l'objet est celui attendu dans cette zone
            if (IsAcceptedDeliverID(deliverable.deliverID))
            {
                // Chercher le composant PickupDeliverable
                PickupDeliverable pickup = deliverable.GetComponent<PickupDeliverable>();
                if (pickup == null)
                    pickup = deliverable.GetComponentInParent<PickupDeliverable>();
                    
                if (pickup != null)
                {
                    pickup.isInDeliveryZone = true;
                    
                    // Si l'objet est tenu, montrer le message de livraison
                    if (deliverable.isHeld)
                    {
                        SafeShowUI("Interagissez pour livrer");
                    }
                }
            }
        }
        
        // Détecter le joueur 
        if (IsPlayer(other))
        {
            if (debugMode)
                Debug.Log("[DeliveryZone] Joueur détecté dans la zone");
                
            playerIsInRange = true;
            SafeShowUI("Zone de livraison - Apportez l'objet ici");
        }
    }
    
    private bool IsPlayer(Collider collider)
    {
        // Plusieurs méthodes pour détecter le joueur
        if (collider.CompareTag(playerTag))
            return true;
            
        if (collider.transform.root.CompareTag(playerTag))
            return true;
            
        if (collider.GetComponent<CharacterController>() != null)
            return true;
            
        if (collider.name.Contains("Player") || collider.transform.root.name.Contains("Player"))
            return true;
            
        return false;
    }
    
    private void CheckForDeliverable(Collider collider)
    {
        // Ne rien faire si le joueur n'est pas dans la zone
        if (!playerIsInRange)
            return;
            
        Deliverable deliverable = collider.GetComponent<Deliverable>();
        if (deliverable == null)
            deliverable = collider.GetComponentInParent<Deliverable>();
            
        if (deliverable != null)
        {
            if (debugMode)
                Debug.Log($"[DeliveryZone] Objet livrable détecté: {deliverable.deliverID}");
                
            // Vérifier si c'est l'objet attendu en utilisant la méthode IsAcceptedDeliverID
            if (IsAcceptedDeliverID(deliverable.deliverID))
            {
                PickupDeliverable pickup = deliverable.GetComponent<PickupDeliverable>();
                if (pickup == null)
                    pickup = deliverable.GetComponentInParent<PickupDeliverable>();
                
                if (pickup != null && deliverable.isHeld)
                {
                    if (debugMode)
                        Debug.Log("[DeliveryZone] Objet prêt pour livraison");
                        
                    pendingDeliverable = deliverable.gameObject;
                    SafeUpdateUIText("Interagissez pour livrer");
                }
            }
            else
            {
                SafeUpdateUIText("Cet objet n'est pas attendu ici");
            }
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Si un objet est déjà en attente de livraison, ignorer
        if (pendingDeliverable != null)
            return;
            
        // Vérifier continuellement les objets livrables
        CheckForDeliverable(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // Vérifier si c'est le joueur qui sort
        if (IsPlayer(other))
        {
            if (debugMode)
                Debug.Log("[DeliveryZone] Joueur quitte la zone");
                
            playerIsInRange = false;
            SafeHideUI();
        }
        
        // Vérifier si c'est l'objet en attente qui sort
        if (pendingDeliverable != null)
        {
            Deliverable deliverable = other.GetComponent<Deliverable>();
            if (deliverable == null)
                deliverable = other.GetComponentInParent<Deliverable>();
                
            if (deliverable != null && deliverable.gameObject == pendingDeliverable)
            {
                if (debugMode)
                    Debug.Log("[DeliveryZone] Objet en attente quitte la zone");
                    
                pendingDeliverable = null;
                
                if (playerIsInRange)
                    SafeUpdateUIText("Zone de livraison - Apportez l'objet ici");
            }
        }
    }
    
    // Méthodes sécurisées pour l'affichage des UI avec vérification du gestionnaire de prompts
    private void SafeShowUI(string message)
    {
        if (debugMode)
            Debug.Log($"[DeliveryZone] ShowUI appelée avec message: {message}");
    
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.ShowPrompt(message);
        }
        else
        {
            Debug.LogWarning("[DeliveryZone] InteractionPromptManager.Instance est null");
            
            // UI Locale (fallback)
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
                if (promptText != null)
                    promptText.text = message;
            }
        }
    }
    
    private void SafeHideUI()
    {
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.HidePrompt();
        }
        else
        {
            // UI Locale (fallback)
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }
    
    private void SafeUpdateUIText(string message)
    {
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.UpdatePromptText(message);
        }
        else
        {
            Debug.LogWarning("[DeliveryZone] InteractionPromptManager.Instance est null");
            
            // UI Locale (fallback)
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
                if (promptText != null)
                    promptText.text = message;
            }
        }
    }
    
    void OnGUI()
    {
        if (playerIsInRange)
        {
            GUI.Label(new Rect(10, 130, 300, 20), $"Zone: {deliveryZoneID}, ExpectingID: {deliverID}");
            
            if (pendingDeliverable != null)
            {
                Deliverable d = pendingDeliverable.GetComponent<Deliverable>();
                if (d != null)
                {
                    GUI.Label(new Rect(10, 150, 300, 20), $"Pending: {d.deliverID}, IsHeld: {d.isHeld}");
                }
            }
        }
    }
    
    // Méthode pour obtenir le point de livraison
    public Vector3 GetDeliveryPoint()
    {
        if (deliveryPoint != null)
            return deliveryPoint.position;
        else
            return transform.position + new Vector3(0, 0.5f, 0);
    }
    
    // Méthode pour obtenir le Transform du point de livraison
    public Transform GetDeliveryPointTransform()
    {
        return deliveryPoint;
    }

    public void CompleteDelivery(GameObject deliverableObject)
    {
        // Obtenir l'ID de l'objet réellement livré
        string deliveredObjectID = deliverID; // Par défaut
        Deliverable deliverable = deliverableObject.GetComponent<Deliverable>();
        if (deliverable != null)
        {
            deliveredObjectID = deliverable.deliverID;
        }
        
        // Jouer les effets
        if (deliveryEffect != null)
            deliveryEffect.Play();
            
        if (audioSource != null && deliverySound != null)
            audioSource.PlayOneShot(deliverySound);
            
        // Notifier le système de mission avec plus de détails de débogage
        if (MissionManager.Instance != null)
        {
            Debug.Log($"[DeliveryZone] Notification au MissionManager: ObjectiveType.Deliver={ObjectiveType.Deliver}, ID={deliveredObjectID}");
            
            // Afficher plus d'informations sur la mission active
            if (MissionManager.Instance.ActiveMission != null)
            {
                Debug.Log($"[DeliveryZone] Mission active: {MissionManager.Instance.ActiveMission.missionTitle}");
                Debug.Log($"[DeliveryZone] Nombre d'objectifs: {MissionManager.Instance.ActiveMission.objectives.Count}");
                
                // Vérifier si un objectif correspond à cet ID
                bool found = false;
                foreach (var objective in MissionManager.Instance.ActiveMission.objectives)
                {
                    Debug.Log($"[DeliveryZone] Objectif: {objective.description}, Type={objective.type}, TargetID={objective.targetID}");
                    if (objective.type == ObjectiveType.Deliver && objective.targetID == deliveredObjectID)
                    {
                        found = true;
                        Debug.Log($"[DeliveryZone] Objectif correspondant trouvé!");
                    }
                }
                
                if (!found)
                    Debug.LogWarning($"[DeliveryZone] Aucun objectif de type Deliver avec targetID={deliveredObjectID} trouvé!");
            }
            else
            {
                Debug.LogWarning("[DeliveryZone] MissionManager.Instance.ActiveMission est null!");
            }
            
            // Appeler la notification avec l'ID RÉEL de l'objet livré
            MissionManager.Instance.NotifyObjectives(ObjectiveType.Deliver, deliveredObjectID);
        }
        else
        {
            Debug.LogError("[DeliveryZone] MissionManager.Instance est null - Impossible de notifier!");
        }
        
        Debug.Log($"[DeliveryZone] Livraison complétée pour {deliveredObjectID}");
        
        // Mettre à jour l'UI
        SafeUpdateUIText("Livraison complétée !");
        
        // Supprimer l'objet si nécessaire
        if (removeAfterDelivery && deliverableObject != null)
        {
            if (removalDelay <= 0)
                Destroy(deliverableObject);
            else
                StartCoroutine(RemoveAfterDelay(deliverableObject));
        }
        
        // Masquer l'UI après un délai
        Invoke("SafeHideUI", 2.0f);
        
        // Réinitialiser l'état pour permettre de nouvelles livraisons après la première
        isDelivered = false;
    }
    
    private System.Collections.IEnumerator RemoveAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(removalDelay);
        if (obj != null)
            Destroy(obj);
    }
    
    // Visualisation pour le débogage
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        
        // Dessiner la zone de détection
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1.0f);
        }
        
        // Dessiner le point de livraison
        if (deliveryPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(deliveryPoint.position, 0.2f);
        }
    }
}