using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Fungus;

public class UICanvasSelectionFrame : MonoBehaviour
{
    [System.Serializable]
    private class ContextStyleSettings
    {
        public bool useOverrides = true;
        public Vector2 offset = Vector2.zero;
        public Vector2 sizeMultiplier = Vector2.one;
        public Vector2 padding = new Vector2(18f, 14f);
        public bool clampSizeToRoot = true;
        public Vector2 clampRootPadding = new Vector2(4f, 4f);
        public float cornerLength = 36f;
        public float cornerThickness = 7f;
        public bool keepOnTop = true;
        public bool hideWhenNoSelection = true;
        public bool snapInstantly = false;
        public float followSpeed = 18f;
    }

    [Header("References")]
    [SerializeField] private RectTransform frameRoot;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Transform navigationRoot;
    [SerializeField] private ControllerUINavigationController navigationController;
    [SerializeField] private RectTransform sizeClampRoot;
    [SerializeField] private bool useSceneWideFallbackNavigation = true;
    [SerializeField] private Transform pauseContextRoot;
    [SerializeField] private Transform fungusContextRoot;
    [SerializeField] private Transform chooseDateContextRoot;

    [Header("Look")]
    [SerializeField] private Color frameColor = new Color(0.88f, 0.62f, 0.18f, 1f);
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private Vector2 sizeMultiplier = Vector2.one;
    [SerializeField] private Vector2 padding = new Vector2(18f, 14f);
    [SerializeField] private bool clampSizeToRoot = true;
    [SerializeField] private Vector2 clampRootPadding = new Vector2(4f, 4f);
    [SerializeField] private float cornerLength = 36f;
    [SerializeField] private float cornerThickness = 7f;
    [SerializeField] private bool keepOnTop = true;

    [Header("Movement")]
    [SerializeField] private bool hideWhenNoSelection = true;
    [SerializeField] private bool snapInstantly = false;
    [SerializeField] private float followSpeed = 18f;

    [Header("Context Styles")]
    [SerializeField] private ContextStyleSettings pauseStyle = new ContextStyleSettings
    {
        useOverrides = true,
        offset = new Vector2(0.52f, 0f),
        sizeMultiplier = new Vector2(1.35f, 1f),
        padding = new Vector2(15.8f, 14.5f),
        clampSizeToRoot = true,
        clampRootPadding = new Vector2(-14.16f, 4f),
        cornerLength = 36f,
        cornerThickness = 7f,
        keepOnTop = true,
        hideWhenNoSelection = true,
        snapInstantly = false,
        followSpeed = 18f
    };
    [SerializeField] private ContextStyleSettings fungusStyle = new ContextStyleSettings
    {
        useOverrides = true,
        offset = new Vector2(1f, 0f),
        sizeMultiplier = new Vector2(1.38f, 0.88f),
        padding = new Vector2(15.34f, 17.7f),
        clampSizeToRoot = true,
        clampRootPadding = new Vector2(-416.4f, 4f),
        cornerLength = 32f,
        cornerThickness = 7.14f,
        keepOnTop = true,
        hideWhenNoSelection = true,
        snapInstantly = false,
        followSpeed = 18f
    };
    [SerializeField] private ContextStyleSettings chooseDateStyle = new ContextStyleSettings
    {
        useOverrides = true,
        offset = new Vector2(0.52f, 0f),
        sizeMultiplier = new Vector2(1.28f, 1f),
        padding = new Vector2(20f, 18f),
        clampSizeToRoot = true,
        clampRootPadding = new Vector2(12f, 12f),
        cornerLength = 36f,
        cornerThickness = 7f,
        keepOnTop = true,
        hideWhenNoSelection = true,
        snapInstantly = false,
        followSpeed = 18f
    };

    private readonly Vector3[] selectedWorldCorners = new Vector3[4];
    private RectTransform parentRect;
    private Image[] cornerBars;

    private void Awake()
    {
        if (frameRoot == null)
            frameRoot = transform as RectTransform;

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (navigationController == null)
            navigationController = FindObjectOfType<ControllerUINavigationController>(true);

        AutoAssignContextRoots();

        parentRect = frameRoot != null && frameRoot.parent != null
            ? frameRoot.parent as RectTransform
            : null;

        ConfigureFrameRaycasts();
        ConfigureFrameRoot();
        EnsureCornerBars();
        SetFrameVisible(false);
    }

    private void LateUpdate()
    {
        if (!ControllerInputModeTracker.IsControllerMode)
        {
            SetFrameVisible(false);
            return;
        }

        if (EventSystem.current == null || frameRoot == null || parentRect == null)
        {
            SetFrameVisible(false);
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        ContextStyleSettings activeStyle = GetActiveStyle(selectedObject);
        Transform activeContextRoot = GetActiveContextRoot(selectedObject);
        if (!TryGetSelectedRect(selectedObject, activeContextRoot, out RectTransform selectedRect))
        {
            SetFrameVisible(false);
            return;
        }

        MoveToSelectedRect(selectedRect, activeStyle, activeContextRoot as RectTransform);
        ApplyCornerStyle(activeStyle);

        if (activeStyle.keepOnTop)
            frameRoot.SetAsLastSibling();

        SetFrameVisible(true);
    }

    private bool TryGetSelectedRect(GameObject selectedObject, Transform activeContextRoot, out RectTransform selectedRect)
    {
        selectedRect = null;

        if (selectedObject == null || !selectedObject.activeInHierarchy)
            return false;

        Transform effectiveNavigationRoot = navigationRoot != null
            ? navigationRoot
            : targetCanvas != null
                ? targetCanvas.transform
                : null;

        if (activeContextRoot != null)
            effectiveNavigationRoot = activeContextRoot;

        if (navigationController != null)
        {
            Transform controllerRoot = navigationController.GetCurrentNavigationRoot();
            if (controllerRoot != null)
                effectiveNavigationRoot = controllerRoot;
        }

        if (!useSceneWideFallbackNavigation &&
            effectiveNavigationRoot != null &&
            !selectedObject.transform.IsChildOf(effectiveNavigationRoot))
            return false;

        Selectable selectable = selectedObject.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsInteractable())
            return false;

        selectedRect = selectedObject.transform as RectTransform;
        return selectedRect != null;
    }

    private void MoveToSelectedRect(RectTransform selectedRect, ContextStyleSettings activeStyle, RectTransform activeClampRoot)
    {
        selectedRect.GetWorldCorners(selectedWorldCorners);

        Camera canvasCamera = GetCanvasCamera();
        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(canvasCamera, selectedWorldCorners[0]);
        Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(canvasCamera, selectedWorldCorners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, bottomLeftScreen, canvasCamera, out Vector2 localBottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, topRightScreen, canvasCamera, out Vector2 localTopRight);

        Vector2 targetPosition = (localBottomLeft + localTopRight) * 0.5f + activeStyle.offset;
        Vector2 targetSize = new Vector2(
            Mathf.Abs(localTopRight.x - localBottomLeft.x) + activeStyle.padding.x * 2f,
            Mathf.Abs(localTopRight.y - localBottomLeft.y) + activeStyle.padding.y * 2f);
        targetSize = new Vector2(targetSize.x * activeStyle.sizeMultiplier.x, targetSize.y * activeStyle.sizeMultiplier.y);
        targetSize = ClampTargetSizeToRoot(targetSize, activeStyle, activeClampRoot);

        if (activeStyle.snapInstantly)
        {
            frameRoot.anchoredPosition = targetPosition;
        }
        else
        {
            float t = 1f - Mathf.Exp(-activeStyle.followSpeed * Time.unscaledDeltaTime);
            frameRoot.anchoredPosition = Vector2.Lerp(frameRoot.anchoredPosition, targetPosition, t);
        }

        frameRoot.sizeDelta = targetSize;
    }

    private Vector2 ClampTargetSizeToRoot(Vector2 targetSize, ContextStyleSettings activeStyle, RectTransform activeClampRoot)
    {
        if (!activeStyle.clampSizeToRoot)
            return targetSize;

        RectTransform clampRoot = sizeClampRoot != null
            ? sizeClampRoot
            : activeClampRoot != null
                ? activeClampRoot
                : navigationRoot as RectTransform;

        if (clampRoot == null)
            return targetSize;

        Vector2 maxSize = new Vector2(
            Mathf.Max(1f, clampRoot.rect.width - activeStyle.clampRootPadding.x * 2f),
            Mathf.Max(1f, clampRoot.rect.height - activeStyle.clampRootPadding.y * 2f));

        return new Vector2(
            Mathf.Min(targetSize.x, maxSize.x),
            Mathf.Min(targetSize.y, maxSize.y));
    }

    private void ConfigureFrameRoot()
    {
        if (frameRoot == null)
            return;

        frameRoot.anchorMin = new Vector2(0.5f, 0.5f);
        frameRoot.anchorMax = new Vector2(0.5f, 0.5f);
        frameRoot.pivot = new Vector2(0.5f, 0.5f);
    }

    private void ConfigureFrameRaycasts()
    {
        if (frameRoot == null)
            return;

        Graphic[] graphics = frameRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }

    private void EnsureCornerBars()
    {
        if (frameRoot == null)
            return;

        cornerBars = new Image[8];
        CreateCornerBar(0, "TopLeft_H", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        CreateCornerBar(1, "TopLeft_V", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        CreateCornerBar(2, "TopRight_H", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        CreateCornerBar(3, "TopRight_V", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        CreateCornerBar(4, "BottomLeft_H", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        CreateCornerBar(5, "BottomLeft_V", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        CreateCornerBar(6, "BottomRight_H", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        CreateCornerBar(7, "BottomRight_V", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

        ApplyCornerStyle(GetActiveStyle(null));
    }

    private void CreateCornerBar(int index, string childName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Transform existingChild = frameRoot.Find(childName);
        GameObject barObject = existingChild != null
            ? existingChild.gameObject
            : new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existingChild == null)
            barObject.transform.SetParent(frameRoot, false);

        RectTransform rectTransform = barObject.transform as RectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;

        Image image = barObject.GetComponent<Image>();
        image.raycastTarget = false;
        cornerBars[index] = image;
    }

    private void ApplyCornerStyle(ContextStyleSettings activeStyle)
    {
        if (cornerBars == null || cornerBars.Length != 8)
            return;

        for (int i = 0; i < cornerBars.Length; i++)
        {
            if (cornerBars[i] != null)
                cornerBars[i].color = frameColor;
        }

        SetBar(cornerBars[0], new Vector2(activeStyle.cornerLength, activeStyle.cornerThickness));
        SetBar(cornerBars[1], new Vector2(activeStyle.cornerThickness, activeStyle.cornerLength));
        SetBar(cornerBars[2], new Vector2(activeStyle.cornerLength, activeStyle.cornerThickness));
        SetBar(cornerBars[3], new Vector2(activeStyle.cornerThickness, activeStyle.cornerLength));
        SetBar(cornerBars[4], new Vector2(activeStyle.cornerLength, activeStyle.cornerThickness));
        SetBar(cornerBars[5], new Vector2(activeStyle.cornerThickness, activeStyle.cornerLength));
        SetBar(cornerBars[6], new Vector2(activeStyle.cornerLength, activeStyle.cornerThickness));
        SetBar(cornerBars[7], new Vector2(activeStyle.cornerThickness, activeStyle.cornerLength));
    }

    private void SetBar(Image image, Vector2 size)
    {
        if (image == null)
            return;

        RectTransform rectTransform = image.transform as RectTransform;
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private Camera GetCanvasCamera()
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return targetCanvas.worldCamera;
    }

    private void SetFrameVisible(bool visible)
    {
        if (cornerBars == null)
            return;

        for (int i = 0; i < cornerBars.Length; i++)
        {
            if (cornerBars[i] != null && cornerBars[i].enabled != visible)
                cornerBars[i].enabled = visible;
        }
    }

    private void AutoAssignContextRoots()
    {
        if (pauseContextRoot == null)
        {
            GameObject pauseObject = FindChildGameObjectByName(transform.root, "Pause Panel");
            if (pauseObject != null)
                pauseContextRoot = pauseObject.transform;
        }

        if (chooseDateContextRoot == null)
        {
            GameObject chooseDateObject = FindChildGameObjectByName(transform.root, "Person Selection Panel");
            if (chooseDateObject != null)
                chooseDateContextRoot = chooseDateObject.transform;
        }

        if (fungusContextRoot == null && MenuDialog.ActiveMenuDialog != null)
            fungusContextRoot = MenuDialog.ActiveMenuDialog.transform;
    }

    private ContextStyleSettings GetActiveStyle(GameObject selectedObject)
    {
        AutoAssignContextRoots();

        if (IsInContext(selectedObject, chooseDateContextRoot) && chooseDateStyle != null && chooseDateStyle.useOverrides)
            return chooseDateStyle;

        Transform resolvedFungusRoot = GetResolvedFungusContextRoot();
        if (IsInContext(selectedObject, resolvedFungusRoot) && fungusStyle != null && fungusStyle.useOverrides)
            return fungusStyle;

        if (IsInContext(selectedObject, pauseContextRoot) && pauseStyle != null && pauseStyle.useOverrides)
            return pauseStyle;

        return BuildFallbackStyle();
    }

    private Transform GetActiveContextRoot(GameObject selectedObject)
    {
        AutoAssignContextRoots();

        if (IsInContext(selectedObject, chooseDateContextRoot))
            return chooseDateContextRoot;

        Transform resolvedFungusRoot = GetResolvedFungusContextRoot();
        if (IsInContext(selectedObject, resolvedFungusRoot))
            return resolvedFungusRoot;

        if (IsInContext(selectedObject, pauseContextRoot))
            return pauseContextRoot;

        return navigationController != null ? navigationController.GetCurrentNavigationRoot() : navigationRoot;
    }

    private Transform GetResolvedFungusContextRoot()
    {
        if (fungusContextRoot != null)
            return fungusContextRoot;

        if (MenuDialog.ActiveMenuDialog != null)
            fungusContextRoot = MenuDialog.ActiveMenuDialog.transform;

        return fungusContextRoot;
    }

    private bool IsInContext(GameObject selectedObject, Transform contextRoot)
    {
        return selectedObject != null &&
               contextRoot != null &&
               selectedObject.transform.IsChildOf(contextRoot);
    }

    private ContextStyleSettings BuildFallbackStyle()
    {
        return new ContextStyleSettings
        {
            useOverrides = true,
            offset = offset,
            sizeMultiplier = sizeMultiplier,
            padding = padding,
            clampSizeToRoot = clampSizeToRoot,
            clampRootPadding = clampRootPadding,
            cornerLength = cornerLength,
            cornerThickness = cornerThickness,
            keepOnTop = keepOnTop,
            hideWhenNoSelection = hideWhenNoSelection,
            snapInstantly = snapInstantly,
            followSpeed = followSpeed
        };
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
}
