// Assets/Scripts/Triggers/BossEnemy.cs
using UnityEngine;
using System;
#if EMERALD_AI_2024_PRESENT
using EmeraldAI;
#endif

[DisallowMultipleComponent]
public class BossEnemy : MonoBehaviour
{
    [Tooltip("Identifiant unique de ce boss")]
    public string bossID;

    // Event global écouté par ton UI display
    public static event Action<string> OnBossEnemyKilled;

    #if EMERALD_AI_2024_PRESENT
    private EmeraldHealth healthComp;
    #endif

    void Awake()
    {
        #if EMERALD_AI_2024_PRESENT
        healthComp = GetComponent<EmeraldHealth>();
        if (healthComp != null)
            healthComp.OnDeath += HandleDeath;
        #endif
    }

    void OnDestroy()
    {
        #if EMERALD_AI_2024_PRESENT
        if (healthComp != null)
            healthComp.OnDeath -= HandleDeath;
        #endif
    }

    // Méthode appelée quand EmeraldHealth déclenche OnDeath
    private void HandleDeath()
    {
        OnBossEnemyKilled?.Invoke(bossID);
        Destroy(gameObject);
    }
}
