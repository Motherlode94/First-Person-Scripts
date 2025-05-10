// Assets/Scripts/UI/UITextPulse.cs
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class UITextPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float pulseScale = 1.1f;

    private TMP_Text text;
    private Vector3 originalScale;
    private float timer = 0f;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        originalScale = text.transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime * pulseSpeed;
        float scale = 1f + Mathf.Sin(timer) * (pulseScale - 1f);
        text.transform.localScale = originalScale * scale;
    }

    void OnDisable()
    {
        if (text != null)
            text.transform.localScale = originalScale;
    }
}
