using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MissionConsequence
{
    public string missionID;            // ID de la mission affectée
    public bool unlockOnSuccess;        // Déverrouiller si la mission actuelle réussit
    public bool unlockOnFailure;        // Déverrouiller si la mission actuelle échoue
    public bool modifyOnSuccess;        // Modifier la mission si réussite
    public bool modifyOnFailure;        // Modifier la mission si échec
    
    [Header("Modifications")]
    public List<string> addObjectives;  // Objectifs à ajouter
    public List<string> removeObjectives; // Objectifs à retirer
    public float difficultyMultiplier;  // Multiplicateur de difficulté
    
    [TextArea(2, 4)]
    public string consequenceDescription; // Description de la conséquence
}