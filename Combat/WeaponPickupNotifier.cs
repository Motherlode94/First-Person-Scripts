// Assets/Scripts/WeaponPickupNotifier.cs
using UnityEngine;
using NeoFPS;           // namespace de NeoFPS
using UnityEngine.Events;

[RequireComponent(typeof(NeoFPS.InteractivePickup))]
public class WeaponPickupNotifier : MonoBehaviour
{
    [Tooltip("Même ID que dans ta mission EquipWeapon")]
    public string weaponID;
    void Awake()
    {
        var ip = GetComponent<NeoFPS.InteractivePickup>();
        if (ip != null)
            ip.onPickedUp += OnPickedUp;
    }

    void OnDestroy()
    {
        var ip = GetComponent<NeoFPS.InteractivePickup>();
        if (ip != null)
            ip.onPickedUp -= OnPickedUp;
    }

    void OnPickedUp(NeoFPS.IInventory inv, NeoFPS.IInventoryItem item)
    {
        // On notifie via la méthode static de WeaponManager
        WeaponManager.NotifyWeaponEquipped(weaponID);
    }
}
