using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ControllerUINavigationController : MonoBehaviour
{
    [System.Serializable]
    private class PanelNavigationTarget
    {
        public Transform panelRoot;
        public Selectable initialSelection;
    }

    [Header("Selection")]
    [SerializeField] private Selectable initialSelection;
    [SerializeField] private Transform navigationRoot;
    [SerializeField] private List<PanelNavigationTarget> panelNavigationTargets = new List<PanelNavigationTarget>();
    [SerializeField] private bool selectFirstOnEnable = true;
    [SerializeField] private bool keepSelectionAlive = true;
    [SerializeField] private float selectionRepairDelay = 0.05f;

    [Header("Unity Navigation")]
    [SerializeField] private bool autoConfigureNavigation = true;
    [SerializeField] private bool useExplicitPositionNavigation = false;
    [SerializeField] private float explicitNavigationForwardWeight = 0.25f;
    [SerializeField] private bool useSceneWideFallbackNavigation = true;

    private float nextRepairTime;
    private Transform lastActiveNavigationRoot;

    private void OnEnable()
    {
        if (!ControllerInputModeTracker.IsControllerMode)
        {
            ClearOwnedSelection();
            lastActiveNavigationRoot = null;
            return;
        }

        RefreshNavigation();

        if (selectFirstOnEnable)
            SelectInitialOrFirstAvailable();
    }

    private void Start()
    {
        if (!ControllerInputModeTracker.IsControllerMode)
            return;

        RefreshNavigation();

        if (EventSystem.current == null)
            return;

        if (EventSystem.current.currentSelectedGameObject == null)
            SelectInitialOrFirstAvailable();
    }

    private void Update()
    {
        if (!keepSelectionAlive || EventSystem.current == null || Time.unscaledTime < nextRepairTime)
            return;

        if (!ControllerInputModeTracker.IsControllerMode)
        {
            ClearOwnedSelection();
            lastActiveNavigationRoot = null;
            return;
        }

        Transform activeRoot = GetActiveNavigationRoot();
        if (activeRoot != lastActiveNavigationRoot)
        {
            lastActiveNavigationRoot = activeRoot;
            RefreshNavigation();
            SelectInitialOrFirstAvailable();
            nextRepairTime = Time.unscaledTime + selectionRepairDelay;
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (IsValidSelection(selectedObject))
            return;

        SelectInitialOrFirstAvailable();
        nextRepairTime = Time.unscaledTime + selectionRepairDelay;
    }

    public void Select(Selectable selectable)
    {
        if (!IsSelectableUsable(selectable) || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    public void SelectFirstAvailable()
    {
        Select(GetFirstAvailableSelectable());
    }

    public Transform GetCurrentNavigationRoot()
    {
        return GetActiveNavigationRoot();
    }

    public void RefreshNavigation()
    {
        if (!autoConfigureNavigation)
            return;

        List<Selectable> selectables = GetAllConfiguredSelectables();
        if (useExplicitPositionNavigation)
        {
            ConfigureExplicitPositionNavigation(GetUsableSelectables());
            return;
        }

        for (int i = 0; i < selectables.Count; i++)
        {
            Navigation navigation = selectables[i].navigation;
            navigation.mode = Navigation.Mode.Automatic;
            selectables[i].navigation = navigation;
        }
    }

    private void ConfigureExplicitPositionNavigation(List<Selectable> selectables)
    {
        List<Selectable> usableSelectables = new List<Selectable>();
        for (int i = 0; i < selectables.Count; i++)
        {
            if (IsSelectableUsable(selectables[i]))
                usableSelectables.Add(selectables[i]);
        }

        for (int i = 0; i < usableSelectables.Count; i++)
        {
            Selectable selectable = usableSelectables[i];
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = FindBestDirectionalSelectable(selectable, usableSelectables, Vector2.up);
            navigation.selectOnDown = FindBestDirectionalSelectable(selectable, usableSelectables, Vector2.down);
            navigation.selectOnLeft = FindBestDirectionalSelectable(selectable, usableSelectables, Vector2.left);
            navigation.selectOnRight = FindBestDirectionalSelectable(selectable, usableSelectables, Vector2.right);
            selectable.navigation = navigation;
        }
    }

    private Selectable FindBestDirectionalSelectable(Selectable source, List<Selectable> candidates, Vector2 direction)
    {
        RectTransform sourceRect = source != null ? source.transform as RectTransform : null;
        if (sourceRect == null)
            return null;

        Vector2 sourcePosition = sourceRect.position;
        Selectable bestSelectable = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Selectable candidate = candidates[i];
            RectTransform candidateRect = candidate != null ? candidate.transform as RectTransform : null;
            if (candidate == source || candidateRect == null)
                continue;

            Vector2 offset = (Vector2)candidateRect.position - sourcePosition;
            float forwardDistance = Vector2.Dot(offset, direction);
            if (forwardDistance <= 0.01f)
                continue;

            Vector2 perpendicular = offset - direction * forwardDistance;
            float score = perpendicular.sqrMagnitude + forwardDistance * explicitNavigationForwardWeight;
            if (score < bestScore)
            {
                bestScore = score;
                bestSelectable = candidate;
            }
        }

        return bestSelectable;
    }

    private void SelectInitialOrFirstAvailable()
    {
        Selectable activeInitialSelection = GetActiveInitialSelection();
        Select(IsSelectableUsable(activeInitialSelection) ? activeInitialSelection : GetFirstAvailableSelectable());
    }

    private Selectable GetFirstAvailableSelectable()
    {
        List<Selectable> selectables = GetUsableSelectables();
        return selectables.Count > 0 ? selectables[0] : null;
    }

    private List<Selectable> GetUsableSelectables()
    {
        List<Selectable> usableSelectables = new List<Selectable>();
        Transform activeRoot = GetActiveNavigationRoot();
        if (activeRoot == null)
            return usableSelectables;

        Selectable[] allSelectables = activeRoot.GetComponentsInChildren<Selectable>(true);

        for (int i = 0; i < allSelectables.Length; i++)
        {
            if (IsSelectableUsable(allSelectables[i]))
                usableSelectables.Add(allSelectables[i]);
        }

        usableSelectables.Sort(CompareSelectablesForMenuNavigation);
        return usableSelectables;
    }

    private bool IsValidSelection(GameObject selectedObject)
    {
        if (selectedObject == null || !selectedObject.activeInHierarchy)
            return false;

        Transform activeRoot = GetActiveNavigationRoot();
        if (activeRoot != null && !selectedObject.transform.IsChildOf(activeRoot))
            return false;

        Selectable selectable = selectedObject.GetComponent<Selectable>();
        return IsSelectableUsable(selectable);
    }

    private Transform GetActiveNavigationRoot()
    {
        PanelNavigationTarget activeTarget = GetActivePanelTarget();
        if (activeTarget != null && activeTarget.panelRoot != null)
            return activeTarget.panelRoot;

        if (navigationRoot != null && navigationRoot.gameObject.activeInHierarchy)
            return navigationRoot;

        return useSceneWideFallbackNavigation
            ? GetFallbackNavigationRoot()
            : null;
    }

    private Selectable GetActiveInitialSelection()
    {
        PanelNavigationTarget activeTarget = GetActivePanelTarget();
        return activeTarget != null && activeTarget.initialSelection != null
            ? activeTarget.initialSelection
            : initialSelection;
    }

    private PanelNavigationTarget GetActivePanelTarget()
    {
        for (int i = 0; i < panelNavigationTargets.Count; i++)
        {
            PanelNavigationTarget target = panelNavigationTargets[i];
            if (target != null && target.panelRoot != null && target.panelRoot.gameObject.activeInHierarchy)
                return target;
        }

        return null;
    }

    private List<Selectable> GetAllConfiguredSelectables()
    {
        List<Selectable> selectables = new List<Selectable>();
        AddUsableSelectablesFromRoot(navigationRoot, selectables);

        for (int i = 0; i < panelNavigationTargets.Count; i++)
        {
            PanelNavigationTarget target = panelNavigationTargets[i];
            if (target != null)
                AddUsableSelectablesFromRoot(target.panelRoot, selectables);
        }

        if (selectables.Count == 0)
            selectables.AddRange(GetUsableSelectables());

        return selectables;
    }

    private void AddUsableSelectablesFromRoot(Transform root, List<Selectable> selectables)
    {
        if (root == null)
            return;

        Selectable[] rootSelectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < rootSelectables.Length; i++)
        {
            Selectable selectable = rootSelectables[i];
            if (selectable != null && !selectables.Contains(selectable))
                selectables.Add(selectable);
        }
    }

    private bool IsSelectableUsable(Selectable selectable)
    {
        return selectable != null &&
               selectable.gameObject.activeInHierarchy &&
               selectable.IsInteractable();
    }

    private Transform GetFallbackNavigationRoot()
    {
        if (EventSystem.current != null)
        {
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject != null && selectedObject.activeInHierarchy)
            {
                Transform selectedRoot = GetSelectableRoot(selectedObject.transform);
                if (selectedRoot != null)
                    return selectedRoot;
            }
        }

        Selectable[] allSelectables = Selectable.allSelectablesArray;
        for (int i = 0; i < allSelectables.Length; i++)
        {
            if (!IsSelectableUsable(allSelectables[i]))
                continue;

            Transform selectableRoot = GetSelectableRoot(allSelectables[i].transform);
            if (selectableRoot != null)
                return selectableRoot;
        }

        return null;
    }

    private static Transform GetSelectableRoot(Transform selectableTransform)
    {
        if (selectableTransform == null)
            return null;

        Canvas parentCanvas = selectableTransform.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            return parentCanvas.transform;

        return selectableTransform.root;
    }

    private int CompareSelectablesForMenuNavigation(Selectable a, Selectable b)
    {
        if (a == b)
            return 0;

        RectTransform rectA = a.transform as RectTransform;
        RectTransform rectB = b.transform as RectTransform;
        if (rectA != null && rectB != null)
        {
            Vector3 positionA = rectA.position;
            Vector3 positionB = rectB.position;

            int verticalCompare = -positionA.y.CompareTo(positionB.y);
            if (verticalCompare != 0)
                return verticalCompare;

            int horizontalCompare = positionA.x.CompareTo(positionB.x);
            if (horizontalCompare != 0)
                return horizontalCompare;
        }

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    private void ClearOwnedSelection()
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
            return;

        Transform activeRoot = GetActiveNavigationRoot();
        if (activeRoot != null && selectedObject.transform.IsChildOf(activeRoot))
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        for (int i = 0; i < panelNavigationTargets.Count; i++)
        {
            PanelNavigationTarget target = panelNavigationTargets[i];
            if (target != null &&
                target.panelRoot != null &&
                selectedObject.transform.IsChildOf(target.panelRoot))
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }
        }
    }

}

public class ControllerInputModeTracker : MonoBehaviour
{
    [SerializeField] private float gamepadStickDeadzone = 0.35f;
    [SerializeField] private float mouseMoveDeadzone = 0.01f;

    public static bool IsControllerMode { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateTracker()
    {
        if (FindObjectOfType<ControllerInputModeTracker>() != null)
            return;

        GameObject trackerObject = new GameObject(nameof(ControllerInputModeTracker));
        DontDestroyOnLoad(trackerObject);
        trackerObject.AddComponent<ControllerInputModeTracker>();
    }

    public static void NotifyControllerInput()
    {
        IsControllerMode = true;
    }

    public static void NotifyMouseKeyboardInput()
    {
        IsControllerMode = false;
    }

    private void Update()
    {
        if (HasMouseInputThisFrame() || HasKeyboardInputThisFrame())
        {
            IsControllerMode = false;
            return;
        }

        if (HasGamepadInputThisFrame())
            IsControllerMode = true;
    }

    private bool HasMouseInputThisFrame()
    {
        if (Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1) ||
            Input.GetMouseButtonDown(2))
            return true;

        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f)
            return true;

        return Mathf.Abs(GetAxisRaw("Mouse X")) > mouseMoveDeadzone ||
               Mathf.Abs(GetAxisRaw("Mouse Y")) > mouseMoveDeadzone;
    }

    private bool HasKeyboardInputThisFrame()
    {
        return Input.anyKeyDown && !HasGamepadButtonInputThisFrame();
    }

    private bool HasGamepadInputThisFrame()
    {
        return HasGamepadButtonInputThisFrame() ||
               Mathf.Abs(GetAxisRaw("Horizontal")) > gamepadStickDeadzone ||
               Mathf.Abs(GetAxisRaw("Vertical")) > gamepadStickDeadzone;
    }

    private bool HasGamepadButtonInputThisFrame()
    {
        for (int buttonIndex = 0; buttonIndex <= 19; buttonIndex++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + buttonIndex)))
                return true;
        }

        return false;
    }

    private static float GetAxisRaw(string axisName)
    {
        try
        {
            return Input.GetAxisRaw(axisName);
        }
        catch (UnityException)
        {
            return 0f;
        }
    }
}
