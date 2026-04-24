using System;
using System.Collections.Generic;
using System.Reflection;
using Fungus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattlePauseMenuController : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pauseUiRoot;
    [SerializeField] private Transform navigationRoot;
    [SerializeField] private Selectable initialSelection;
    [SerializeField] private UICanvasSelectionFrame selectionFrame;
    [SerializeField] private ControllerUINavigationController navigationController;
    [SerializeField] private bool hidePauseUiRootWhenClosed = true;
    [SerializeField] private bool pauseGameTime = true;
    [SerializeField] private float inputBlockDurationAfterPauseClose = 0.2f;

    [Header("Input")]
    [SerializeField] private bool startButtonTogglesPause = true;
    [SerializeField] private bool eastButtonClosesPause = true;

    public static bool IsPauseMenuOpen { get; private set; }

    private bool previousEastPressed;
    private float previousTimeScale = 1f;
    private float inputBlockedUntilUnscaledTime;
    private readonly List<DialogInput> dialogInputsDisabledByPause = new List<DialogInput>();
    private readonly List<PausedCanvasGroupState> pausedCanvasGroups = new List<PausedCanvasGroupState>();

    private struct PausedCanvasGroupState
    {
        public CanvasGroup group;
        public bool interactable;
        public bool blocksRaycasts;
    }

    private void Awake()
    {
        AutoAssignMissingReferences();
        SetPauseOpen(false, true);
    }

    private void AutoAssignMissingReferences()
    {
        if (pauseUiRoot == null)
            pauseUiRoot = FindChildGameObjectByName(transform.root, "Pause Panel");

        if (navigationRoot == null && pauseUiRoot != null)
            navigationRoot = pauseUiRoot.transform;

        if (initialSelection == null && navigationRoot != null)
            initialSelection = GetFirstUsableSelectable();

        if (selectionFrame == null)
            selectionFrame = FindObjectOfType<UICanvasSelectionFrame>(true);

        if (navigationController == null)
            navigationController = FindObjectOfType<ControllerUINavigationController>(true);
    }

    private GameObject FindChildGameObjectByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
                return children[i].gameObject;
        }

        return null;
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void Update()
    {
        if (!IsPauseMenuOpen &&
            dialogInputsDisabledByPause.Count > 0 &&
            Time.unscaledTime >= inputBlockedUntilUnscaledTime)
        {
            RestoreFungusInteraction();
        }

        if (!IsPauseMenuOpen && Time.unscaledTime < inputBlockedUntilUnscaledTime)
            return;

        if (IsBattleResultPanelOpen())
        {
            if (IsPauseMenuOpen)
                SetPauseOpen(false);

            return;
        }

        HandleGamepadInput();
        HandlePauseInput();
    }

    public void OpenPauseMenu()
    {
        SetPauseOpen(true);
    }

    public void ClosePauseMenu()
    {
        SetPauseOpen(false);
    }

    public void TogglePauseMenu()
    {
        SetPauseOpen(!IsPauseMenuOpen);
    }

    private void HandlePauseInput()
    {
        if (!startButtonTogglesPause)
            return;

        if (Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            ControllerInputModeTracker.NotifyControllerInput();
            TogglePauseMenu();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ControllerInputModeTracker.NotifyMouseKeyboardInput();
            TogglePauseMenu();
        }
    }

    private void HandleGamepadInput()
    {
        bool eastPressed = Input.GetKey(KeyCode.JoystickButton1);
        if (eastButtonClosesPause && IsPauseMenuOpen && eastPressed && !previousEastPressed)
        {
            ControllerInputModeTracker.NotifyControllerInput();
            SetPauseOpen(false);
        }

        previousEastPressed = eastPressed;
    }

    private void SetPauseOpen(bool open, bool instant = false)
    {
        IsPauseMenuOpen = open;

        if (pauseGameTime)
        {
            if (open)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            }
        }

        if (open)
        {
            inputBlockedUntilUnscaledTime = 0f;
            PauseFungusInteraction();
        }
        else
        {
            inputBlockedUntilUnscaledTime = instant
                ? 0f
                : Time.unscaledTime + inputBlockDurationAfterPauseClose;

            if (!instant)
                DialogueAdvanceInputBlocker.BlockForSeconds(inputBlockDurationAfterPauseClose);
        }

        if (pauseUiRoot != null && hidePauseUiRootWhenClosed)
            pauseUiRoot.SetActive(open);

        if (open)
        {
            if (navigationController != null)
                navigationController.RefreshNavigation();

            if (ControllerInputModeTracker.IsControllerMode)
                SelectInitialButton();

            return;
        }

        if (!instant)
            ClearPauseSelection();
    }

    private void PauseFungusInteraction()
    {
        dialogInputsDisabledByPause.Clear();
        pausedCanvasGroups.Clear();

        DialogInput[] dialogInputs = FindObjectsOfType<DialogInput>(true);
        for (int i = 0; i < dialogInputs.Length; i++)
        {
            DialogInput dialogInput = dialogInputs[i];
            if (dialogInput != null && dialogInput.enabled)
            {
                dialogInput.enabled = false;
                dialogInputsDisabledByPause.Add(dialogInput);
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
        for (int i = 0; i < dialogInputsDisabledByPause.Count; i++)
        {
            DialogInput dialogInput = dialogInputsDisabledByPause[i];
            if (dialogInput != null)
                dialogInput.enabled = true;
        }
        dialogInputsDisabledByPause.Clear();

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

    private void SelectInitialButton()
    {
        if (EventSystem.current == null)
            return;

        Selectable selection = IsSelectableUsable(initialSelection)
            ? initialSelection
            : GetFirstUsableSelectable();

        if (selection != null)
            EventSystem.current.SetSelectedGameObject(selection.gameObject);
    }

    private Selectable GetFirstUsableSelectable()
    {
        if (navigationRoot == null)
            return null;

        Selectable[] selectables = navigationRoot.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            if (IsSelectableUsable(selectables[i]))
                return selectables[i];
        }

        return null;
    }

    private bool IsSelectableUsable(Selectable selectable)
    {
        return selectable != null &&
               selectable.gameObject.activeInHierarchy &&
               selectable.IsInteractable();
    }

    private void ClearPauseSelection()
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
            return;

        Transform root = navigationRoot != null
            ? navigationRoot
            : pauseUiRoot != null
                ? pauseUiRoot.transform
                : null;

        if (root != null && selectedObject.transform.IsChildOf(root))
            EventSystem.current.SetSelectedGameObject(null);
    }

    private bool IsBattleResultPanelOpen()
    {
        Type battleStateManagerType = GetOptionalType("BattleStateManager");
        if (battleStateManagerType == null)
            return false;

        object instance = null;
        PropertyInfo instanceProperty = battleStateManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProperty != null)
            instance = instanceProperty.GetValue(null, null);

        if (instance == null)
            return false;

        PropertyInfo battleEndedProperty = battleStateManagerType.GetProperty("BattleEnded", BindingFlags.Public | BindingFlags.Instance);
        if (battleEndedProperty != null && battleEndedProperty.PropertyType == typeof(bool))
            return (bool)battleEndedProperty.GetValue(instance, null);

        FieldInfo battleEndedField = battleStateManagerType.GetField("BattleEnded", BindingFlags.Public | BindingFlags.Instance);
        return battleEndedField != null &&
               battleEndedField.FieldType == typeof(bool) &&
               (bool)battleEndedField.GetValue(instance);
    }

    private static Type GetOptionalType(string typeName)
    {
        Type type = Type.GetType(typeName);
        if (type != null)
            return type;

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }
}
