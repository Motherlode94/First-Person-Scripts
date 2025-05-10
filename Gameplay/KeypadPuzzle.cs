// KeypadPuzzle.cs - Version corrigée sans PlayerController
using UnityEngine;
using TMPro;

public class KeypadPuzzle : MonoBehaviour
{
    [Tooltip("Code correct à saisir")]
    public string correctCode = "1234";

    [Tooltip("Champ TMP pour l'affichage du code")]
    public TMP_Text displayText;

    [Tooltip("Zone de désamorçage à déclencher si succès")]
    public string disarmID = "bomb_A";

    [Tooltip("Objet à déplacer/ouvrir si le code est correct")]
    public Transform doorToOpen;
    [Tooltip("Déplacement final relatif (par ex: (0,2,0))")]
    public Vector3 openOffset = new Vector3(0, 2, 0);
    [Tooltip("Vitesse d'ouverture")]
    public float openSpeed = 1.5f;
    
    [Header("Effets")]
    [SerializeField] private AudioClip keyPressSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    private string currentInput = "";
    private bool isOpening = false;
    private Vector3 targetPosition;
    private AudioSource audioSource;
    
    void Awake()
    {
        // Obtenir l'AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Ne pas désactiver automatiquement en mode éditeur
        #if UNITY_EDITOR
        // En mode éditeur, on peut le laisser actif pour test
        #else
        gameObject.SetActive(false);
        #endif
        
        UpdateDisplay();
    }

    public void PressKey(string digit)
    {
        if (currentInput.Length >= correctCode.Length) return;
        
        // Jouer un son
        if (audioSource != null && keyPressSound != null)
            audioSource.PlayOneShot(keyPressSound);
            
        currentInput += digit;
        UpdateDisplay();
        
        // Si on a le bon nombre de chiffres, vérifier automatiquement
        if (currentInput.Length == correctCode.Length)
            Invoke("CheckCode", 0.5f);
    }

    public void PressClear()
    {
        // Jouer un son
        if (audioSource != null && keyPressSound != null)
            audioSource.PlayOneShot(keyPressSound);
            
        currentInput = "";
        UpdateDisplay();
    }

    private void CheckCode()
    {
        if (currentInput == correctCode)
        {
            PressEnter();
        }
        else
        {
            // Code incorrect
            Debug.Log("[Keypad] Code incorrect.");
            
            // Jouer un son d'erreur
            if (audioSource != null && errorSound != null)
                audioSource.PlayOneShot(errorSound);
                
            // Réinitialiser
            currentInput = "";
            UpdateDisplay();
        }
    }

    public void PressEnter()
    {
        if (currentInput == correctCode)
        {
            Debug.Log("[Keypad] Code correct ! Zone désarmée.");
            
            // Jouer un son de succès
            if (audioSource != null && successSound != null)
                audioSource.PlayOneShot(successSound);
            
            // Notifier le système de mission
            MissionManager.Instance?.NotifyObjectives(ObjectiveType.Disarm, id: disarmID);

            // Notifier le panneau de désamorçage
            DisarmPanel panel = FindObjectOfType<DisarmPanel>();
            if (panel != null)
                panel.NotifyDisarmed();

            // Ouvrir la porte si configurée
            if (doorToOpen != null)
            {
                targetPosition = doorToOpen.position + openOffset;
                isOpening = true;
            }

            // Réactiver les contrôles du joueur
            InteractionManager manager = FindObjectOfType<InteractionManager>();
            if (manager != null)
                manager.EnableControls(true);

            // Fermer le keypad
            Invoke("CloseKeypad", 2f);
        }
        else
        {
            Debug.Log("[Keypad] Code incorrect.");
            currentInput = "";
            UpdateDisplay();
        }
    }
    
    private void CloseKeypad()
    {
        gameObject.SetActive(false);
    }

    void UpdateDisplay()
    {
        if (displayText != null)
        {
            // Format avec tirets pour les chiffres non entrés
            string display = "";
            for (int i = 0; i < correctCode.Length; i++)
            {
                if (i < currentInput.Length)
                    display += currentInput[i];
                else
                    display += "-";
                    
                // Ajouter un espace entre les chiffres
                if (i < correctCode.Length - 1)
                    display += " ";
            }
            
            displayText.text = display;
        }
    }

    void Update()
    {
        // Animation d'ouverture de porte
        if (isOpening && doorToOpen != null)
        {
            doorToOpen.position = Vector3.MoveTowards(doorToOpen.position, targetPosition, openSpeed * Time.deltaTime);
            if (Vector3.Distance(doorToOpen.position, targetPosition) < 0.01f)
                isOpening = false;
        }
        
        // Support du clavier pour entrer le code
        if (Input.GetKeyDown(KeyCode.Backspace))
            PressClear();
            
        // Entrée des chiffres
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                PressKey(i.ToString());
        }
        
        // Touche entrée pour valider
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            PressEnter();
            
        // Échap pour fermer
        if (Input.GetKeyDown(KeyCode.Escape))
            CloseKeypad();
    }
}