using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class PowerSwitch : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string switchID = "Switch01";
    [SerializeField] private string switchDisplayName = "";
    [SerializeField] private float activationDuration = 2f;
    [SerializeField] private KeyCode activationKey = KeyCode.E;
    [SerializeField] private bool requiresBattery = false;
    [SerializeField] private string requiredBatteryID = "";
    
    [Header("Visuals & Feedback")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text switchNameText;
    [SerializeField] private GameObject activationEffect;
    [SerializeField] private Light statusLight;
    [SerializeField] private Color inactiveColor = Color.red;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private AudioSource activationSound;
    
    [Header("Sequence")]
    [SerializeField] private SequenceManager sequenceManager;

    [Header("Events")]
    [SerializeField] private UnityEvent onSwitchActivated;

    // State
    private bool playerInRange = false;
    private bool isActivated = false;
    private float currentHoldTime = 0f;
    private Coroutine activationCoroutine = null;

    private void Start()
    {
        // Initialize components
        if (promptUI != null)
            promptUI.SetActive(false);
            
        if (activationEffect != null)
            activationEffect.SetActive(false);
            
        if (statusLight != null)
        {
            statusLight.color = inactiveColor;
            statusLight.intensity = 1f;
        }
    }

    private void Update()
    {
        if (!playerInRange || isActivated)
            return;

        // Check if battery is required but not available
        if (requiresBattery && !string.IsNullOrEmpty(requiredBatteryID) && 
            !BatteryManager.HasBattery(requiredBatteryID))
        {
            if (promptText != null)
                promptText.text = "Nécessite une batterie pour fonctionner";
            return;
        }

        if (Input.GetKey(activationKey))
        {
            if (currentHoldTime == 0f && HoldPromptUI.Instance != null)
            {
                HoldPromptUI.Instance.ShowPrompt($"Maintenir [{activationKey}] pour activer", activationDuration);
            }

            currentHoldTime += Time.deltaTime;
            
            if (HoldPromptUI.Instance != null)
            {
                HoldPromptUI.Instance.UpdateHoldTime(activationDuration - currentHoldTime);
            }

            if (currentHoldTime >= activationDuration)
            {
                ActivateSwitch();
            }
        }
        else if (currentHoldTime > 0f)
        {
            // Reset if key is released before completion
            currentHoldTime = 0f;
            if (HoldPromptUI.Instance != null)
            {
                HoldPromptUI.Instance.HidePrompt();
            }
        }
    }

    private void ActivateSwitch()
    {
        if (isActivated)
            return;

        isActivated = true;
        currentHoldTime = 0f;
        
        if (HoldPromptUI.Instance != null)
        {
            HoldPromptUI.Instance.HidePrompt();
        }

        // Feedback visuel
        if (activationEffect != null)
            activationEffect.SetActive(true);
        
        // Changer la couleur et l'intensité de la lumière
        if (statusLight != null)
        {
            statusLight.color = activeColor;
            statusLight.intensity = 2.5f; // Intensité plus forte pour un meilleur effet visuel
        }
        
        if (promptUI != null)
            promptUI.SetActive(false);

        // Désactiver le collider pour empêcher de nouvelles interactions
        GetComponent<Collider>().enabled = false;
        
        // Animer l'interrupteur
        activationCoroutine = StartCoroutine(AnimateSwitch());

        // Feedback audio
        if (activationSound != null)
            activationSound.Play();

        // Notifier le système de mission
        if (MissionManager.Instance != null)
            MissionManager.Instance.NotifyObjectives(ObjectiveType.ActivateSwitch, id: switchID);

        // Notifier le gestionnaire de séquence
        if (sequenceManager != null)
            sequenceManager.NotifySwitchActivated(switchID);
            
        // Notifier le système d'affichage de mission (si disponible)
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateCurrentSwitchID(switchID);

        // Déclencher les événements personnalisés
        onSwitchActivated?.Invoke();
        
        Debug.Log($"[PowerSwitch] Switch {switchID} activated successfully!");
    }

    private IEnumerator AnimateSwitch()
    {
        // Attendre un moment pour que les effets soient visibles
        yield return new WaitForSeconds(0.5f);
        
        // Faire disparaître le switch
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            // Sauvegarder la couleur d'origine
            Color originalColor = renderer.material.color;
            
            // S'assurer que le matériau peut être transparent
            renderer.material.SetFloat("_Mode", 3); // 3 = Transparent
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = 3000;
            
            // Animation de disparition
            float duration = 1.0f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                renderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // Désactiver les renderers après le fade out
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        // Conserver l'effet lumineux
        if (statusLight != null)
        {
            // Vous pouvez ajouter une pulsation légère pour l'effet lumineux
            StartCoroutine(PulseLight());
        }
    }
    
    private IEnumerator PulseLight()
    {
        if (statusLight == null) yield break;
        
        float minIntensity = 1.5f;
        float maxIntensity = 2.5f;
        float pulseSpeed = 1.0f;
        
        while (true)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1.0f);
            statusLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            if (!isActivated && promptUI != null)
                promptUI.SetActive(true);
                
            // Configurer le texte pour le prompt
            if (promptText != null)
            {
                // Si un nom personnalisé est défini, l'afficher au-dessus du message d'action
                if (!string.IsNullOrEmpty(switchDisplayName))
                    promptText.text = $"{switchDisplayName}\n";
                    
                // Ajouter le message d'action
                if (requiresBattery && !string.IsNullOrEmpty(requiredBatteryID) && 
                    !BatteryManager.HasBattery(requiredBatteryID))
                    promptText.text += "Nécessite une batterie pour fonctionner";
                else
                    promptText.text += $"Appuyez sur [{activationKey}] pour activer";
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Vérifier si le joueur sort de la zone d'activation
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            // Vérifier si le HoldPromptUI.Instance existe avant de l'utiliser
            if (HoldPromptUI.Instance != null)
            {
                HoldPromptUI.Instance.HidePrompt();
            }
            
            // Arrêter l'activation si en cours
            if (activationCoroutine != null)
            {
                StopCoroutine(activationCoroutine);
                activationCoroutine = null;
                currentHoldTime = 0f;
            }
        }
    }

    public void ResetSwitch()
    {
        isActivated = false;
        currentHoldTime = 0f;
        
        if (activationEffect != null)
            activationEffect.SetActive(false);
            
        if (statusLight != null)
            statusLight.color = inactiveColor;
            
        // Réactiver les renderers
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        // Réactiver le collider
        GetComponent<Collider>().enabled = true;
        
        // Arrêter toutes les coroutines
        StopAllCoroutines();
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
                Gizmos.DrawCube(box.center, box.size);
            else if (col is SphereCollider sphere)
                Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}