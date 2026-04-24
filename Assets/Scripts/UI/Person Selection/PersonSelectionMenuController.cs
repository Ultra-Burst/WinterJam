using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Fungus;
using UnityEngine.SceneManagement;

public class PersonSelectionMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private PersonSelectionCardUI cardTemplate;
    [SerializeField] private TMP_Text emptyStateLabel;
    [SerializeField] private Transform peopleRoot;
    [SerializeField] private List<Person> people = new List<Person>();
    [SerializeField] private Flowchart lastFlowchart;
    [SerializeField] private string lastFlowchartBlockName = "Start";
    [SerializeField] private string lastFlowchartResultBoolName = "PlayerWon";
    [SerializeField] private string winSceneName = "YouWinScene";
    [SerializeField] private string loseSceneName = "YouLoseScene";

    [Header("Navigation")]
    [SerializeField] private ControllerUINavigationController navigationController;
    [SerializeField] private bool showMenuOnStart = true;
    [SerializeField] private bool refreshCardsOnShow = true;
    [SerializeField] private float inputBlockDurationAfterMenuClose = 0.2f;
    [SerializeField] private float conversationCompletionDelay = 1f;

    private readonly List<PersonSelectionCardUI> spawnedCards = new List<PersonSelectionCardUI>();
    private readonly List<DialogInput> dialogInputsDisabledByMenu = new List<DialogInput>();
    private readonly List<PausedCanvasGroupState> pausedCanvasGroups = new List<PausedCanvasGroupState>();
    private readonly HashSet<Person> completedPeople = new HashSet<Person>();
    private float inputBlockedUntilUnscaledTime;
    private Person currentConversationPerson;
    private Flowchart activeConversationFlowchart;
    private int activeConversationStartFrame = -1;
    private bool activeConversationWasObserved;
    private float activeConversationInactiveSince = -1f;
    private bool finalSequenceStarted;
    private bool waitingForLastFlowchart;
    private int lastFlowchartStartFrame = -1;
    private bool lastFlowchartWasObserved;
    private float lastFlowchartInactiveSince = -1f;
    private bool pendingConversationFinishedByFungus;
    private float pendingConversationFinishedAt = -1f;

    private struct PausedCanvasGroupState
    {
        public CanvasGroup group;
        public bool interactable;
        public bool blocksRaycasts;
    }

    private void Awake()
    {
        AutoAssignMissingReferences();

        if (cardTemplate != null)
            cardTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        DialogueAdvanceInputBlocker.Clear();
        ResetSelectionProgress();
        RefreshCards();

        if (showMenuOnStart)
            ShowSelectionMenu();
        else
            HideSelectionMenu();
    }

    private void Update()
    {
        if ((menuRoot == null || !menuRoot.activeSelf) &&
            dialogInputsDisabledByMenu.Count > 0 &&
            Time.unscaledTime >= inputBlockedUntilUnscaledTime)
        {
            RestoreFungusInteraction();
        }

        TickConversationCompletion();
        TickLastFlowchartCompletion();
        TickPendingConversationFinishedByFungus();
    }

    private void OnDisable()
    {
        RestoreFungusInteraction();
    }

    public void ShowSelectionMenu()
    {
        AutoAssignMissingReferences();

        if (finalSequenceStarted)
            return;

        if (GetAvailablePeopleCount() == 0)
        {
            StartLastFlowchartSequence();
            return;
        }

        if (refreshCardsOnShow)
            RefreshCards();

        if (menuRoot != null)
            menuRoot.SetActive(true);

        inputBlockedUntilUnscaledTime = 0f;
        PauseFungusInteraction();

        if (navigationController != null)
            navigationController.RefreshNavigation();

        if (ControllerInputModeTracker.IsControllerMode && navigationController != null)
        {
            navigationController.SelectFirstAvailable();
        }
        else if (!ControllerInputModeTracker.IsControllerMode && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void HideSelectionMenu()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);

        inputBlockedUntilUnscaledTime = Time.unscaledTime + inputBlockDurationAfterMenuClose;
        DialogueAdvanceInputBlocker.BlockForSeconds(inputBlockDurationAfterMenuClose);
        RestoreFungusInteraction();
    }

    public void ToggleSelectionMenu()
    {
        if (menuRoot == null)
            return;

        if (menuRoot.activeSelf)
            HideSelectionMenu();
        else
            ShowSelectionMenu();
    }

    public void RefreshCards()
    {
        AutoAssignMissingReferences();

        if (cardContainer == null || cardTemplate == null)
            return;

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i].gameObject);
        }
        spawnedCards.Clear();

        List<Person> people = GetPeopleInScene();
        for (int i = 0; i < people.Count; i++)
        {
            PersonSelectionCardUI card = Instantiate(cardTemplate, cardContainer);
            card.name = people[i].DisplayName + " Card";
            card.gameObject.SetActive(true);
            card.Bind(people[i], this);
            spawnedCards.Add(card);
        }

        if (emptyStateLabel != null)
            emptyStateLabel.gameObject.SetActive(people.Count == 0);
    }

    public void SelectPerson(Person person)
    {
        if (person == null || completedPeople.Contains(person))
            return;

        int remainingMatchesAfterThisDate = Mathf.Max(0, GetAvailablePeopleCount() - 1);
        currentConversationPerson = person;
        activeConversationFlowchart = person.flowchart;
        activeConversationStartFrame = Time.frameCount;
        activeConversationWasObserved = false;
        activeConversationInactiveSince = -1f;
        pendingConversationFinishedByFungus = false;
        pendingConversationFinishedAt = -1f;
        ResetMenuDialogState();
        HideSelectionMenu();
        person.StartConversation(remainingMatchesAfterThisDate);
    }

    public void ResetSelectionProgress()
    {
        completedPeople.Clear();
        currentConversationPerson = null;
        activeConversationFlowchart = null;
        activeConversationStartFrame = -1;
        activeConversationWasObserved = false;
        activeConversationInactiveSince = -1f;
        finalSequenceStarted = false;
        waitingForLastFlowchart = false;
        lastFlowchartStartFrame = -1;
        lastFlowchartWasObserved = false;
        lastFlowchartInactiveSince = -1f;
        pendingConversationFinishedByFungus = false;
        pendingConversationFinishedAt = -1f;
    }

    public void CompleteConversationFromFungus()
    {
        pendingConversationFinishedByFungus = true;
        pendingConversationFinishedAt = Time.unscaledTime + 0.05f;
    }

    private List<Person> GetPeopleInScene()
    {
        List<Person> resolvedPeople = new List<Person>();

        for (int i = 0; i < people.Count; i++)
        {
            if (people[i] != null && !completedPeople.Contains(people[i]))
                resolvedPeople.Add(people[i]);
        }

        if (resolvedPeople.Count > 0)
        {
            resolvedPeople.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase));
            return resolvedPeople;
        }

        if (peopleRoot != null)
        {
            Person[] scopedPeople = peopleRoot.GetComponentsInChildren<Person>(true);
            for (int i = 0; i < scopedPeople.Length; i++)
            {
                if (scopedPeople[i] != null && !completedPeople.Contains(scopedPeople[i]))
                    resolvedPeople.Add(scopedPeople[i]);
            }
        }
        else
        {
            Person[] allPeople = FindObjectsOfType<Person>(true);
            for (int i = 0; i < allPeople.Length; i++)
            {
                Person person = allPeople[i];
                if (person == null)
                    continue;

                if (person.gameObject.scene != gameObject.scene)
                    continue;

                if (completedPeople.Contains(person))
                    continue;

                resolvedPeople.Add(person);
            }
        }

        resolvedPeople.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase));
        return resolvedPeople;
    }

    private void AutoAssignMissingReferences()
    {
        if (menuRoot == null)
            menuRoot = FindChildGameObjectByName(transform.root, "Person Selection Panel");

        if (cardContainer == null && menuRoot != null)
        {
            Transform container = menuRoot.transform.Find("Frame/Card List");
            if (container != null)
                cardContainer = container;
        }

        if (cardTemplate == null && cardContainer != null)
        {
            Transform template = cardContainer.Find("Person Card Template");
            if (template != null)
                cardTemplate = template.GetComponent<PersonSelectionCardUI>();
        }

        if (emptyStateLabel == null && menuRoot != null)
        {
            Transform label = menuRoot.transform.Find("Frame/Empty State");
            if (label != null)
                emptyStateLabel = label.GetComponent<TMP_Text>();
        }

        if (peopleRoot == null)
            peopleRoot = FindChildTransformByName(transform.root, "People");

        if (navigationController == null)
            navigationController = FindObjectOfType<ControllerUINavigationController>(true);
    }

    private void PauseFungusInteraction()
    {
        dialogInputsDisabledByMenu.Clear();
        pausedCanvasGroups.Clear();

        DialogInput[] dialogInputs = FindObjectsOfType<DialogInput>(true);
        for (int i = 0; i < dialogInputs.Length; i++)
        {
            DialogInput dialogInput = dialogInputs[i];
            if (dialogInput != null && dialogInput.enabled)
            {
                dialogInput.enabled = false;
                dialogInputsDisabledByMenu.Add(dialogInput);
            }
        }

        PauseCanvasGroup(MenuDialog.ActiveMenuDialog);
        PauseCanvasGroup(SayDialog.ActiveSayDialog);
    }

    private void PauseCanvasGroup(Component component)
    {
        if (component == null)
            return;

        CanvasGroup canvasGroup = component.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            return;

        for (int i = 0; i < pausedCanvasGroups.Count; i++)
        {
            if (pausedCanvasGroups[i].group == canvasGroup)
                return;
        }

        pausedCanvasGroups.Add(new PausedCanvasGroupState
        {
            group = canvasGroup,
            interactable = canvasGroup.interactable,
            blocksRaycasts = canvasGroup.blocksRaycasts
        });

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void RestoreFungusInteraction()
    {
        for (int i = 0; i < dialogInputsDisabledByMenu.Count; i++)
        {
            DialogInput dialogInput = dialogInputsDisabledByMenu[i];
            if (dialogInput != null)
                dialogInput.enabled = true;
        }
        dialogInputsDisabledByMenu.Clear();

        for (int i = 0; i < pausedCanvasGroups.Count; i++)
        {
            PausedCanvasGroupState state = pausedCanvasGroups[i];
            if (state.group == null)
                continue;

            state.group.interactable = state.interactable;
            state.group.blocksRaycasts = state.blocksRaycasts;
        }
        pausedCanvasGroups.Clear();
    }

    private void HandleConversationFinished()
    {
        if (finalSequenceStarted)
            return;

        CompleteCurrentConversationPerson();

        if (GetAvailablePeopleCount() == 0)
        {
            StartLastFlowchartSequence();
            return;
        }

        ShowSelectionMenu();
    }

    private int GetAvailablePeopleCount()
    {
        return GetPeopleInScene().Count;
    }

    private void StartLastFlowchartSequence()
    {
        if (finalSequenceStarted)
            return;

        finalSequenceStarted = true;
        ResetMenuDialogState();
        HideSelectionMenu();

        if (lastFlowchart == null)
        {
            LoadResultScene(true);
            return;
        }

        lastFlowchart.Reset(true, true);
        lastFlowchart.SetIntegerVariable("RemainingMatches", 0);
        waitingForLastFlowchart = true;
        lastFlowchartStartFrame = Time.frameCount;
        lastFlowchartWasObserved = false;
        lastFlowchartInactiveSince = -1f;

        bool started = !string.IsNullOrWhiteSpace(lastFlowchartBlockName)
            ? lastFlowchart.ExecuteIfHasBlock(lastFlowchartBlockName)
            : lastFlowchart.ExecuteIfHasBlock("Start");

        if (!started && !lastFlowchart.ExecuteIfHasBlock("Start"))
        {
            waitingForLastFlowchart = false;
            LoadResultScene(true);
        }
    }

    private void LoadResultSceneFromLastFlowchart()
    {
        bool playerWon = true;

        if (lastFlowchart != null && !string.IsNullOrWhiteSpace(lastFlowchartResultBoolName))
        {
            BooleanVariable resultVariable = lastFlowchart.GetVariable(lastFlowchartResultBoolName) as BooleanVariable;
            if (resultVariable != null)
                playerWon = resultVariable.Value;
        }

        LoadResultScene(playerWon);
    }

    private void LoadResultScene(bool playerWon)
    {
        Time.timeScale = 1f;
        DialogueAdvanceInputBlocker.Clear();

        string sceneName = playerWon ? winSceneName : loseSceneName;
        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = "YouWinScene";

        SceneManager.LoadScene(sceneName);
    }

    private void TickConversationCompletion()
    {
        if (activeConversationFlowchart == null)
            return;

        if (Time.timeScale <= 0f)
            return;

        bool conversationUiActive = IsConversationPresentationVisible();
        bool flowchartStillExecuting = activeConversationFlowchart.HasExecutingBlocks();
        if (conversationUiActive)
        {
            activeConversationWasObserved = true;
            activeConversationInactiveSince = -1f;
            return;
        }

        if (flowchartStillExecuting)
        {
            activeConversationWasObserved = true;
            activeConversationInactiveSince = -1f;
            return;
        }

        if (!activeConversationWasObserved || Time.frameCount <= activeConversationStartFrame)
            return;

        if (activeConversationInactiveSince < 0f)
            activeConversationInactiveSince = Time.unscaledTime;

        if (Time.unscaledTime - activeConversationInactiveSince < conversationCompletionDelay)
            return;

        activeConversationFlowchart = null;
        activeConversationStartFrame = -1;
        activeConversationWasObserved = false;
        activeConversationInactiveSince = -1f;
        HandleConversationFinished();
    }

    private void TickLastFlowchartCompletion()
    {
        if (!waitingForLastFlowchart || lastFlowchart == null)
            return;

        if (Time.timeScale <= 0f)
            return;

        bool conversationUiActive = IsConversationPresentationVisible();
        bool flowchartStillExecuting = lastFlowchart.HasExecutingBlocks();
        if (conversationUiActive)
        {
            lastFlowchartWasObserved = true;
            lastFlowchartInactiveSince = -1f;
            return;
        }

        if (flowchartStillExecuting)
        {
            lastFlowchartWasObserved = true;
            lastFlowchartInactiveSince = -1f;
            return;
        }

        if (!lastFlowchartWasObserved || Time.frameCount <= lastFlowchartStartFrame)
            return;

        if (lastFlowchartInactiveSince < 0f)
            lastFlowchartInactiveSince = Time.unscaledTime;

        if (Time.unscaledTime - lastFlowchartInactiveSince < conversationCompletionDelay)
            return;

        waitingForLastFlowchart = false;
        lastFlowchartStartFrame = -1;
        lastFlowchartWasObserved = false;
        lastFlowchartInactiveSince = -1f;
        LoadResultSceneFromLastFlowchart();
    }

    private void TickPendingConversationFinishedByFungus()
    {
        if (!pendingConversationFinishedByFungus)
            return;

        if (Time.unscaledTime < pendingConversationFinishedAt)
            return;

        if (IsConversationPresentationVisible())
            return;

        pendingConversationFinishedByFungus = false;
        pendingConversationFinishedAt = -1f;
        activeConversationFlowchart = null;
        activeConversationStartFrame = -1;
        activeConversationWasObserved = false;
        activeConversationInactiveSince = -1f;
        DialogueAdvanceInputBlocker.BlockForSeconds(inputBlockDurationAfterMenuClose);
        HandleConversationFinished();
    }

    private void CompleteCurrentConversationPerson()
    {
        if (currentConversationPerson == null)
            return;

        completedPeople.Add(currentConversationPerson);
        currentConversationPerson = null;
    }

    private static bool IsConversationPresentationVisible()
    {
        return IsSayDialogVisible() || AreAnyMenuOptionsVisible();
    }

    private static bool IsSayDialogVisible()
    {
        SayDialog sayDialog = SayDialog.ActiveSayDialog;
        if (sayDialog == null || !sayDialog.gameObject.activeInHierarchy)
            return false;

        CanvasGroup canvasGroup = sayDialog.GetComponent<CanvasGroup>();
        return canvasGroup == null || canvasGroup.alpha > 0.001f;
    }

    private static bool AreAnyMenuOptionsVisible()
    {
        MenuDialog menuDialog = MenuDialog.ActiveMenuDialog;
        if (menuDialog == null || !menuDialog.gameObject.activeInHierarchy)
            return false;

        Button[] buttons = menuDialog.CachedButtons;
        if (buttons == null || buttons.Length == 0)
            return false;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            if (!button.gameObject.activeInHierarchy)
                continue;

            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha <= 0.001f)
                continue;

            return true;
        }

        return false;
    }

    private static void ResetMenuDialogState()
    {
        MenuDialog menuDialog = MenuDialog.ActiveMenuDialog;
        if (menuDialog == null)
            return;

        menuDialog.Clear();
        menuDialog.SetActive(false);
    }

    private GameObject FindChildGameObjectByName(Transform root, string childName)
    {
        Transform child = FindChildTransformByName(root, childName);
        return child != null ? child.gameObject : null;
    }

    private Transform FindChildTransformByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
                return children[i];
        }

        return null;
    }
}
