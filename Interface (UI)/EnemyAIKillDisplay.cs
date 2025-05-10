using System.Linq;
using UnityEngine;
using TMPro;

public class EnemyAIKillDisplay : MonoBehaviour
{
    [Header("ID à suivre")]
    [Tooltip("Liste d'IDs d'ennemis (doit correspondre à EnemyAI.ID)")]
    [SerializeField] private string[] enemyIDs;

    [Tooltip("Liste d'IDs de boss (doit correspondre à BossEnemy.bossID)")]
    [SerializeField] private string[] bossIDs;

    [Header("Référence UI")]
    [Tooltip("Le TMP_Text (enfant) qui affichera Tués et Restants")]
    [SerializeField] private TMP_Text displayText;

    private int totalCount;
    private int killedCount;

    void Awake()
    {
        // Initialisation à 0 au départ
        totalCount = 0;
        killedCount = 0;
        UpdateDisplay();
    }

    void OnEnable()
    {
        // Utilise les événements des classes actuelles, pas des références inexistantes
        EnemyAI.OnEnemyAIKilled += OnEnemyAIKilled;
        BossEnemy.OnBossEnemyKilled += OnBossKilled;
    }

    void OnDisable()
    {
        // Désinscription des événements
        EnemyAI.OnEnemyAIKilled -= OnEnemyAIKilled;
        BossEnemy.OnBossEnemyKilled -= OnBossKilled;
    }

    private void OnEnemyAIKilled(string id)
    {
        if (!enemyIDs.Contains(id))
            return;

        killedCount = Mathf.Min(killedCount + 1, totalCount);
        UpdateDisplay();
    }

    private void OnBossKilled(string id)
    {
        if (!bossIDs.Contains(id))
            return;

        killedCount = Mathf.Min(killedCount + 1, totalCount);
        UpdateDisplay();
    }

    public void ResetKills(int newTotalCount)
    {
        killedCount = 0;
        totalCount = newTotalCount;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (displayText == null) 
            return;

        int remaining = Mathf.Max(0, totalCount - killedCount);
        displayText.text = $"Tués : {killedCount}\nRestants : {remaining}";
    }
}