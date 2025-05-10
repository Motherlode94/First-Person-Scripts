// Assets/Scripts/Missions/PickupDeliverable.cs
using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PickupDeliverable : MonoBehaviour
{
    [Header("Physique")]
    [Tooltip("Délai en secondes avant que l'objet puisse être ramassé à nouveau après l'avoir lâché")]
    [Range(0.1f, 2.0f)]
    public float triggerRestoreDelay = 0.2f;
    
    [Header("Hold Point")]
    public Transform holdPoint;
    
    [Header("UI")]
    public GameObject interactionPanel;
    public TMP_Text interactionText;
    
    [Header("Delivery Zone")]
    public bool isInDeliveryZone = false;
    public float dropDownForce = 0f;  // optionnel, pour un petit coup vers le bas

    // État interne
    private bool isInRange = false;
    private GameObject player = null;
    private Deliverable deliverable;
    private bool isPickedUp = false;
    private Collider objCollider;
    private Rigidbody objRigidbody;

    private void Start()
    {
        deliverable = GetComponent<Deliverable>();
        if (deliverable == null)
            Debug.LogError("[PickupDeliverable] Il manque un composant Deliverable !");

        objCollider = GetComponent<Collider>();
        objRigidbody = GetComponent<Rigidbody>();

        // On part en trigger pour détecter le joueur
        objCollider.isTrigger = true;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    private void Update()
    {
        // 1) Ramassage
        if (isInRange && !isPickedUp)
        {
            // Vérifier si le joueur interagit pour ramasser
            if (IsInteractionTriggered())
            {
                Pickup();
                SafeHidePrompt();
            }
        }
        // 2) Dépose
        else if (isPickedUp && IsDropTriggered())
        {
            Drop();
        }
        // 3) Livraison
        else if (isPickedUp && isInDeliveryZone && IsDeliverTriggered())
        {
            // Trouver la zone de livraison
            DeliveryZone[] zones = FindObjectsOfType<DeliveryZone>();
            foreach (DeliveryZone zone in zones)
            {
                // Vérifier si la zone est dans le trigger
                if (zone != null && zone.gameObject != null && zone.gameObject.CompareTag("DeliveryZone"))
                {
                    DeliverToZone(zone);
                    break;
                }
            }
        }
        
        // Mettre à jour les UI selon l'état
        UpdateInteractionPrompts();
    }
    
    // Méthode pour gérer l'affichage des prompts selon l'état
    private void UpdateInteractionPrompts()
    {
        if (isInRange && !isPickedUp)
        {
            SafeShowPrompt("Interagir pour ramasser");
        }
        else if (isPickedUp && isInDeliveryZone)
        {
            SafeShowPrompt("Interagir pour livrer");
        }
        else
        {
            SafeHidePrompt();
        }
    }
    
    // Méthodes sécurisées pour l'affichage des prompts
    private void SafeShowPrompt(string text)
    {
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.ShowPrompt(text);
        }
        else
        {
            // Utiliser l'UI locale si disponible
            SafeSetActive(interactionPanel, true);
            SafeSetText(interactionText, text);
        }
    }
    
    private void SafeHidePrompt()
    {
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.HidePrompt();
        }
        else
        {
            // Utiliser l'UI locale
            SafeSetActive(interactionPanel, false);
        }
    }
    
    // Helper methods to safely interact with Unity objects
    private void SafeSetActive(GameObject obj, bool active)
    {
        if (obj != null && obj)  // The second check verifies the object hasn't been destroyed
        {
            obj.SetActive(active);
        }
    }
    
    private void SafeSetText(TMP_Text textField, string text)
    {
        if (textField != null && textField)  // The second check verifies the object hasn't been destroyed
        {
            textField.text = text;
        }
    }
    
    // Méthodes pour vérifier les différentes interactions (corrigées)
    private bool IsInteractionTriggered()
    {
        // Utiliser KeyCode au lieu des boutons standards
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
    }
    
    private bool IsDropTriggered()
    {
        // Utiliser directement KeyCode
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q);
    }
    
    private bool IsDeliverTriggered()
    {
        // Utilisez F spécifiquement pour la livraison, mais gardez aussi les touches d'interaction
        return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return; // Safety check
        
        Debug.Log($"[PickupDeliverable] Collision détectée avec {other.name}, tag: {other.tag}");
        
        if (other.CompareTag("Player") && !isPickedUp)
        {
            isInRange = true;
            player = other.gameObject;
            Debug.Log("[PickupDeliverable] Joueur détecté, activation de l'UI");
            
            // Afficher le prompt d'interaction
            SafeShowPrompt("Interagir pour ramasser");
        }
        else if (other.CompareTag("DeliveryZone"))
        {
            isInDeliveryZone = true;
            Debug.Log("[PickupDeliverable] Zone de livraison détectée");
            
            // Si l'objet est tenu et qu'on est dans une zone de livraison
            if (isPickedUp)
            {
                SafeShowPrompt("Interagir pour livrer");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return; // Safety check
        
        if (other.CompareTag("Player") && !isPickedUp)
        {
            isInRange = false;
            player = null;
            
            // Use the safer helper method instead of ?. operator
            SafeSetActive(interactionPanel, false);
            SafeHidePrompt();
        }
        else if (other.CompareTag("DeliveryZone"))
        {
            isInDeliveryZone = false;
        }
    }

    private void Pickup()
    {
        if (deliverable == null || player == null) return;

        // Recherche du HoldPoint
        if (holdPoint == null)
        {
            holdPoint = player.transform.Find("HoldPoint");
            if (holdPoint == null)
            {
                Debug.LogWarning("[PickupDeliverable] Pas de child 'HoldPoint' trouvé sur le joueur.");
                return;
            }
        }

        // Parent & position
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // On désactive les collisions pour éviter que
        // l'objet accroche le joueur ou la scène
        objCollider.enabled = false;

        // Kinematic pour stopper toute force en cours
        objRigidbody.isKinematic = true;

        deliverable.isHeld = true;
        isPickedUp = true;
    }
    
    void OnGUI()
    {
        if (isPickedUp && deliverable != null) // Added null check
        {
            GUI.Label(new Rect(10, 70, 300, 20), $"Deliverable: {deliverable.deliverID}, InZone: {isInDeliveryZone}");
            
            if (isInDeliveryZone)
            {
                DeliveryZone[] zones = FindObjectsOfType<DeliveryZone>();
                GUI.Label(new Rect(10, 90, 300, 20), $"Zones found: {zones.Length}");
                
                foreach (DeliveryZone zone in zones)
                {
                    if (zone != null && zone.gameObject != null && zone.gameObject.CompareTag("DeliveryZone"))
                    {
                        GUI.Label(new Rect(10, 110, 300, 20), $"Valid zone: {zone.deliveryZoneID}");
                    }
                }
            }
        }
    }
    
    public void DeliverToZone(DeliveryZone zone)
    {
        if (deliverable == null || !isPickedUp || zone == null) return;
        
        Debug.Log($"[PickupDeliverable] Livraison de {deliverable.deliverID} à la zone {zone.deliveryZoneID}");
        
        // Détacher de la main du joueur
        transform.SetParent(null);
        
        // Obtenir le DeliveryPoint
        Transform targetPoint = zone.GetDeliveryPointTransform();
        
        if (targetPoint != null)
        {
            // Attacher l'objet au DeliveryPoint
            transform.SetParent(targetPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Si pas de DeliveryPoint, placer l'objet dans la zone
            transform.position = zone.GetDeliveryPoint();
        }
        
        // Désactiver les composants physiques
        objCollider.enabled = false;
        objRigidbody.isKinematic = true;
        
        // Notifier la zone que l'objet a été livré
        zone.CompleteDelivery(gameObject);
        
        // Mettre à jour les états
        deliverable.isHeld = false;
        isPickedUp = false;
        isInRange = false;
        player = null;
    }

    private void Drop()
    {
        // Détache de l'objet
        transform.SetParent(null);

        // On réactive la physique avec précaution
        objCollider.enabled = true;
        objCollider.isTrigger = false;
        objRigidbody.isKinematic = false;
        
        // Réinitialiser la vitesse pour éviter les mouvements erratiques
        objRigidbody.velocity = Vector3.zero;
        objRigidbody.angularVelocity = Vector3.zero;
        
        // S'assurer que la gravité est bien activée
        objRigidbody.useGravity = true;

        // Optionnel : petit coup vers le bas pour simuler un lâcher franc
        if (dropDownForce != 0f)
            objRigidbody.AddForce(Vector3.down * dropDownForce, ForceMode.Impulse);

        deliverable.isHeld = false;
        isPickedUp = false;

        Debug.Log("[PickupDeliverable] Objet lâché, il devrait maintenant tomber naturellement.");

        // On libère la référence au joueur
        isInRange = false;
        player = null;
        
        // Annuler toute coroutine précédente si elle existe
        StopAllCoroutines();
        
        // Remettre en mode trigger après un délai pour pouvoir le ramasser à nouveau
        StartCoroutine(ResetTriggerAfterDelay(triggerRestoreDelay));
    }
    
    private IEnumerator ResetTriggerAfterDelay(float delay)
    {
        // Attendre que l'objet soit probablement au repos
        yield return new WaitForSeconds(delay);
        
        // Vérifier que l'objet n'est pas détruit et qu'il n'est pas déjà ramassé
        if (this == null || objCollider == null || objRigidbody == null)
            yield break; // Exit if objects have been destroyed
            
        if (!isPickedUp)
        {
            // Vérifier la position et vitesse
            if (transform.position.y < -10f)
            {
                // L'objet est tombé trop bas, le repositionner
                Debug.LogWarning("[PickupDeliverable] Objet tombé trop bas, repositionnement");
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
                objRigidbody.velocity = Vector3.zero;
                objRigidbody.angularVelocity = Vector3.zero;
            }
            
            // Vérifier que l'objet est presque immobile avant de le rendre interactif
            bool isAlmostStill = objRigidbody.velocity.magnitude < 0.1f;
            
            if (isAlmostStill)
            {
                objCollider.isTrigger = true;
                Debug.Log("[PickupDeliverable] Collider remis en mode trigger après repos");
                
                // Garantir que l'objet ne tombe plus à travers le sol
                objRigidbody.isKinematic = true;
                objRigidbody.useGravity = false;
            }
            else
            {
                // S'il bouge encore, attendre un peu plus
                Debug.Log("[PickupDeliverable] Objet encore en mouvement, attente prolongée");
                StartCoroutine(ResetTriggerAfterDelay(0.3f));
            }
        }
    }
    
    private void OnDestroy()
    {
        // Make sure to clean up any references when this object is destroyed
        StopAllCoroutines();
        player = null;
        holdPoint = null;
        deliverable = null;
        interactionPanel = null;
        interactionText = null;
    }
}