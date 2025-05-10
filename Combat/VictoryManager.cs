// Assets/Scripts/Game/VictoryManager.cs
using UnityEngine;
using TMPro;
using System.Collections;
public class VictoryManager : MonoBehaviour
{
    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text victoryMessage;
    [SerializeField] private float delayBeforeVictory = 2f;

    private bool victoryTriggered = false;

    void OnEnable()
    {
        WaveSpawner.OnAllWavesComplete += OnAllWavesCompleted;
    }

    void OnDisable()
    {
        WaveSpawner.OnAllWavesComplete -= OnAllWavesCompleted;
    }

    private void OnAllWavesCompleted(int finalWave)
    {
        if (victoryTriggered) return;

        StartCoroutine(HandleVictorySequence());
    }

    private IEnumerator HandleVictorySequence()
    {
        yield return new WaitForSeconds(delayBeforeVictory);

        if (MissionManager.Instance != null && MissionManager.Instance.AllMissionsCompleted)
        {
            TriggerVictory();
        }
    }

    private void TriggerVictory()
    {
        victoryTriggered = true;

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (victoryMessage != null)
            victoryMessage.text = "🎉 Victoire ! Toutes les vagues et missions terminées ! 🎉";

        Time.timeScale = 0f; // Pause le jeu
        Debug.Log("[VictoryManager] Victoire déclenchée !");
    }
}
