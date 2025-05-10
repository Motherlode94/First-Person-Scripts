using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text speakerNameText;
    public Image speakerPortrait;
    public Button yesButton;
    public Button noButton;
    public TMP_Text yesButtonText;
    public TMP_Text noButtonText;

    [Header("Typing Effect")]
    public bool useTypewriterEffect = true;
    public float typingSpeed = 0.05f;
    public AudioClip typingSoundEffect;
    public int charactersPerSound = 2;
    public bool skipOnInput = true;

    [Header("Animation")]
    public Animator panelAnimator;
    public string showAnimTrigger = "Show";
    public string hideAnimTrigger = "Hide";

    public UnityEvent OnDialogueStart;
    public UnityEvent OnDialogueEnd;
    public UnityEvent<int> OnLineShown;
    public UnityEvent<bool> OnChoiceMade;

    private DialogueTrigger currentTrigger;
    private string[] lines;
    private int index;
    private bool waitingForChoice = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        dialoguePanel.SetActive(false);

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(false);
            yesButton.onClick.AddListener(OnYes);
        }

        if (noButton != null)
        {
            noButton.gameObject.SetActive(false);
            noButton.onClick.AddListener(OnNo);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
    }

    public void StartDialogue(DialogueTrigger trigger)
    {
        currentTrigger = trigger;
        lines = trigger.dialogueLines;
        index = 0;

        dialoguePanel.SetActive(true);

        if (panelAnimator != null)
            panelAnimator.SetTrigger(showAnimTrigger);

        ShowLine();
        OnDialogueStart?.Invoke();
    }

    void ShowLine()
    {
        if (index < lines.Length)
        {
            SetChoiceButtonsActive(false);
            string currentLine = lines[index];

            if (useTypewriterEffect)
            {
                if (typingCoroutine != null)
                    StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(currentLine));
            }
            else
            {
                dialogueText.text = currentLine;
                CheckForChoice();
            }

            OnLineShown?.Invoke(index);
        }
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        int charCount = 0;

        foreach (char c in text.ToCharArray())
        {
            dialogueText.text += c;
            charCount++;

            if (typingSoundEffect != null && charCount % charactersPerSound == 0)
            {
                audioSource.PlayOneShot(typingSoundEffect);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        CheckForChoice();
    }

    void CheckForChoice()
    {
        if (index == currentTrigger.choiceIndex)
        {
            waitingForChoice = true;
            SetChoiceButtonsActive(true);

            if (yesButtonText != null)
                yesButtonText.text = "Accepter";
            if (noButtonText != null)
                noButtonText.text = "Refuser";
        }
    }

    public void NextLine()
    {
        if (isTyping && skipOnInput)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            dialogueText.text = lines[index];
            isTyping = false;
            CheckForChoice();
            return;
        }

        if (waitingForChoice) return;

        index++;
        if (index < lines.Length)
        {
            ShowLine();
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger(hideAnimTrigger);
            StartCoroutine(CloseAfterAnimation());
        }
        else
        {
            CompleteDialogueClose();
        }
    }

    IEnumerator CloseAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        CompleteDialogueClose();
    }

    void CompleteDialogueClose()
    {
        dialoguePanel.SetActive(false);
        SetChoiceButtonsActive(false);
        OnDialogueEnd?.Invoke();
    }

    void Update()
    {
        if (dialoguePanel.activeSelf && !waitingForChoice)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                NextLine();
            }
        }
    }

    public void OnYes()
    {
        waitingForChoice = false;
        SetChoiceButtonsActive(false);

        if (!string.IsNullOrEmpty(currentTrigger.missionID))
        {
            Mission m = MissionManager.Instance?.allMissions.Find(m => m.missionID == currentTrigger.missionID);
            if (m != null)
                MissionManager.Instance?.ActivateMission(m);
        }

        OnChoiceMade?.Invoke(true);
        NextLine();
    }

    void OnNo()
    {
        waitingForChoice = false;
        OnChoiceMade?.Invoke(false);
        EndDialogue();
    }

    private void SetChoiceButtonsActive(bool active)
    {
        if (yesButton != null)
            yesButton.gameObject.SetActive(active);
        if (noButton != null)
            noButton.gameObject.SetActive(active);
    }

    public bool IsDialogueActive()
    {
        return dialoguePanel.activeSelf;
    }

    public void SetCustomChoiceTexts(string yesText, string noText)
    {
        if (yesButtonText != null && !string.IsNullOrEmpty(yesText))
            yesButtonText.text = yesText;
        if (noButtonText != null && !string.IsNullOrEmpty(noText))
            noButtonText.text = noText;
    }

    public void SetSpeakerInfo(string name, Sprite portrait = null)
    {
        if (speakerNameText != null && !string.IsNullOrEmpty(name))
            speakerNameText.text = name;
        if (speakerPortrait != null && portrait != null)
            speakerPortrait.sprite = portrait;
    }
}