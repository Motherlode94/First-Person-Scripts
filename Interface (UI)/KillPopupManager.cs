// Assets/Scripts/UI/KillPopupManager.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class KillPopupManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject popupPanel; // Le parent
    [SerializeField] private GameObject popupPrefab; // Le prefab TMP_Text
    [SerializeField] private float popupLifetime = 1.5f;
    [SerializeField] private Vector2 randomOffsetRange = new Vector2(30f, 50f);

    private int activePopups = 0;

    public static KillPopupManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public void ShowKillPopup()
    {
        if (popupPanel == null || popupPrefab == null) return;

        popupPanel.SetActive(true);

        GameObject popup = Instantiate(popupPrefab, popupPanel.transform);
        TMP_Text text = popup.GetComponent<TMP_Text>();
        if (text != null)
            text.text = "+1 kill";

        Vector2 randomOffset = new Vector2(
            Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
            Random.Range(-randomOffsetRange.y, randomOffsetRange.y)
        );

        RectTransform rect = popup.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition += randomOffset;

        activePopups++;
        StartCoroutine(HidePopupAfterDelay(popup));
    }

    private IEnumerator HidePopupAfterDelay(GameObject popup)
    {
        yield return new WaitForSeconds(popupLifetime);

        if (popup != null)
            Destroy(popup);

        activePopups--;
        if (activePopups <= 0 && popupPanel != null)
            popupPanel.SetActive(false);
    }
}
