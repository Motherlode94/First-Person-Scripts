// Assets/Scripts/Triggers/ZoneDiscoveryNotifier.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class ZoneDiscoveryNotifier : MonoBehaviour
{
    [Tooltip("ID de la zone à inspecter (doit correspondre au Target ID Reach Zone de ta mission)")]
    public string zoneID;

    public static event Action<string> OnZoneEntered;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[{name}] Collider mis en trigger automatiquement pour detection de zone.", this);
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (ZoneStateManager.instance != null && !ZoneStateManager.instance.IsDiscovered(zoneID))
            {
                ZoneStateManager.instance.MarkAsDiscovered(zoneID);
                OnZoneEntered?.Invoke(zoneID);
            }
        }
    }
}