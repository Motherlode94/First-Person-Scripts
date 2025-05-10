using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public class ReputationNotifier : MonoBehaviour
{
    public static ReputationNotifier instance;

    [Header("Configuration")]
    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] private Transform notificationParent;
    [SerializeField] private float notificationDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("Style")]
    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private AudioClip positiveSound;
    [SerializeField] private AudioClip negativeSound;
    
    [Header("Avancé")]
    [SerializeField] private int maxNotifications = 3;
    [SerializeField] private float notificationSpacing = 5f;
    
    private AudioSource audioSource;
    private Queue<GameObject> activeNotifications = new Queue<GameObject>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
            
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void ShowNotification(string faction, int amount)
    {
        if (notificationPrefab == null || notificationParent == null)
            return;
            
        // Créer le notification
        GameObject notification = Instantiate(notificationPrefab, notificationParent);
        
        // Configurer la notification
        TextMeshProUGUI text = notification.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            string sign = amount > 0 ? "+" : "";
            text.text = $"{faction}: {sign}{amount} réputation";
            
            // Définir la couleur
            if (amount > 0)
                text.color = positiveColor;
            else if (amount < 0)
                text.color = negativeColor;
            else
                text.color = neutralColor;
        }
        
        // Positionner la notification
        RectTransform rect = notification.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(0, activeNotifications.Count * -notificationSpacing);
        }
        
        // Ajouter à la file
        activeNotifications.Enqueue(notification);
        
        // Limiter le nombre de notifications
        if (activeNotifications.Count > maxNotifications)
        {
            GameObject oldNotification = activeNotifications.Dequeue();
            Destroy(oldNotification);
        }
        
        // Jouer un son selon la valeur
        if (audioSource != null)
        {
            if (amount > 0 && positiveSound != null)
                audioSource.PlayOneShot(positiveSound);
            else if (amount < 0 && negativeSound != null)
                audioSource.PlayOneShot(negativeSound);
        }
        
        // Animer et détruire après délai
        StartCoroutine(AnimateNotification(notification));
    }
    
    public void ShowNotification(string faction, int amount, string reason)
    {
        if (notificationPrefab == null || notificationParent == null)
            return;
            
        // Créer le notification
        GameObject notification = Instantiate(notificationPrefab, notificationParent);
        
        // Configurer la notification avec la raison
        TextMeshProUGUI text = notification.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            string sign = amount > 0 ? "+" : "";
            text.text = $"{faction}: {sign}{amount} réputation\n<size=80%>{reason}</size>";
            
            // Définir la couleur
            if (amount > 0)
                text.color = positiveColor;
            else if (amount < 0)
                text.color = negativeColor;
            else
                text.color = neutralColor;
        }
        
        // Positionner la notification
        RectTransform rect = notification.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(0, activeNotifications.Count * -notificationSpacing);
        }
        
        // Ajouter à la file
        activeNotifications.Enqueue(notification);
        
        // Limiter le nombre de notifications
        if (activeNotifications.Count > maxNotifications)
        {
            GameObject oldNotification = activeNotifications.Dequeue();
            Destroy(oldNotification);
        }
        
        // Jouer un son selon la valeur
        if (audioSource != null)
        {
            if (amount > 0 && positiveSound != null)
                audioSource.PlayOneShot(positiveSound);
            else if (amount < 0 && negativeSound != null)
                audioSource.PlayOneShot(negativeSound);
        }
        
        // Animer et détruire après délai
        StartCoroutine(AnimateNotification(notification));
    }
    
    private IEnumerator AnimateNotification(GameObject notification)
    {
        // Récupérer le canvas group ou en ajouter un
        CanvasGroup group = notification.GetComponent<CanvasGroup>();
        if (group == null)
            group = notification.AddComponent<CanvasGroup>();
            
        // Fade in
        group.alpha = 0f;
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            group.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        group.alpha = 1f;
        
        // Attendre la durée d'affichage
        yield return new WaitForSeconds(notificationDuration);
        
        // Fade out
        elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            group.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        group.alpha = 0f;
        
        // Supprimer la notification et mettre à jour la file
        if (activeNotifications.Contains(notification))
        {
            activeNotifications = new Queue<GameObject>(System.Linq.Enumerable.Where(activeNotifications, n => n != notification));
        }
        
        // Repositionner les notifications restantes
        int index = 0;
        foreach (var notif in activeNotifications)
        {
            RectTransform rect = notif.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0, index * -notificationSpacing);
                index++;
            }
        }
        
        Destroy(notification);
    }
}