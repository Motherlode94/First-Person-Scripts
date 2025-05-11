// ItemIDUtility.cs
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utilitaire pour gérer et valider les identifiants d'items dans le jeu
/// </summary>
public static class ItemIDUtility
{
    // Catégories principales d'items
    public enum ItemCategory
    {
        Weapon,
        Ammo,
        QuestItem,
        Consumable,
        Equipment,
        Electronic,
        Resource,
        Deliverable
    }

    // Sous-catégories d'armes
    public enum WeaponSubcategory
    {
        Pistol,
        Rifle,
        Shotgun,
        SniperRifle,
        AssaultRifle,
        SMG,
        Revolver,
        MachineGun,
        Grenade,
        MeleeWeapon
    }

    // Mapping des catégories vers leurs préfixes d'ID
    private static readonly Dictionary<ItemCategory, string[]> CategoryPrefixes = new Dictionary<ItemCategory, string[]>
    {
        { ItemCategory.Weapon, new[] { "Weapon", "Pistol", "Rifle", "Shotgun", "SniperRifle", "AssaultRifle", "SMG", "Revolver", "MachineGun", "Grenade", "MeleeWeapon" } },
        { ItemCategory.Ammo, new[] { "Ammo", "PistolAmmo", "RifleAmmo", "ShotgunAmmo", "SniperAmmo", "RocketAmmo" } },
        { ItemCategory.QuestItem, new[] { "QuestItem", "Key", "Document", "Intel", "Package" } },
        { ItemCategory.Consumable, new[] { "HealthPack", "Medkit", "Bandage", "Stimpack", "Antidote", "Food", "Water", "Potion" } },
        { ItemCategory.Equipment, new[] { "Armor", "Helmet", "Shield", "Backpack", "NightVision", "Binoculars" } },
        { ItemCategory.Electronic, new[] { "Battery", "Circuit", "Chip", "Device", "Radio", "Flashlight" } },
        { ItemCategory.Resource, new[] { "Scrap", "Metal", "Wood", "Plastic", "Chemical", "Fuel" } },
        { ItemCategory.Deliverable, new[] { "Deliverable", "Package", "MedicalSupply", "WeaponCache" } }
    };

    // Pattern regex pour valider les IDs
    private static readonly Regex IdPattern = new Regex(@"^([A-Z][a-zA-Z]+)(\d{2})$");

    /// <summary>
    /// Génère un identifiant d'item standard basé sur la catégorie et le numéro
    /// </summary>
    /// <param name="category">Préfixe de catégorie</param>
    /// <param name="number">Numéro séquentiel (1-99)</param>
    /// <returns>ID formaté</returns>
    public static string GenerateItemID(string category, int number)
    {
        if (string.IsNullOrEmpty(category))
            throw new System.ArgumentException("La catégorie ne peut pas être vide", nameof(category));

        if (number < 1 || number > 99)
            throw new System.ArgumentOutOfRangeException(nameof(number), "Le numéro doit être entre 1 et 99");

        // S'assurer que la catégorie commence par une majuscule
        if (char.IsLower(category[0]))
            category = char.ToUpper(category[0]) + category.Substring(1);

        // Formater le numéro avec deux chiffres
        string formattedNumber = number.ToString("D2");

        return $"{category}{formattedNumber}";
    }

    /// <summary>
    /// Vérifie si l'ID donné est conforme au standard
    /// </summary>
    /// <param name="itemID">ID d'item à vérifier</param>
    /// <returns>True si l'ID est valide</returns>
    public static bool IsValidItemID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return false;

        return IdPattern.IsMatch(itemID);
    }

    /// <summary>
    /// Extrait la catégorie d'un ID d'item
    /// </summary>
    /// <param name="itemID">ID d'item</param>
    /// <returns>Catégorie de l'item ou null si l'ID est invalide</returns>
    public static string GetCategoryFromID(string itemID)
    {
        if (!IsValidItemID(itemID))
            return null;

        var match = IdPattern.Match(itemID);
        return match.Groups[1].Value;
    }

    /// <summary>
    /// Extrait le numéro d'un ID d'item
    /// </summary>
    /// <param name="itemID">ID d'item</param>
    /// <returns>Numéro de l'item ou -1 si l'ID est invalide</returns>
    public static int GetNumberFromID(string itemID)
    {
        if (!IsValidItemID(itemID))
            return -1;

        var match = IdPattern.Match(itemID);
        return int.Parse(match.Groups[2].Value);
    }

    /// <summary>
    /// Trouve la catégorie principale d'un ID d'item
    /// </summary>
    /// <param name="itemID">ID d'item</param>
    /// <returns>Catégorie principale ou null si non trouvée</returns>
    public static ItemCategory? GetMainCategory(string itemID)
    {
        if (!IsValidItemID(itemID))
            return null;

        string category = GetCategoryFromID(itemID);
        
        foreach (var entry in CategoryPrefixes)
        {
            if (System.Array.Exists(entry.Value, prefix => prefix == category))
                return entry.Key;
        }

        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Vérifie les IDs de tous les InventoryItem dans le projet
    /// </summary>
    [MenuItem("Tools/Validate Item IDs")]
    public static void ValidateAllItemIDs()
    {
        string[] guids = AssetDatabase.FindAssets("t:InventoryItem");
        bool allValid = true;
        
        Debug.Log($"Validation de {guids.Length} items d'inventaire...");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InventoryItem item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
            
            if (item == null) continue;
            
            if (!IsValidItemID(item.itemID))
            {
                Debug.LogWarning($"ID invalide: {item.itemID} sur {item.name} ({path})");
                allValid = false;
            }
        }
        
        if (allValid)
            Debug.Log("✓ Tous les identifiants d'items sont valides!");
        else
            Debug.LogError("⚠ Certains identifiants d'items ne sont pas conformes au standard!");
    }
    
    /// <summary>
    /// Génère un rapport d'IDs d'items utilisés
    /// </summary>
    [MenuItem("Tools/Generate Item ID Report")]
    public static void GenerateItemIDReport()
    {
        string[] guids = AssetDatabase.FindAssets("t:InventoryItem");
        Dictionary<string, List<string>> categoryItemsMap = new Dictionary<string, List<string>>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InventoryItem item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
            
            if (item == null || string.IsNullOrEmpty(item.itemID)) continue;
            
            string category = GetCategoryFromID(item.itemID) ?? "Unknown";
            
            if (!categoryItemsMap.ContainsKey(category))
                categoryItemsMap[category] = new List<string>();
                
            categoryItemsMap[category].Add($"{item.itemID} - {item.displayName} ({path})");
        }
        
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("# Rapport des IDs d'Items\n");
        report.AppendLine($"Généré le: {System.DateTime.Now}\n");
        
        foreach (var category in categoryItemsMap.Keys)
        {
            report.AppendLine($"## Catégorie: {category}");
            report.AppendLine($"Total: {categoryItemsMap[category].Count} items\n");
            
            foreach (var itemInfo in categoryItemsMap[category])
            {
                report.AppendLine($"- {itemInfo}");
            }
            
            report.AppendLine();
        }
        
        string reportPath = System.IO.Path.Combine(Application.dataPath, "ItemIDReport.md");
        System.IO.File.WriteAllText(reportPath, report.ToString());
        
        Debug.Log($"Rapport généré dans: {reportPath}");
        
        // Rafraîchir l'Asset Database pour montrer le fichier
        AssetDatabase.Refresh();
    }
#endif
}