using UnityEngine;
using UnityEngine.UI;
using TMPro;  // ← Assurez-vous d'avoir ce namespace pour TextMeshPro

public class VehicleHUD : MonoBehaviour
{
    public static VehicleHUD Instance { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI vitesseText;
    public TextMeshProUGUI gearText;
    public Slider essenceSlider;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI damageText;
    
    [Header("UI Container")]
    public GameObject hudContainer; // Reference to the parent container of all HUD elements

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
            
        // Ensure the HUD is hidden at start
        if (hudContainer != null)
            hudContainer.SetActive(false);
    }

    public void UpdateSpeed(float nouvelleVitesse)
    {
        if (vitesseText != null)
            vitesseText.text = $"{nouvelleVitesse:F0} km/h";
    }

    public void UpdateStats(string texte)
    {
        if (statsText != null)
            statsText.text = texte;
    }

    public void UpdateDamage(float degats)
    {
        if (damageText != null)
            damageText.text = $"Dégâts : {degats:F0} %";
    }

    public void UpdateFuel(float niveau)
    {
        if (essenceSlider != null)
            essenceSlider.value = Mathf.Clamp01(niveau / 100f);
    }

    // Méthodes alias si vous préférez
    public void SetVitesse(float nouvelleVitesse) => UpdateSpeed(nouvelleVitesse);
    public void SetRapport(int rapport)
    {
        if (gearText != null)
            gearText.text = $"Rapport : {rapport}";
    }
    public void SetEssence(float niveau) => UpdateFuel(niveau);
    
    // Adding the missing methods
    
    /// <summary>
    /// Show the vehicle HUD and initialize it with the given vehicle
    /// </summary>
    public void ShowHUD(IVehicle vehicle)
    {
        if (hudContainer != null)
            hudContainer.SetActive(true);
            
        // Initialize HUD values
        if (vehicle != null)
        {
            UpdateSpeed(vehicle.CurrentSpeed);
            UpdateFuel(vehicle.FuelLevel / 100f);
            UpdateDamage(100f - (vehicle.Health / 100f * 100f));
            UpdateStats($"Vehicle: {vehicle.DisplayName}");
        }
    }
    
    /// <summary>
    /// Hide the vehicle HUD
    /// </summary>
    public void HideHUD()
    {
        if (hudContainer != null)
            hudContainer.SetActive(false);
    }
}