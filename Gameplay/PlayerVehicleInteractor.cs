using UnityEngine;
using System.Collections;

/// <summary>
/// Gère l'interaction du joueur avec les véhicules dans le jeu.
/// Permet au joueur d'entrer, de sortir et d'utiliser les contrôles du véhicule.
/// </summary>
public class PlayerVehicleInteractor : MonoBehaviour
{
    [Header("Détection")]
    [Tooltip("Distance maximale à laquelle le joueur peut interagir avec un véhicule")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Tooltip("Couches à vérifier pour la détection de véhicules")]
    [SerializeField] private LayerMask vehicleLayer;
    
    [Tooltip("Origine du rayon de détection (si null, utilise la position du joueur)")]
    [SerializeField] private Transform rayOrigin;
    
    [Header("Contrôles")]
    [Tooltip("Touche pour entrer/sortir du véhicule")]
    [SerializeField] private KeyCode enterExitKey = KeyCode.E;
    
    [Tooltip("Touche pour activer/désactiver les phares")]
    [SerializeField] private KeyCode lightsKey = KeyCode.L;
    
    [Tooltip("Touche pour le klaxon/sirène")]
    [SerializeField] private KeyCode hornKey = KeyCode.H;
    
    [Header("Options")]
    [Tooltip("Délai entre les tentatives de sortie (secondes)")]
    [SerializeField] private float exitCooldown = 0.5f;
    
    [Tooltip("Distance de vérification pour les obstacles lors de la sortie")]
    [SerializeField] private float exitObstacleCheckRadius = 0.5f;
    
    [Tooltip("Désactiver automatiquement le CharacterController en entrant dans un véhicule")]
    [SerializeField] private bool autoDisableCharacterController = true;
    
    [Tooltip("Couches à vérifier pour les obstacles lors de la sortie")]
    [SerializeField] private LayerMask obstacleLayer;

    // État
    private IVehicle currentVehicle;
    public bool isInVehicle { get; private set; } = false;
    private float lastExitAttemptTime = 0f;
    private CharacterController characterController;
    private GameObject closestVehicle;
    private bool isExitBlocked = false;

    private void Awake()
    {
        // Obtenir le CharacterController si présent
        characterController = GetComponent<CharacterController>();
        
        // Si aucune origine de rayon n'est définie, utiliser la position du joueur
        if (rayOrigin == null)
        {
            // Chercher la caméra du joueur
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                rayOrigin = playerCamera.transform;
            }
            else
            {
                rayOrigin = transform;
            }
        }
    }

    private void Update()
    {
        // Vérifier l'interaction d'entrée/sortie
        if (Input.GetKeyDown(enterExitKey))
        {
            if (isInVehicle)
            {
                TryExitVehicle();
            }
            else
            {
                TryEnterVehicle();
            }
        }
        
        // Gérer les contrôles supplémentaires du véhicule
        if (isInVehicle && currentVehicle != null)
        {
            // Phares (L)
            if (Input.GetKeyDown(lightsKey))
            {
                currentVehicle.ToggleLights();
            }
            
            // Klaxon (H)
            if (Input.GetKeyDown(hornKey))
            {
                currentVehicle.UseHorn();
            }
        }
        else
        {
            // Si pas dans un véhicule, rechercher périodiquement les véhicules à proximité
            DetectNearbyVehicle();
        }
    }

    /// <summary>
    /// Détecte les véhicules à proximité et affiche un prompt d'interaction
    /// </summary>
    private void DetectNearbyVehicle()
    {
        // Cast un rayon pour trouver un véhicule
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;

        // Dessiner le rayon de détection pour debug
        Debug.DrawRay(rayOrigin.position, rayOrigin.forward * interactionDistance, Color.yellow);
        
        if (Physics.Raycast(ray, out hit, interactionDistance, vehicleLayer))
        {
            // Essayer d'obtenir le composant IVehicle
            GameObject hitVehicle = hit.collider.gameObject;
            IVehicle vehicle = hitVehicle.GetComponentInParent<IVehicle>();
            
            if (vehicle != null)
            {
                // Afficher le prompt d'interaction que le véhicule soit opérationnel ou non
                if (closestVehicle != hitVehicle)
                {
                    closestVehicle = hitVehicle;
                    ShowVehiclePrompt(vehicle);
                }
                return;
            }
        }
        
        // Si aucun véhicule trouvé, masquer le prompt
        if (closestVehicle != null)
        {
            closestVehicle = null;
            HideVehiclePrompt();
        }
    }

    /// <summary>
    /// Affiche un prompt d'interaction pour un véhicule
    /// </summary>
    private void ShowVehiclePrompt(IVehicle vehicle)
    {
        string promptMessage;
        
        if (vehicle.IsOperational)
        {
            promptMessage = $"Appuyez sur {enterExitKey} pour entrer ({vehicle.DisplayName})";
        }
        else
        {
            if (vehicle.FuelLevel <= 0)
                promptMessage = $"{vehicle.DisplayName} - Manque de carburant";
            else if (vehicle.Health <= 0)
                promptMessage = $"{vehicle.DisplayName} - Trop endommagé";
            else
                promptMessage = $"{vehicle.DisplayName} - Hors service";
        }
        
        // Essayer d'utiliser InteractionPromptManager en priorité
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.ShowPrompt(promptMessage);
        }
        // Sinon utiliser InteractionPrompt si disponible
        else if (InteractionPrompt.Instance != null)
        {
            InteractionPrompt.Instance.ShowPrompt(promptMessage);
        }
        // Si aucun système d'UI d'interaction n'est disponible, utiliser le système de NeoFPS
        else
        {
            // Chercher un gestionnaire d'interface NeoFPS
            var neoPrompt = FindObjectOfType<InteractionManager>();
            if (neoPrompt != null)
            {
                Debug.Log($"Utilisation de l'UI de NeoFPS pour afficher: {promptMessage}");
                // La méthode ShowInteractionUI sera appelée automatiquement par InteractionManager
            }
            else
            {
                Debug.LogWarning($"Aucun système d'UI d'interaction trouvé. Message: {promptMessage}");
            }
        }
    }

    /// <summary>
    /// Masque le prompt d'interaction pour les véhicules
    /// </summary>
    private void HideVehiclePrompt()
    {
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.HidePrompt();
        }
        else if (InteractionPrompt.Instance != null)
        {
            InteractionPrompt.Instance.HidePrompt();
        }
        // Pas besoin de masquer explicitement l'UI NeoFPS car le système le fait automatiquement
    }

    /// <summary>
    /// Tente de faire entrer le joueur dans un véhicule
    /// </summary>
    private void TryEnterVehicle()
    {
        // Cast un rayon pour trouver un véhicule
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionDistance, vehicleLayer))
        {
            // Essayer d'obtenir le composant IVehicle
            IVehicle vehicle = hit.collider.GetComponentInParent<IVehicle>();
            
            if (vehicle != null && vehicle.IsOperational)
            {
                EnterVehicle(vehicle);
            }
            else if (vehicle != null && !vehicle.IsOperational)
            {
                // Afficher un message si le véhicule est endommagé/inutilisable
                string message = "Ce véhicule n'est pas en état de fonctionner";
                if (vehicle.FuelLevel <= 0)
                    message = "Ce véhicule n'a plus de carburant";
                else if (vehicle.Health <= 0)
                    message = "Ce véhicule est trop endommagé";
                
                // Afficher le message d'erreur
                if (InteractionPromptManager.Instance != null)
                {
                    InteractionPromptManager.Instance.UpdatePromptText(message);
                    InteractionPromptManager.Instance.FlashPrompt();
                }
                else if (InteractionPrompt.Instance != null)
                {
                    InteractionPrompt.Instance.ShowPrompt(message);
                }
            }
        }
    }

    /// <summary>
    /// Fait entrer le joueur dans un véhicule
    /// </summary>
    private void EnterVehicle(IVehicle vehicle)
    {
        if (vehicle == null) return;
        
        // Masquer le prompt d'interaction
        HideVehiclePrompt();
        
        // Désactiver le CharacterController
        if (characterController != null && autoDisableCharacterController)
        {
            characterController.enabled = false;
        }
        
        // Mémoriser le véhicule actuel et mettre à jour l'état
        currentVehicle = vehicle;
        isInVehicle = true;
        
        // Notifier le véhicule de l'entrée du joueur
        vehicle.OnPlayerEnter(gameObject);
        
        // Notifier le système de mission si nécessaire
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.NotifyObjectives(ObjectiveType.EquipWeapon, id: vehicle.VehicleID);
        }

        // Afficher un message de succès
        Debug.Log($"Joueur entré dans {vehicle.DisplayName}");
    }

    /// <summary>
    /// Tente de faire sortir le joueur du véhicule
    /// </summary>
    private void TryExitVehicle()
    {
        // Vérifier le cooldown
        if (Time.time - lastExitAttemptTime < exitCooldown)
        {
            return;
        }
        
        lastExitAttemptTime = Time.time;
        
        if (currentVehicle == null)
        {
            isInVehicle = false;
            return;
        }
        
        // Trouver une position de sortie sûre
        Transform exitPoint = currentVehicle.ExitPoint;
        Vector3 exitPosition = exitPoint != null ? exitPoint.position : transform.position;
        
        // Vérifier si la sortie est bloquée
        isExitBlocked = Physics.CheckSphere(exitPosition, exitObstacleCheckRadius, obstacleLayer);
        
        if (isExitBlocked)
        {
            // Afficher un message si la sortie est bloquée
            string message = "Sortie bloquée ! Essayez un autre endroit.";
            if (InteractionPromptManager.Instance != null)
            {
                InteractionPromptManager.Instance.ShowPrompt(message);
                InteractionPromptManager.Instance.FlashPrompt();
            }
            else if (InteractionPrompt.Instance != null)
            {
                InteractionPrompt.Instance.ShowPrompt(message);
            }
            
            // Essayer de trouver un point de sortie alternatif
            StartCoroutine(FindAlternativeExitPoint());
            return;
        }
        
        // La sortie n'est pas bloquée, sortir du véhicule
        ExitVehicle(exitPosition);
    }

    /// <summary>
    /// Cherche un point de sortie alternatif si la sortie principale est bloquée
    /// </summary>
    private IEnumerator FindAlternativeExitPoint()
    {
        // Attendre un court instant
        yield return new WaitForSeconds(0.1f);
        
        // Essayer plusieurs directions autour du véhicule
        Vector3[] directions = new Vector3[]
        {
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };
        
        Transform vehicleTransform = (currentVehicle as MonoBehaviour)?.transform;
        if (vehicleTransform == null) yield break;
        
        foreach (Vector3 dir in directions)
        {
            Vector3 testPoint = vehicleTransform.position + vehicleTransform.TransformDirection(dir * 2f);
            testPoint.y = vehicleTransform.position.y;
            
            // Vérifier si ce point est libre
            if (!Physics.CheckSphere(testPoint, exitObstacleCheckRadius, obstacleLayer))
            {
                // Point trouvé, sortir ici
                ExitVehicle(testPoint);
                yield break;
            }
        }
        
        // Si aucun point n'est trouvé, afficher un message
        string message = "Impossible de trouver un point de sortie sûr. Déplacez le véhicule.";
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.ShowPrompt(message);
        }
        else if (InteractionPrompt.Instance != null)
        {
            InteractionPrompt.Instance.ShowPrompt(message);
        }
    }

    /// <summary>
    /// Fait sortir le joueur du véhicule
    /// </summary>
    private void ExitVehicle(Vector3 exitPosition)
    {
        if (currentVehicle == null) return;
        
        // Notifier le véhicule que le joueur est sorti
        currentVehicle.OnPlayerExit();
        
        // Mettre à jour la position du joueur
        transform.position = exitPosition;
        
        // Réactiver le CharacterController
        if (characterController != null && autoDisableCharacterController)
        {
            // Petite attente pour éviter les problèmes de collision
            StartCoroutine(EnableCharacterControllerAfterDelay());
        }
        
        // Réinitialiser l'état
        currentVehicle = null;
        isInVehicle = false;
        
        Debug.Log("Joueur sorti du véhicule");
    }

    /// <summary>
    /// Réactive le CharacterController après un court délai
    /// </summary>
    private IEnumerator EnableCharacterControllerAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    /// <summary>
    /// Méthode publique pour forcer la sortie du véhicule (appelée par d'autres systèmes)
    /// </summary>
    public void ForceExitVehicle()
    {
        if (currentVehicle == null) return;
        
        // Trouver la position de sortie la plus sûre possible
        Vector3 exitPosition;
        Transform exitPoint = currentVehicle.ExitPoint;
        
        if (exitPoint != null)
        {
            exitPosition = exitPoint.position;
        }
        else
        {
            // Utiliser la position actuelle avec un petit décalage vers le haut
            exitPosition = transform.position + Vector3.up;
        }
        
        // Sortir immédiatement, même si bloqué
        ExitVehicle(exitPosition);
    }

    /// <summary>
    /// Récupère le véhicule actuellement utilisé
    /// </summary>
    public IVehicle GetCurrentVehicle()
    {
        return currentVehicle;
    }

    /// <summary>
    /// Indique si un véhicule spécifique est utilisé
    /// </summary>
    public bool IsUsingVehicle(string vehicleID)
    {
        return isInVehicle && currentVehicle != null && currentVehicle.VehicleID == vehicleID;
    }
}