#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Éditeur personnalisé pour les InventoryItem afin de faciliter la gestion des IDs
/// </summary>
[CustomEditor(typeof(InventoryItem))]
public class InventoryItemEditor : Editor
{
    // Les catégories d'items que nous supportons dans l'éditeur
    private readonly string[] itemCategories = new string[]
    {
        // Armes
        "Pistol",
        "Rifle",
        "Shotgun",
        "SniperRifle",
        "AssaultRifle",
        "SMG",
        "Revolver",
        "MachineGun",
        "Grenade",
        "MeleeWeapon",
        
        // Munitions
        "PistolAmmo",
        "RifleAmmo",
        "ShotgunAmmo",
        "SniperAmmo",
        "RocketAmmo",
        
        // Objets de quête
        "QuestItem",
        "Key",
        "Document",
        "Intel",
        "Package",
        
        // Consommables
        "HealthPack",
        "Medkit",
        "Bandage",
        "Stimpack",
        "Antidote",
        "Food",
        "Water",
        "Potion",
        
        // Équipement
        "Armor",
        "Helmet",
        "Shield",
        "Backpack",
        "NightVision",
        "Binoculars",
        
        // Électronique
        "Battery",
        "Circuit",
        "Chip",
        "Device",
        "Radio",
        "Flashlight",
        
        // Ressources
        "Scrap",
        "Metal",
        "Wood",
        "Plastic",
        "Chemical",
        "Fuel",
        
        // Livrables
        "Deliverable",
        "MedicalSupply",
        "WeaponCache"
    };

    // Variables pour gérer l'état de l'éditeur
    private string selectedCategory = "";
    private int selectedNumber = 1;
    private int selectedCategoryIndex = 0;
    private bool showIDGenerator = false;
    private bool showIDValidator = false;
    private List<InventoryItem> similarItems = new List<InventoryItem>();
    private bool hasSearched = false;
    
    // Style pour les alertes
    private GUIStyle warningStyle;
    private GUIStyle successStyle;

    public override void OnInspectorGUI()
    {
        // Initialiser les styles
        if (warningStyle == null)
        {
            warningStyle = new GUIStyle(EditorStyles.helpBox);
            warningStyle.normal.textColor = new Color(0.9f, 0.4f, 0.1f);
            warningStyle.fontSize = 12;
            warningStyle.fontStyle = FontStyle.Bold;
            
            successStyle = new GUIStyle(EditorStyles.helpBox);
            successStyle.normal.textColor = new Color(0.2f, 0.7f, 0.2f);
            successStyle.fontSize = 12;
            successStyle.fontStyle = FontStyle.Bold;
        }
        
        // Récupérer la référence à l'InventoryItem
        InventoryItem item = (InventoryItem)target;
        
        // Dessin de l'inspecteur par défaut
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Titre de la section des outils ID
        EditorGUILayout.LabelField("Outils de Gestion des IDs", EditorStyles.boldLabel);
        
        // Vérification de l'ID actuel
        if (!string.IsNullOrEmpty(item.itemID))
        {
            bool isValidID = ItemIDUtility.IsValidItemID(item.itemID);
            EditorGUILayout.BeginHorizontal();
            
            // Afficher un message de validation
            if (isValidID)
            {
                EditorGUILayout.LabelField("✓ ID valide", successStyle);
                string category = ItemIDUtility.GetCategoryFromID(item.itemID);
                int number = ItemIDUtility.GetNumberFromID(item.itemID);
                EditorGUILayout.LabelField($"Catégorie: {category}, Numéro: {number}");
            }
            else
            {
                EditorGUILayout.LabelField("⚠ ID non conforme au standard", warningStyle);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("L'ID d'item est vide. Veuillez définir un ID valide.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(5);
        
        // Générateur d'ID
        showIDGenerator = EditorGUILayout.Foldout(showIDGenerator, "Générateur d'ID", true);
        if (showIDGenerator)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Liste déroulante pour la catégorie
            selectedCategoryIndex = EditorGUILayout.Popup("Catégorie:", selectedCategoryIndex, itemCategories);
            selectedCategory = itemCategories[selectedCategoryIndex];
            
            // Champ pour le numéro
            selectedNumber = EditorGUILayout.IntSlider("Numéro:", selectedNumber, 1, 99);
            
            // Générer le nouvel ID
            string newID = ItemIDUtility.GenerateItemID(selectedCategory, selectedNumber);
            EditorGUILayout.LabelField("ID généré:", newID);
            
            // Bouton pour appliquer l'ID
            if (GUILayout.Button("Appliquer cet ID"))
            {
                Undo.RecordObject(item, "Change Item ID");
                item.itemID = newID;
                EditorUtility.SetDirty(item);
                hasSearched = false; // Réinitialiser la recherche
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(5);
        
        // Vérificateur d'ID similaires
        showIDValidator = EditorGUILayout.Foldout(showIDValidator, "Vérificateur d'IDs similaires", true);
        if (showIDValidator)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Bouton pour rechercher les IDs similaires dans le projet
            if (GUILayout.Button("Rechercher des IDs similaires"))
            {
                similarItems.Clear();
                FindSimilarItems(item);
                hasSearched = true;
            }
            
            // Afficher les résultats de la recherche
            if (hasSearched)
            {
                if (similarItems.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Trouvé {similarItems.Count} item(s) avec des IDs similaires:", MessageType.Warning);
                    
                    foreach (var similarItem in similarItems)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(similarItem.name, GUILayout.Width(150));
                        EditorGUILayout.LabelField(similarItem.itemID, GUILayout.Width(100));
                        
                        // Bouton pour sélectionner l'item similaire
                        if (GUILayout.Button("Sélectionner", GUILayout.Width(100)))
                        {
                            Selection.activeObject = similarItem;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Aucun item avec un ID similaire trouvé.", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(5);
        
        // Boutons d'actions rapides
        EditorGUILayout.BeginHorizontal();
        
        // Extraire la catégorie du nom de l'item si possible
        if (GUILayout.Button("Auto-ID depuis le nom"))
        {
            string itemName = item.name;
            foreach (string category in itemCategories)
            {
                if (itemName.Contains(category))
                {
                    // Trouver le prochain numéro disponible
                    int nextNumber = FindNextAvailableNumber(category);
                    string newID = ItemIDUtility.GenerateItemID(category, nextNumber);
                    
                    Undo.RecordObject(item, "Auto Generate Item ID");
                    item.itemID = newID;
                    EditorUtility.SetDirty(item);
                    break;
                }
            }
        }
        
        // Synchroniser le displayName avec l'itemID
        if (GUILayout.Button("Sync displayName"))
        {
            if (!string.IsNullOrEmpty(item.itemID) && ItemIDUtility.IsValidItemID(item.itemID))
            {
                string category = ItemIDUtility.GetCategoryFromID(item.itemID);
                
                Undo.RecordObject(item, "Sync Display Name");
                item.displayName = category;
                EditorUtility.SetDirty(item);
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Trouve les items avec des IDs similaires dans le projet
    /// </summary>
    private void FindSimilarItems(InventoryItem currentItem)
    {
        // Si l'ID est invalide ou vide, on ne peut pas chercher d'items similaires
        if (string.IsNullOrEmpty(currentItem.itemID) || !ItemIDUtility.IsValidItemID(currentItem.itemID))
            return;
        
        string currentCategory = ItemIDUtility.GetCategoryFromID(currentItem.itemID);
        
        // Chercher tous les InventoryItem dans le projet
        string[] guids = AssetDatabase.FindAssets("t:InventoryItem");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InventoryItem item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
            
            // Ignorer l'item actuel et les items sans ID valide
            if (item == null || item == currentItem || string.IsNullOrEmpty(item.itemID) || !ItemIDUtility.IsValidItemID(item.itemID))
                continue;
            
            // Vérifier si la catégorie est la même
            string itemCategory = ItemIDUtility.GetCategoryFromID(item.itemID);
            if (itemCategory == currentCategory)
            {
                similarItems.Add(item);
            }
        }
    }
    
    /// <summary>
    /// Trouve le prochain numéro disponible pour une catégorie donnée
    /// </summary>
    private int FindNextAvailableNumber(string category)
    {
        // Liste pour stocker les numéros déjà utilisés
        List<int> usedNumbers = new List<int>();
        
        // Recherche tous les InventoryItem dans le projet
        string[] guids = AssetDatabase.FindAssets("t:InventoryItem");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InventoryItem item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
            
            // Ignorer les items sans ID valide
            if (item == null || string.IsNullOrEmpty(item.itemID) || !ItemIDUtility.IsValidItemID(item.itemID))
                continue;
            
            // Vérifier si la catégorie est la même
            string itemCategory = ItemIDUtility.GetCategoryFromID(item.itemID);
            if (itemCategory == category)
            {
                int number = ItemIDUtility.GetNumberFromID(item.itemID);
                usedNumbers.Add(number);
            }
        }
        
        // Trouver le premier numéro non utilisé
        for (int i = 1; i <= 99; i++)
        {
            if (!usedNumbers.Contains(i))
                return i;
        }
        
        // Par défaut, retourner 1
        return 1;
    }
}
#endif