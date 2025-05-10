using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MissionCondition
{
    public enum ConditionType
    {
        StealthOnly,          // Ne pas être détecté
        TimeLimit,            // Limite de temps
        NoWeapons,            // Pas d'armes autorisées
        NoKills,              // Interdiction de tuer
        SpecificWeaponOnly,   // Utiliser uniquement une arme spécifique
        LowHealth             // Santé limitée
    }
    
    public ConditionType type;
    public string parameter;  // Paramètre spécifique à la condition (ex: ID d'arme, limite de temps)
    public float value;       // Valeur numérique (temps, santé, etc.)
    
    [TextArea(2, 3)]
    public string description; // Description pour l'UI
    
    public bool isFailed = false;
}

