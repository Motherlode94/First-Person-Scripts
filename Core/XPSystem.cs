using System.Collections;
using System.Collections.Generic;

public static class XPSystem
{
    // XP total accumulé
    public static int CurrentXP { get; private set; } = 0;
    // Niveau courant (niveau 1 à 0 XP)
    public static int CurrentLevel { get; private set; } = 1;

    // XP requis par niveau (modifiable)
    private const int xpPerLevel = 100;

    /// <summary>
    /// Ajoute de l'XP et fait monter de niveau si on atteint le palier.
    /// </summary>
    public static void AddXP(int amount)
    {
        CurrentXP += amount;
        // calcule nouveau niveau
        int newLevel = (CurrentXP / xpPerLevel) + 1;
        if (newLevel > CurrentLevel)
            CurrentLevel = newLevel;
        UIManager.Instance?.FlashXP(); // Ajout notification UI
    }
}
