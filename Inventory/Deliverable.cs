// Assets/Scripts/Missions/Deliverable.cs
using UnityEngine;

public class Deliverable : MonoBehaviour
{
    [Header("Paramètres")]
    [Tooltip("ID unique de l'objet livrable")]
    public string deliverID = "package_01";
    
    [Tooltip("ID de la zone de livraison ciblée pour cet objet")]
    public string targetDeliveryZoneID = "deliveryZone01";
    
    [Header("Feedback")]
    [Tooltip("Message affiché lors de la livraison à la bonne zone")]
    public string successMessage = "Colis livré avec succès!";
    
    [Tooltip("Message affiché lors de la livraison à la mauvaise zone")]
    public string wrongZoneMessage = "Ce n'est pas la bonne zone de livraison!";

    [Tooltip("L'objet est-il actuellement tenu par le joueur ?")]
    public bool isHeld = false;
}