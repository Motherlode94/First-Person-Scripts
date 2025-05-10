using UnityEngine;

public class BatteryPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string batteryID = "";
    [SerializeField] private GameObject visualModel;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private string interactionText = "Appuyez sur E pour ramasser la batterie";
    
    private bool isCollected = false;

    // Implémentation de IInteractable
    public string GetInteractionText()
    {
        return interactionText;
    }

    public void Interact(GameObject interactor)
    {
        if (isCollected)
            return;
            
        isCollected = true;
        
        // Register battery in manager
        BatteryManager.CollectBattery(batteryID);
        
        // Mettre à jour tous les DisarmPanel
        DisarmPanel[] panels = FindObjectsOfType<DisarmPanel>();
        foreach (var panel in panels)
        {
            panel.CheckBatteryStatus();
        }
        
        // Mettre à jour tous les DisarmZone
        DisarmZone[] zones = FindObjectsOfType<DisarmZone>();
        foreach (var zone in zones)
        {
            zone.CheckBatteryStatus();
        }
        
        // Visual feedback
        if (visualModel != null)
            visualModel.SetActive(false);
            
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
            
        // Audio feedback
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            
        Debug.Log($"[BatteryPickup] Battery {batteryID} collected! Mise à jour de {panels.Length} panels et {zones.Length} zones.");
        
        // Optionally notify mission system
        if (MissionManager.Instance != null)
            MissionManager.Instance.NotifyObjectives(ObjectiveType.Collect, id: batteryID);
            
        Destroy(gameObject, 0.2f);
    }

    // Gardez également OnTriggerEnter comme méthode alternative de ramassage
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[BatteryPickup] Trigger avec {other.name}, tag: {other.tag}");
        
        if (isCollected || !other.CompareTag("Player"))
            return;
            
        // Utiliser la méthode Interact avec l'objet qui a déclenché le trigger
        Interact(other.gameObject);
    }
}