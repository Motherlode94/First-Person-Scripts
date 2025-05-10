// DisarmZone.cs - Version compatible NeoFPS
using UnityEngine;
using TMPro;
using System.Collections;

public class DisarmZone : MonoBehaviour, IInteractable
{
    [Header("Paramètres")]
    [Tooltip("ID unique de la zone à désamorcer")]
    public string disarmID = "BombA1";
    
    [Tooltip("Texte affiché lors de l'interaction")]
    [SerializeField] private string interactionText = "Appuyez sur E pour interagir avec la bombe";
    
    [Header("Keypad")]
    [Tooltip("Objet à activer lors de l'interaction (keypad)")]
    [SerializeField] private GameObject keypadObject;
    
    [Header("Batterie")]
    [Tooltip("ID de la batterie requise pour l'activation")]
    [SerializeField] private string requiredBatteryID = "battery_disarm";
    
    [Header("État")]
    [SerializeField] private bool isArmed = true;
    [SerializeField] private bool hasRequiredBattery = false;
    
    [Header("Effets de disparition")]
    [Tooltip("Faire disparaître l'objet après désamorçage")]
    [SerializeField] private bool disappearAfterDisarm = true;
    
    [Tooltip("Délai avant disparition (secondes)")]
    [SerializeField] private float disappearDelay = 1.5f;
    
    private void Update()
    {
        // Vérification continue de la batterie
        if (!hasRequiredBattery)
        {
            CheckBatteryStatus();
        }
    }
    
    // Pour l'interface IInteractable
    public string GetInteractionText()
    {
        if (!isArmed)
            return "Bombe déjà désamorcée";
            
        if (!hasRequiredBattery)
            return "Batterie requise pour activer le panneau";
            
        return interactionText;
    }
    
    public void Interact(GameObject interactor)
    {
        if (!isArmed || keypadObject == null)
            return;
            
        // Vérifier si le joueur a la batterie nécessaire
        if (!hasRequiredBattery)
        {
            CheckBatteryStatus();
            
            if (!hasRequiredBattery)
            {
                Debug.Log($"[DisarmZone] Batterie {requiredBatteryID} requise pour activer le panneau.");
                return;
            }
        }
        
        // Activer le keypad
        keypadObject.SetActive(true);
        
        // Désactiver les contrôles du joueur temporairement
        InteractionManager interactionManager = interactor.GetComponent<InteractionManager>();
        if (interactionManager != null)
            interactionManager.EnableControls(false);
        else
        {
            // Chercher sur le parent ou la scène
            interactionManager = interactor.GetComponentInParent<InteractionManager>();
            if (interactionManager != null)
                interactionManager.EnableControls(false);
            else
            {
                interactionManager = FindObjectOfType<InteractionManager>();
                if (interactionManager != null)
                    interactionManager.EnableControls(false);
            }
        }
    }
    
    public void CheckBatteryStatus()
    {
        // Mettre à jour l'état de la batterie
        if (BatteryManager.HasBattery(requiredBatteryID))
        {
            hasRequiredBattery = true;
            Debug.Log($"[DisarmZone] État de batterie mis à jour. Batterie {requiredBatteryID} détectée.");
        }
    }
    
    public void NotifyDisarmed()
    {
        isArmed = false;
        Debug.Log($"[DisarmZone] Zone désarmée: {disarmID}");
        MissionManager.Instance?.NotifyObjectives(ObjectiveType.Disarm, id: disarmID);
        
        // Faire disparaître l'objet après désamorçage (si l'option est activée)
        if (disappearAfterDisarm)
        {
            // Si on veut un effet immédiat
            if (disappearDelay <= 0)
            {
                gameObject.SetActive(false);
            }
            // Sinon, on utilise une coroutine pour attendre le délai
            else
            {
                StartCoroutine(DisappearAfterDelay());
            }
        }
    }
    
    private IEnumerator DisappearAfterDelay()
    {
        yield return new WaitForSeconds(disappearDelay);
        gameObject.SetActive(false);
    }
    
    // Pour le débogage
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}