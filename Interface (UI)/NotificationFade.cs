using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationFade : MonoBehaviour
{
    public float fadeInDuration = 0.5f;
    public float displayDuration = 2f;
    public float fadeOutDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private float timer = 0f;
    private enum FadeState { FadingIn, Displaying, FadingOut }
    private FadeState state = FadeState.FadingIn;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        switch (state)
        {
            case FadeState.FadingIn:
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
                if (timer >= fadeInDuration)
                {
                    timer = 0f;
                    state = FadeState.Displaying;
                }
                break;

            case FadeState.Displaying:
                if (timer >= displayDuration)
                {
                    timer = 0f;
                    state = FadeState.FadingOut;
                }
                break;

            case FadeState.FadingOut:
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
                if (timer >= fadeOutDuration)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }
}
