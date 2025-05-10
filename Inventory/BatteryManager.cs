// Assets/Scripts/Systems/BatteryManager.cs
using System.Collections.Generic;
using UnityEngine;

public class BatteryManager : MonoBehaviour
{
    public static BatteryManager instance;

    private HashSet<string> collectedBatteries = new HashSet<string>();

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>Appelée lorsqu'une batterie est ramassée</summary>
    public static void CollectBattery(string id)
    {
    if (instance == null)
    {
        Debug.LogError("[BatteryManager] Aucune instance trouvée ! Création d'une instance...");
        // Créer automatiquement une instance si elle n'existe pas
        new GameObject("BatteryManager").AddComponent<BatteryManager>();
    }
    
    if (string.IsNullOrEmpty(id)) return;
    instance.collectedBatteries.Add(id);
    Debug.Log($"[BatteryManager] Batterie {id} collectée. Total: {instance.collectedBatteries.Count}");
    }

    /// <summary>Vérifie si une batterie a été ramassée</summary>
public static bool HasBattery(string id)
{
    bool result = instance != null && instance.collectedBatteries.Contains(id);
    Debug.Log($"[BatteryManager] Vérification batterie '{id}': {result}. Total batteries: {(instance != null ? instance.collectedBatteries.Count : 0)}");
    
    if (instance != null)
    {
        string allBatteries = string.Join(", ", instance.collectedBatteries);
        Debug.Log($"[BatteryManager] Batteries collectées: {allBatteries}");
    }
    
    return result;
}
}
