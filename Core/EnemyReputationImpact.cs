using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class EnemyReputationImpact : MonoBehaviour
{
    [Header("Réputation")]
    [SerializeField] private string factionID = "Gang";
    [SerializeField] private int killReputationChange = -10;
    [SerializeField] private bool notifyOtherFactions = true;
    
    [Header("Notifications entre factions")]
    [SerializeField] private string[] relatedFactions;
    [SerializeField] private int[] relatedFactionImpacts; // Valeurs positives ou négatives

    private EnemyAI enemyAI;
    private bool hasAppliedReputation = false;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        
        if (enemyAI != null)
        {
            // Vérifier si l'événement OnDamaged existe et s'y abonner
            try
            {
                enemyAI.OnDamaged += HandleDamage;
                Debug.Log("EnemyReputationImpact: Abonné avec succès à OnDamaged");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EnemyReputationImpact: Impossible de s'abonner à OnDamaged: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("EnemyReputationImpact: Aucun composant EnemyAI trouvé sur cet objet!");
        }
    }
    
    private void OnDestroy()
    {
        if (enemyAI != null)
        {
            try
            {
                enemyAI.OnDamaged -= HandleDamage;
            }
            catch (System.Exception)
            {
                // Ignorer l'erreur lors du désabonnement
            }
        }
    }

    private void OnEnable()
    {
        try
        {
            EnemyAI.OnEnemyAIKilled += HandleEnemyKilled;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EnemyReputationImpact: Impossible de s'abonner à OnEnemyAIKilled: {e.Message}");
        }
    }
    
    private void OnDisable()
    {
        try
        {
            EnemyAI.OnEnemyAIKilled -= HandleEnemyKilled;
        }
        catch (System.Exception)
        {
            // Ignorer l'erreur
        }
    }
    
    // Méthode pour gérer l'événement OnEnemyAIKilled
    private void HandleEnemyKilled(string id)
    {
        if (enemyAI != null && enemyAI.ID == id && !hasAppliedReputation)
        {
            ApplyReputationImpact();
        }
    }
    
    // Méthode pour gérer l'événement OnDamaged
    private void HandleDamage()
    {
        // Si l'ennemi est mort et que nous n'avons pas encore appliqué l'impact de réputation
        if (!enemyAI.IsAlive && !hasAppliedReputation)
        {
            ApplyReputationImpact();
        }
    }
    
    // Méthode commune pour appliquer l'impact de réputation
    private void ApplyReputationImpact()
    {
        // Éviter d'appliquer plusieurs fois
        hasAppliedReputation = true;
        
        // Appliquer le changement de réputation avec la faction principale
        if (ReputationManager.instance != null)
        {
            // Appliquer le changement principal
            ReputationManager.instance.ChangeReputation(factionID, killReputationChange);
            
            // Si activé, notifier les factions associées
            if (notifyOtherFactions && relatedFactions != null && relatedFactionImpacts != null)
            {
                int min = Mathf.Min(relatedFactions.Length, relatedFactionImpacts.Length);
                for (int i = 0; i < min; i++)
                {
                    ReputationManager.instance.ChangeReputation(relatedFactions[i], relatedFactionImpacts[i]);
                }
            }
        }
        else
        {
            Debug.LogWarning("EnemyReputationImpact: ReputationManager.instance est null!");
        }
    }
}