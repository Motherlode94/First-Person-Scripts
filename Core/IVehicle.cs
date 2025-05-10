// Assets/Scripts/Vehicles/IVehicle.cs
using UnityEngine;

/// <summary>
/// Interface commune pour tous les véhicules du jeu
/// </summary>
public interface IVehicle
{
    /// <summary>
    /// Identifiant unique du véhicule, utile pour les missions
    /// </summary>
    string VehicleID { get; }
    
    /// <summary>
    /// Type/modèle du véhicule
    /// </summary>
    string VehicleType { get; }
    
    /// <summary>
    /// Nom visible du véhicule
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Indicateur si le véhicule est actuellement contrôlé par le joueur
    /// </summary>
    bool IsControlled { get; set; }
    
    /// <summary>
    /// Point où le joueur sera placé quand il sort du véhicule
    /// </summary>
    Transform ExitPoint { get; }
    
    /// <summary>
    /// Vitesse actuelle du véhicule en km/h
    /// </summary>
    float CurrentSpeed { get; }
    
    /// <summary>
    /// Niveau de santé/dommage du véhicule (0-100)
    /// </summary>
    float Health { get; }
    
    /// <summary>
    /// Niveau de carburant du véhicule (0-100)
    /// </summary>
    float FuelLevel { get; }
    
    /// <summary>
    /// Est-ce que le véhicule peut être utilisé
    /// </summary>
    bool IsOperational { get; }
    
    /// <summary>
    /// Appelé quand le joueur entre dans le véhicule
    /// </summary>
    void OnPlayerEnter(GameObject player);
    
    /// <summary>
    /// Appelé quand le joueur sort du véhicule
    /// </summary>
    void OnPlayerExit();
    
    /// <summary>
    /// Active/désactive les effets visuels du véhicule (phares, etc.)
    /// </summary>
    void ToggleLights();
    
    /// <summary>
    /// Déclenche un klaxon ou une sirène
    /// </summary>
    void UseHorn();
}