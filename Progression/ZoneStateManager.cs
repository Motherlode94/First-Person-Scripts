using System.Collections;
// Assets/Scripts/Missions/ZoneStateManager.cs
using System.Collections.Generic;
using UnityEngine;

public class ZoneStateManager : MonoBehaviour
{
    public static ZoneStateManager instance;
    public static ZoneStateManager Instance => instance; // Ajout compatibilité MissionManager

    private HashSet<string> discoveredZones = new();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public bool IsDiscovered(string zoneID)
        => discoveredZones.Contains(zoneID);

    public void MarkAsDiscovered(string zoneID)
    {
        if (!discoveredZones.Contains(zoneID))
            discoveredZones.Add(zoneID);
    }

    public void ResetForMission(Mission mission)
    {
        foreach (var obj in mission.objectives)
        {
            if (obj.type != ObjectiveType.ReachZone) continue;

            foreach (var notifier in FindObjectsOfType<ZoneDiscoveryNotifier>())
            {
                if (notifier.zoneID == obj.targetID && !IsDiscovered(notifier.zoneID))
                {
                    notifier.gameObject.SetActive(true);
                }
            }
        }
    }
}