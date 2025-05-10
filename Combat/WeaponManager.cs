// Assets/Scripts/Weapons/WeaponManager.cs
using System;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager instance;

    // l’ID arme équipée + event
    public static string CurrentWeaponID { get; private set; }
    public static event Action<string> OnWeaponEquipped;

    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Méthode publique à appeler pour équiper une arme.
    /// C’est ici que l’on met à jour l’ID et qu’on invoque l’event.
    /// </summary>
    public void Equip(string weaponID)
    {
        CurrentWeaponID = weaponID;
        OnWeaponEquipped?.Invoke(weaponID);
    }

    /// <summary>
    /// Wrapper static pour pouvoir appeler depuis n’importe où
    /// sans avoir à récupérer l’instance manuellement.
    /// </summary>
    public static void NotifyWeaponEquipped(string weaponID)
    {
        if (instance != null)
            instance.Equip(weaponID);
        else
        {
            // au cas (très rare) où l’instance n’existe pas encore
            CurrentWeaponID = weaponID;
            OnWeaponEquipped?.Invoke(weaponID);
        }
    }
}
