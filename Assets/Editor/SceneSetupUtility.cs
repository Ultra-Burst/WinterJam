using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Fungus;

public static class SceneSetupUtility
{
    private const string HomeScenePath = "Assets/Scenes/StartScene.unity";
    private const string GameScenePath = "Assets/Scenes/MainScene.unity";
    private const string TestScenePath = "Assets/Scenes/TEST.unity";
    private const string WinScenePath = "Assets/Scenes/YouWinScene.unity";
    private const string LoseScenePath = "Assets/Scenes/YouLoseScene.unity";
    private const string PersonPrefabPath = "Assets/Prefabs/Person.prefab";
    private const string FlowchartPrefabPath = "Assets/Fungus/Resources/Prefabs/Flowchart.prefab";
    private const string SayDialogPrefabPath = "Assets/Fungus/Resources/Prefabs/SayDialog.prefab";
    private const string MenuDialogPrefabPath = "Assets/Fungus/Resources/Prefabs/MenuDialog.prefab";
    private const string DefaultTmpFontResourcePath = "Fonts & Materials/LiberationSans SDF";
    private const string DemoPortraitPathA = "Assets/FungusExamples/Sherlock/Portraits/John/neutral.png";
    private const string DemoPortraitPathB = "Assets/FungusExamples/Sherlock/Portraits/Sherlock/happy.png";
    private const string DemoPortraitPathC = "Assets/FungusExamples/Sherlock/Portraits/John/worried.png";

    [MenuItem("Tools/WinterJam/Setup Main Menu And Pause")]
    public static void SetupMainMenuAndPause()
    {
        SetupHomeScene();
        SetupGameScene(true);
        SetupResultScenes();
        SetupBuildSettings(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("WinterJam", "Main menu, pause menu, result scenes, and Fungus setup complete.", "OK");
    }

    [MenuItem("Tools/WinterJam/Setup Menus Without Fungus")]
    public static void SetupMenusWithoutFungus()
    {
        SetupHomeScene();
        SetupGameScene(false);
        SetupResultScenes();
        SetupBuildSettings(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("WinterJam", "Main menu, pause menu, and result scenes setup complete without Fungus.", "OK");
    }

    public static void SetupMainMenuAndPauseBatch()
    {
        SetupHomeScene();
        SetupGameScene(true);
        SetupResultScenes();
        SetupBuildSettings(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }

    public static void SetupMenusWithoutFungusBatch()
    {
        SetupHomeScene();
        SetupGameScene(false);
        SetupResultScenes();
        SetupBuildSettings(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }

    [MenuItem("Tools/WinterJam/Setup Fungus Test Scene")]
    public static void SetupFungusTestSceneMenu()
    {
        SetupFungusTestScene();
        SetupResultScenes();
        SetupBuildSettings(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("WinterJam", "Fungus test scene setup complete.", "OK");
    }

    public static void SetupFungusTestSceneBatch()
    {
        SetupFungusTestScene();
        SetupResultScenes();
        SetupBuildSettings(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }

    private static void SetupHomeScene()
    {
        Scene scene = OpenOrCreateScene(HomeScenePath);
        RemoveRootObjectIfPresent("Result Canvas");
        RemoveRootObjectIfPresent("Gameplay Canvas");

        EnsureMainCamera();
        EventSystem eventSystem = EnsureEventSystem();
        ConfigureEventSystem(eventSystem);
        Canvas canvas = GetOrCreateCanvas("Home Canvas", 0);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        ClearChildren(canvasRect);

        CreateFullscreenImage("Background", canvasRect, new Color(0.1f, 0.12f, 0.17f, 1f));

        RectTransform contentRoot = CreateUIObject("Content", canvasRect).GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.sizeDelta = new Vector2(720f, 540f);
        contentRoot.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(48, 48, 48, 48);

        AddLayoutSpace(contentRoot, 24f);
        CreateText("Title", contentRoot, "Title", 58, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, 80f);
        CreateText("Subtitle", contentRoot, "Sub Title", 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.78f, 0.82f, 0.9f, 1f), 36f);
        AddLayoutSpace(contentRoot, 14f);

        GameObject controllerObject = GetOrCreateRootObject("MainMenuController");
        MainMenuController menuController = GetOrAddComponent<MainMenuController>(controllerObject);

        Button startButton = CreateButton("Start Game", contentRoot, new Color(0.82f, 0.49f, 0.19f, 1f));
        Button quitButton = CreateButton("Quit", contentRoot, new Color(0.29f, 0.33f, 0.39f, 1f));

        startButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(startButton.onClick, menuController.LoadGameScene);
        UnityEventTools.AddPersistentListener(quitButton.onClick, menuController.QuitGame);

        ControllerUINavigationController navigationController = GetOrAddComponent<ControllerUINavigationController>(canvas.gameObject);
        ConfigureSelectableNavigation(contentRoot);

        UICanvasSelectionFrame selectionFrame = EnsureSelectionFrame(canvas.transform, canvas);
        ConfigureMenuSelectionFrame(selectionFrame, canvas, contentRoot);
        ConfigureNavigationController(navigationController, startButton, contentRoot);

        eventSystem.SetSelectedGameObject(startButton.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupGameScene(bool includeFungus)
    {
        Scene scene = OpenOrCreateScene(GameScenePath);

        EnsureMainCamera();
        EventSystem eventSystem = EnsureEventSystem();
        ConfigureEventSystem(eventSystem);
        Canvas canvas = GetOrCreateCanvas("Gameplay Canvas", 10);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        CreateOrUpdateHudLabel(canvasRect);

        if (includeFungus)
            SetupFungusPrefabs();
        else
            RemoveFungusObjects();

        GameObject menuControllerObject = GetOrCreateRootObject("SceneMenuController");
        MainMenuController menuController = GetOrAddComponent<MainMenuController>(menuControllerObject);

        RectTransform selectionPanel = GetOrCreatePersonSelectionPanel(canvasRect);
        RectTransform pausePanel = GetOrCreatePausePanel(canvasRect, menuController);
        Transform pauseButtonRoot = pausePanel.Find("Buttons");
        Transform selectionCardList = selectionPanel.Find("Frame/Card List");
        Button resumeButton = pausePanel.Find("Buttons/Resume Button").GetComponent<Button>();
        Button restartButton = pausePanel.Find("Buttons/Restart Button").GetComponent<Button>();

        BattlePauseMenuController pauseController = GetOrAddComponent<BattlePauseMenuController>(menuControllerObject);
        SerializedObject pauseControllerObject = new SerializedObject(pauseController);
        pauseControllerObject.FindProperty("pauseUiRoot").objectReferenceValue = pausePanel.gameObject;
        pauseControllerObject.FindProperty("navigationRoot").objectReferenceValue = pauseButtonRoot;
        pauseControllerObject.FindProperty("initialSelection").objectReferenceValue = resumeButton;

        ControllerUINavigationController navigationController = GetOrAddComponent<ControllerUINavigationController>(canvas.gameObject);
        UICanvasSelectionFrame selectionFrame = EnsureSelectionFrame(canvas.transform, canvas);

        pauseControllerObject.FindProperty("selectionFrame").objectReferenceValue = selectionFrame;
        pauseControllerObject.FindProperty("navigationController").objectReferenceValue = navigationController;
        pauseControllerObject.FindProperty("hidePauseUiRootWhenClosed").boolValue = true;
        pauseControllerObject.ApplyModifiedPropertiesWithoutUndo();

        PersonSelectionMenuController selectionMenuController = GetOrAddComponent<PersonSelectionMenuController>(menuControllerObject);
        SerializedObject selectionMenuControllerObject = new SerializedObject(selectionMenuController);
        selectionMenuControllerObject.FindProperty("menuRoot").objectReferenceValue = selectionPanel.gameObject;
        selectionMenuControllerObject.FindProperty("cardContainer").objectReferenceValue = selectionCardList;
        selectionMenuControllerObject.FindProperty("cardTemplate").objectReferenceValue = selectionPanel.Find("Frame/Card List/Person Card Template").GetComponent<PersonSelectionCardUI>();
        selectionMenuControllerObject.FindProperty("emptyStateLabel").objectReferenceValue = selectionPanel.Find("Frame/Empty State").GetComponent<TextMeshProUGUI>();
        selectionMenuControllerObject.FindProperty("peopleRoot").objectReferenceValue = GetOrCreatePeopleHolder();
        selectionMenuControllerObject.FindProperty("navigationController").objectReferenceValue = navigationController;
        selectionMenuControllerObject.FindProperty("showMenuOnStart").boolValue = true;
        selectionMenuControllerObject.FindProperty("refreshCardsOnShow").boolValue = true;
        selectionMenuControllerObject.ApplyModifiedPropertiesWithoutUndo();

        resumeButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(resumeButton.onClick, pauseController.ClosePauseMenu);
        restartButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(restartButton.onClick, menuController.RestartCurrentScene);

        ConfigureSelectableNavigation(pauseButtonRoot as RectTransform);
        ConfigureSelectableNavigation(selectionCardList as RectTransform);
        ConfigureGameplaySelectionFrame(selectionFrame, canvas, pauseButtonRoot);
        ConfigureNavigationController(navigationController, resumeButton, pauseButtonRoot, selectionCardList);

        pausePanel.gameObject.SetActive(false);
        eventSystem.SetSelectedGameObject(null);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupResultScenes()
    {
        SetupResultScene(WinScenePath, "You Win", "The date survived the night.");
        SetupResultScene(LoseScenePath, "You Lose", "The night ends here.");
    }

    private static void SetupResultScene(string scenePath, string title, string subtitle)
    {
        Scene scene = OpenOrCreateScene(scenePath);
        RemoveRootObjectIfPresent("Home Canvas");
        RemoveRootObjectIfPresent("Gameplay Canvas");

        EnsureMainCamera();
        EventSystem eventSystem = EnsureEventSystem();
        ConfigureEventSystem(eventSystem);
        Canvas canvas = GetOrCreateCanvas("Result Canvas", 0);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        ClearChildren(canvasRect);

        CreateFullscreenImage("Background", canvasRect, new Color(0.1f, 0.12f, 0.17f, 1f));

        RectTransform contentRoot = CreateUIObject("Content", canvasRect).GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.sizeDelta = new Vector2(720f, 540f);
        contentRoot.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(48, 48, 48, 48);

        AddLayoutSpace(contentRoot, 24f);
        CreateText("Title", contentRoot, title, 58, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, 80f);
        CreateText("Subtitle", contentRoot, subtitle, 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.78f, 0.82f, 0.9f, 1f), 36f);
        AddLayoutSpace(contentRoot, 14f);

        GameObject controllerObject = GetOrCreateRootObject("ResultMenuController");
        MainMenuController menuController = GetOrAddComponent<MainMenuController>(controllerObject);

        Button restartButton = CreateButton("Restart", contentRoot, new Color(0.82f, 0.49f, 0.19f, 1f));
        Button mainMenuButton = CreateButton("Main Menu", contentRoot, new Color(0.25f, 0.54f, 0.73f, 1f));
        Button quitButton = CreateButton("Quit", contentRoot, new Color(0.29f, 0.33f, 0.39f, 1f));

        restartButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(restartButton.onClick, menuController.LoadGameScene);
        UnityEventTools.AddPersistentListener(mainMenuButton.onClick, menuController.LoadHomeScene);
        UnityEventTools.AddPersistentListener(quitButton.onClick, menuController.QuitGame);

        ControllerUINavigationController navigationController = GetOrAddComponent<ControllerUINavigationController>(canvas.gameObject);
        ConfigureSelectableNavigation(contentRoot);

        UICanvasSelectionFrame selectionFrame = EnsureSelectionFrame(canvas.transform, canvas);
        ConfigureMenuSelectionFrame(selectionFrame, canvas, contentRoot);
        ConfigureNavigationController(navigationController, restartButton, contentRoot);

        eventSystem.SetSelectedGameObject(restartButton.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupFungusTestScene()
    {
        Scene templateScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(templateScene, TestScenePath, true);

        Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);

        EnsureMainCamera();
        ConfigureEventSystem(EnsureEventSystem());
        SetupFungusPrefabs();

        Canvas gameplayCanvas = Object.FindObjectOfType<Canvas>();
        if (gameplayCanvas != null && gameplayCanvas.name == "Gameplay Canvas")
            CreateOrUpdateHudLabel(gameplayCanvas.GetComponent<RectTransform>(), "TEST Scene\nPick a profile to launch that person's Flowchart.\nEsc / Start: Pause");

        SetupPersonSelectionDemo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupPersonSelectionDemo()
    {
        Flowchart rootFlowchart = GameObject.Find("Flowchart") != null ? GameObject.Find("Flowchart").GetComponent<Flowchart>() : null;
        if (rootFlowchart != null)
            ClearFlowchart(rootFlowchart);

        SayDialog sayDialog = Object.FindObjectOfType<SayDialog>();
        MenuDialog menuDialog = Object.FindObjectOfType<MenuDialog>();
        PersonSelectionMenuController selectionMenuController = Object.FindObjectOfType<PersonSelectionMenuController>(true);
        GameObject selectionMenuObject = selectionMenuController != null ? selectionMenuController.gameObject : null;
        Transform peopleHolder = GetOrCreatePeopleHolder();

        ClearChildren(peopleHolder);

        CreateDemoPerson(peopleHolder, "Avery", 24, AssetDatabase.LoadAssetAtPath<Sprite>(DemoPortraitPathA), sayDialog, menuDialog, selectionMenuObject, new Vector2(40f, 40f));
        CreateDemoPerson(peopleHolder, "Morgan", 29, AssetDatabase.LoadAssetAtPath<Sprite>(DemoPortraitPathB), sayDialog, menuDialog, selectionMenuObject, new Vector2(40f, 360f));
        CreateDemoPerson(peopleHolder, "Rowan", 34, AssetDatabase.LoadAssetAtPath<Sprite>(DemoPortraitPathC), sayDialog, menuDialog, selectionMenuObject, new Vector2(40f, 680f));

        if (selectionMenuController != null)
            selectionMenuController.RefreshCards();
    }

    private static void CreateDemoPerson(Transform parent, string personName, int age, Sprite portrait, SayDialog sayDialog, MenuDialog menuDialog, GameObject selectionMenuObject, Vector2 startBlockPosition)
    {
        GameObject personObject = InstantiateScenePerson(parent, personName);
        Person person = GetOrAddComponent<Person>(personObject);
        Flowchart personFlowchart = GetOrAddComponent<Flowchart>(personObject);

        SerializedObject personObjectData = new SerializedObject(person);
        personObjectData.FindProperty("ageMin").intValue = age;
        personObjectData.FindProperty("ageMax").intValue = age;
        personObjectData.FindProperty("personName").stringValue = personName;
        personObjectData.FindProperty("portrait").objectReferenceValue = portrait;
        personObjectData.FindProperty("startBlockName").stringValue = "Start";
        personObjectData.FindProperty("flowchart").objectReferenceValue = personFlowchart;
        personObjectData.ApplyModifiedPropertiesWithoutUndo();

        BuildDemoPersonFlowchart(personFlowchart, personName, sayDialog, menuDialog, selectionMenuObject, startBlockPosition);
    }

    private static void BuildDemoPersonFlowchart(Flowchart flowchart, string personName, SayDialog sayDialog, MenuDialog menuDialog, GameObject selectionMenuObject, Vector2 startBlockPosition)
    {
        ClearFlowchart(flowchart);
        ConfigureMenuDialog(menuDialog);

        Block startBlock = CreateBlock(flowchart, "Start", startBlockPosition);
        AddSetSayDialog(startBlock, sayDialog);
        AddSetMenuDialog(startBlock, menuDialog);
        AddSay(startBlock, "Hi, I'm " + personName + ".");
        AddSay(startBlock, "This conversation started from the person selection menu.");
        AddSay(startBlock, "In MainScene, each person can have their own Flowchart and their own portrait card.");
        AddSay(startBlock, "When this short demo ends, the selector will appear again so you can try another profile.", true);
        AddCallMethod(startBlock, selectionMenuObject, "ShowSelectionMenu");
        AddCommand<Stop>(startBlock);
    }

    private static GameObject InstantiateScenePerson(Transform parent, string personName)
    {
        GameObject personObject = new GameObject("Person", typeof(Transform), typeof(Person));

        personObject.name = personName;
        personObject.transform.SetParent(parent, false);
        personObject.transform.localPosition = Vector3.zero;
        return personObject;
    }

    private static Transform GetOrCreatePeopleHolder()
    {
        GameObject peopleObject = GetOrCreateRootObject("People");
        return peopleObject.transform;
    }

    private static void SetupFungusPrefabs()
    {
        InstantiatePrefabIfMissing(FlowchartPrefabPath, "Flowchart");
        InstantiatePrefabIfMissing(SayDialogPrefabPath, "SayDialog");
        InstantiatePrefabIfMissing(MenuDialogPrefabPath, "MenuDialog");
        ConfigureFungusUi();
    }

    private static void RemoveFungusObjects()
    {
        RemoveRootObjectIfPresent("Flowchart");
        RemoveRootObjectIfPresent("SayDialog");
        RemoveRootObjectIfPresent("MenuDialog");
    }

    private static void InstantiatePrefabIfMissing(string prefabPath, string rootName)
    {
        if (GameObject.Find(rootName) != null)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return;

        instance.name = rootName;
        Undo.RegisterCreatedObjectUndo(instance, "Instantiate " + rootName);
    }

    private static void SetupBuildSettings(bool includeFungusTestScene)
    {
        if (includeFungusTestScene)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(HomeScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
                new EditorBuildSettingsScene(WinScenePath, true),
                new EditorBuildSettingsScene(LoseScenePath, true),
                new EditorBuildSettingsScene(TestScenePath, true)
            };
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(HomeScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
            new EditorBuildSettingsScene(WinScenePath, true),
            new EditorBuildSettingsScene(LoseScenePath, true)
        };
    }

    private static Scene OpenOrCreateScene(string scenePath)
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset != null)
            return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, scenePath);
        return scene;
    }

    private static void EnsureMainCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            camera = cameraObject.GetComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.15f, 0.2f, 1f);
    }

    private static EventSystem EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem != null)
            return eventSystem;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return eventSystemObject.GetComponent<EventSystem>();
    }

    private static void ConfigureEventSystem(EventSystem eventSystem)
    {
        if (eventSystem == null)
            return;

        StandaloneInputModule inputModule = GetOrAddComponent<StandaloneInputModule>(eventSystem.gameObject);
        inputModule.horizontalAxis = "Horizontal";
        inputModule.verticalAxis = "Vertical";
        inputModule.submitButton = "Submit";
        inputModule.cancelButton = "Cancel";
    }

    private static Canvas GetOrCreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = GameObject.Find(name);
        if (canvasObject == null)
            canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAddComponent<GraphicRaycaster>(canvasObject);

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return canvas;
    }

    private static void CreateOrUpdateHudLabel(RectTransform canvasRect)
    {
        CreateOrUpdateHudLabel(canvasRect, "Main Scene\nEsc / Start: Pause");
    }

    private static void CreateOrUpdateHudLabel(RectTransform canvasRect, string labelText)
    {
        TextMeshProUGUI hudText = GetOrCreateText("HUD Label", canvasRect);
        hudText.text = labelText;
        hudText.fontSize = 18;
        hudText.fontStyle = FontStyles.Normal;
        hudText.alignment = TextAlignmentOptions.TopLeft;
        hudText.color = Color.white;

        RectTransform rect = hudText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(540f, 72f);
        rect.anchoredPosition = new Vector2(28f, -28f);
    }

    private static RectTransform GetOrCreatePersonSelectionPanel(RectTransform canvasRect)
    {
        RectTransform panel = GetOrCreateRectTransform("Person Selection Panel", canvasRect);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = GetOrAddComponent<Image>(panel.gameObject);
        panelImage.color = new Color(0.05f, 0.07f, 0.1f, 0.82f);
        panelImage.raycastTarget = true;

        ClearChildren(panel);

        RectTransform frame = CreateUIObject("Frame", panel).GetComponent<RectTransform>();
        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(1440f, 780f);
        frame.anchoredPosition = Vector2.zero;

        Image frameImage = GetOrAddComponent<Image>(frame.gameObject);
        frameImage.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);

        VerticalLayoutGroup layout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(40, 40, 40, 40);

        CreateText("Title", frame, "Choose a Date", 48, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, 72f);
        CreateText("Subtitle", frame, "Pick a profile to start the conversation.", 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.78f, 0.82f, 0.9f, 1f), 36f);

        TextMeshProUGUI emptyState = CreateText("Empty State", frame, "No people available yet.", 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.86f, 0.86f, 0.86f, 1f), 36f);
        emptyState.gameObject.SetActive(false);

        RectTransform cardList = CreateUIObject("Card List", frame).GetComponent<RectTransform>();
        cardList.sizeDelta = new Vector2(1320f, 520f);

        LayoutElement cardListLayout = cardList.gameObject.AddComponent<LayoutElement>();
        cardListLayout.preferredHeight = 520f;

        HorizontalLayoutGroup cardLayout = cardList.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.spacing = 20f;
        cardLayout.childControlWidth = false;
        cardLayout.childControlHeight = false;
        cardLayout.childForceExpandWidth = false;
        cardLayout.childForceExpandHeight = false;

        CreatePersonCardTemplate(cardList);
        return panel;
    }

    private static void CreatePersonCardTemplate(Transform parent)
    {
        RectTransform card = GetOrCreateRectTransform("Person Card Template", parent);
        card.sizeDelta = new Vector2(320f, 500f);

        LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(card.gameObject);
        layoutElement.preferredWidth = 320f;
        layoutElement.preferredHeight = 500f;
        layoutElement.minWidth = 320f;
        layoutElement.minHeight = 500f;

        Image cardImage = GetOrAddComponent<Image>(card.gameObject);
        cardImage.color = new Color(0.12f, 0.16f, 0.23f, 0.98f);
        cardImage.raycastTarget = true;

        ClearChildren(card);

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(20, 20, 20, 20);

        RectTransform portrait = CreateUIObject("Portrait", card).GetComponent<RectTransform>();
        portrait.sizeDelta = new Vector2(0f, 240f);

        LayoutElement portraitLayout = portrait.gameObject.AddComponent<LayoutElement>();
        portraitLayout.preferredHeight = 240f;

        Image portraitImage = portrait.gameObject.AddComponent<Image>();
        portraitImage.color = new Color(0.24f, 0.27f, 0.33f, 1f);
        portraitImage.raycastTarget = false;

        CreateText("Name", card, "Name", 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, 42f);
        CreateText("Age", card, "Age --", 22, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.82f, 0.86f, 0.92f, 1f), 32f);
        AddLayoutSpace(card, 6f);

        Button startButton = CreateButton("Start", card, new Color(0.82f, 0.49f, 0.19f, 1f));
        startButton.name = "Start Button";

        GetOrAddComponent<PersonSelectionCardUI>(card.gameObject);
    }

    private static RectTransform GetOrCreatePausePanel(RectTransform canvasRect, MainMenuController menuController)
    {
        RectTransform panel = GetOrCreateRectTransform("Pause Panel", canvasRect);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(560f, 430f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = GetOrAddComponent<Image>(panel.gameObject);
        panelImage.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

        Canvas panelCanvas = GetOrAddComponent<Canvas>(panel.gameObject);
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 1000;
        GetOrAddComponent<GraphicRaycaster>(panel.gameObject);

        ClearChildren(panel);

        CreateAbsoluteText("Pause Title", panel, "Paused", 40, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 145f), new Vector2(360f, 64f));

        RectTransform buttonRoot = CreateUIObject("Buttons", panel).GetComponent<RectTransform>();
        buttonRoot.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRoot.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRoot.pivot = new Vector2(0.5f, 0.5f);
        buttonRoot.sizeDelta = new Vector2(360f, 210f);
        buttonRoot.anchoredPosition = new Vector2(0f, -10f);

        VerticalLayoutGroup layout = buttonRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Button resumeButton = CreateButton("Resume", buttonRoot, new Color(0.82f, 0.49f, 0.19f, 1f));
        resumeButton.name = "Resume Button";
        Button homeButton = CreateButton("Main Menu", buttonRoot, new Color(0.25f, 0.54f, 0.73f, 1f));
        homeButton.name = "Home Button";
        Button restartButton = CreateButton("Restart", buttonRoot, new Color(0.44f, 0.58f, 0.25f, 1f));
        restartButton.name = "Restart Button";
        Button quitButton = CreateButton("Quit", buttonRoot, new Color(0.29f, 0.33f, 0.39f, 1f));
        quitButton.name = "Quit Button";

        homeButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(homeButton.onClick, menuController.LoadHomeScene);
        UnityEventTools.AddPersistentListener(quitButton.onClick, menuController.QuitGame);

        return panel;
    }

    private static void ConfigureMenuSelectionFrame(UICanvasSelectionFrame selectionFrame, Canvas canvas, Transform navigationRoot)
    {
        ConfigureSelectionFrame(selectionFrame, canvas, navigationRoot, new Vector2(1f, 1f));
    }

    private static void ConfigureGameplaySelectionFrame(UICanvasSelectionFrame selectionFrame, Canvas canvas, Transform navigationRoot)
    {
        ConfigureSelectionFrame(selectionFrame, canvas, navigationRoot, new Vector2(1.35f, 1f));
    }

    private static void ConfigureSelectionFrame(UICanvasSelectionFrame selectionFrame, Canvas canvas, Transform navigationRoot, Vector2 sizeMultiplier)
    {
        RectTransform frameRect = selectionFrame.transform as RectTransform;
        if (frameRect != null)
        {
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = Vector2.zero;
        }

        SerializedObject selectionFrameObject = new SerializedObject(selectionFrame);
        selectionFrameObject.FindProperty("frameRoot").objectReferenceValue = frameRect;
        selectionFrameObject.FindProperty("targetCanvas").objectReferenceValue = canvas;
        selectionFrameObject.FindProperty("navigationRoot").objectReferenceValue = navigationRoot;
        selectionFrameObject.FindProperty("useSceneWideFallbackNavigation").boolValue = false;
        selectionFrameObject.FindProperty("frameColor").colorValue = new Color(0.88f, 0.62f, 0.18f, 1f);
        selectionFrameObject.FindProperty("offset").vector2Value = new Vector2(0.52f, 0f);
        selectionFrameObject.FindProperty("sizeMultiplier").vector2Value = sizeMultiplier;
        selectionFrameObject.FindProperty("padding").vector2Value = new Vector2(15.8f, 14.5f);
        selectionFrameObject.FindProperty("clampSizeToRoot").boolValue = true;
        selectionFrameObject.FindProperty("clampRootPadding").vector2Value = new Vector2(-14.16f, 4f);
        selectionFrameObject.FindProperty("cornerLength").floatValue = 36f;
        selectionFrameObject.FindProperty("cornerThickness").floatValue = 7f;
        selectionFrameObject.FindProperty("keepOnTop").boolValue = true;
        selectionFrameObject.FindProperty("hideWhenNoSelection").boolValue = true;
        selectionFrameObject.FindProperty("snapInstantly").boolValue = false;
        selectionFrameObject.FindProperty("followSpeed").floatValue = 18f;
        selectionFrameObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureNavigationController(ControllerUINavigationController navigationController, Selectable initialSelection, Transform navigationRoot, params Transform[] additionalPanelRoots)
    {
        SerializedObject navigationObject = new SerializedObject(navigationController);
        navigationObject.FindProperty("initialSelection").objectReferenceValue = initialSelection;
        navigationObject.FindProperty("navigationRoot").objectReferenceValue = navigationRoot;
        navigationObject.FindProperty("selectFirstOnEnable").boolValue = true;
        navigationObject.FindProperty("keepSelectionAlive").boolValue = true;

        int panelTargetCount = 0;
        if (navigationRoot != null)
            panelTargetCount++;

        for (int i = 0; i < additionalPanelRoots.Length; i++)
        {
            if (additionalPanelRoots[i] != null)
                panelTargetCount++;
        }

        SerializedProperty panelTargetsProperty = navigationObject.FindProperty("panelNavigationTargets");
        panelTargetsProperty.arraySize = panelTargetCount;

        int targetIndex = 0;
        if (navigationRoot != null)
        {
            SerializedProperty targetProperty = panelTargetsProperty.GetArrayElementAtIndex(targetIndex++);
            targetProperty.FindPropertyRelative("panelRoot").objectReferenceValue = navigationRoot;
            targetProperty.FindPropertyRelative("initialSelection").objectReferenceValue = initialSelection;
        }

        for (int i = 0; i < additionalPanelRoots.Length; i++)
        {
            Transform panelRoot = additionalPanelRoots[i];
            if (panelRoot == null)
                continue;

            SerializedProperty targetProperty = panelTargetsProperty.GetArrayElementAtIndex(targetIndex++);
            targetProperty.FindPropertyRelative("panelRoot").objectReferenceValue = panelRoot;
            targetProperty.FindPropertyRelative("initialSelection").objectReferenceValue = null;
        }

        navigationObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static UICanvasSelectionFrame EnsureSelectionFrame(Transform canvasTransform, Canvas canvas)
    {
        Transform existing = canvasTransform.Find("Selection Frame");
        GameObject frameObject = existing != null
            ? existing.gameObject
            : new GameObject("Selection Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(UICanvasSelectionFrame));

        frameObject.transform.SetParent(canvasTransform, false);
        frameObject.transform.SetAsLastSibling();

        return GetOrAddComponent<UICanvasSelectionFrame>(frameObject);
    }

    private static void ConfigureSelectableNavigation(RectTransform root)
    {
        if (root == null)
            return;

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Navigation navigation = selectables[i].navigation;
            navigation.mode = Navigation.Mode.Automatic;
            selectables[i].navigation = navigation;
        }
    }

    private static Button CreateButton(string label, Transform parent, Color color)
    {
        GameObject buttonObject = CreateUIObject(label + " Button", parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = color * 1.08f;
        colors.selectedColor = color * 1.08f;
        colors.pressedColor = color * 0.92f;
        button.colors = colors;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 58f);

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 58f;
        layoutElement.minHeight = 58f;

        TextMeshProUGUI labelText = CreateText("Label", buttonObject.transform, label, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, 58f);
        labelText.raycastTarget = false;

        return button;
    }

    private static void CreateFullscreenImage(string name, Transform parent, Color color)
    {
        RectTransform rectTransform = GetOrCreateRectTransform(name, parent);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        Image image = GetOrAddComponent<Image>(rectTransform.gameObject);
        image.color = color;
        image.raycastTarget = false;
    }

    private static TextMeshProUGUI GetOrCreateText(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        GameObject textObject = existing != null ? existing.gameObject : null;
        TextMeshProUGUI text = textObject != null ? textObject.GetComponent<TextMeshProUGUI>() : null;

        if (textObject == null || text == null)
        {
            if (textObject != null)
                Object.DestroyImmediate(textObject);

            textObject = CreateUIObject(name, parent);
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        Graphic[] graphics = textObject.GetComponents<Graphic>();
        for (int i = 0; i < graphics.Length; i++)
        {
            if (!(graphics[i] is TextMeshProUGUI))
                Object.DestroyImmediate(graphics[i]);
        }

        TMP_FontAsset defaultFont = GetDefaultTmpFont();
        if (defaultFont != null)
            text.font = defaultFont;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static TMP_FontAsset GetDefaultTmpFont()
    {
        TMP_FontAsset defaultFont = null;

        try
        {
            defaultFont = TMP_Settings.defaultFontAsset;
        }
        catch
        {
            defaultFont = null;
        }

        if (defaultFont != null)
            return defaultFont;

        return Resources.Load<TMP_FontAsset>(DefaultTmpFontResourcePath);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize, FontStyles fontStyle, TextAlignmentOptions anchor, Color color, float preferredHeight)
    {
        TextMeshProUGUI text = GetOrCreateText(name, parent);
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = anchor;
        text.color = color;

        LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(text.gameObject);
        layoutElement.preferredHeight = preferredHeight;

        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(0f, preferredHeight);
        return text;
    }

    private static void CreateAbsoluteText(string name, Transform parent, string content, float fontSize, FontStyles fontStyle, TextAlignmentOptions anchor, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        TextMeshProUGUI text = GetOrCreateText(name, parent);
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = anchor;
        text.color = color;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void AddLayoutSpace(Transform parent, float height)
    {
        GameObject spacer = CreateUIObject("Spacer", parent);
        LayoutElement layout = spacer.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
    }

    private static void ClearFlowchart(Flowchart flowchart)
    {
        Component[] components = flowchart.GetComponents<Component>();
        for (int i = components.Length - 1; i >= 0; i--)
        {
            Component component = components[i];
            if (component == null)
                continue;

            if (component == flowchart || component is Transform)
                continue;

            Object.DestroyImmediate(component);
        }
    }

    private static Block CreateBlock(Flowchart flowchart, string blockName, Vector2 position)
    {
        Block block = flowchart.CreateBlock(position);
        block.BlockName = blockName;
        block.ItemId = flowchart.NextItemId();
        return block;
    }

    private static T AddCommand<T>(Block block) where T : Command
    {
        T command = block.gameObject.AddComponent<T>();
        command.ItemId = block.GetFlowchart().NextItemId();
        command.ParentBlock = block;
        command.CommandIndex = block.CommandList.Count;
        block.CommandList.Add(command);
        return command;
    }

    private static void AddSetSayDialog(Block block, SayDialog sayDialog)
    {
        if (sayDialog == null)
            return;

        SetSayDialog command = AddCommand<SetSayDialog>(block);
        SerializedObject commandObject = new SerializedObject(command);
        commandObject.FindProperty("sayDialog").objectReferenceValue = sayDialog;
        commandObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSetMenuDialog(Block block, MenuDialog menuDialog)
    {
        if (menuDialog == null)
            return;

        SetMenuDialog command = AddCommand<SetMenuDialog>(block);
        SerializedObject commandObject = new SerializedObject(command);
        commandObject.FindProperty("menuDialog").objectReferenceValue = menuDialog;
        commandObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureMenuDialog(MenuDialog menuDialog)
    {
        if (menuDialog == null)
            return;

        SerializedObject menuDialogObject = new SerializedObject(menuDialog);
        menuDialogObject.FindProperty("autoSelectFirstButton").boolValue = true;
        menuDialogObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFungusUi()
    {
        SayDialog sayDialog = Object.FindObjectOfType<SayDialog>(true);
        if (sayDialog != null)
        {
            Canvas sayCanvas = sayDialog.GetComponent<Canvas>();
            if (sayCanvas != null)
            {
                sayCanvas.overrideSorting = true;
                sayCanvas.sortingOrder = 1;
            }

            DialogInput dialogInput = sayDialog.GetComponent<DialogInput>();
            if (dialogInput != null)
            {
                SerializedObject dialogInputObject = new SerializedObject(dialogInput);
                dialogInputObject.FindProperty("cancelEnabled").boolValue = false;
                dialogInputObject.FindProperty("ignoreMenuClicks").boolValue = true;
                dialogInputObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        MenuDialog menuDialog = Object.FindObjectOfType<MenuDialog>(true);
        if (menuDialog != null)
        {
            Canvas menuCanvas = menuDialog.GetComponent<Canvas>();
            if (menuCanvas != null)
            {
                menuCanvas.overrideSorting = true;
                menuCanvas.sortingOrder = 1;
            }
        }
    }

    private static void AddSay(Block block, string text, bool fadeWhenDone = false)
    {
        Say command = AddCommand<Say>(block);
        SerializedObject commandObject = new SerializedObject(command);
        commandObject.FindProperty("storyText").stringValue = text;
        commandObject.FindProperty("waitForClick").boolValue = true;
        commandObject.FindProperty("fadeWhenDone").boolValue = fadeWhenDone;
        commandObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddCallMethod(Block block, GameObject targetObject, string methodName)
    {
        if (targetObject == null || string.IsNullOrWhiteSpace(methodName))
            return;

        CallMethod command = AddCommand<CallMethod>(block);
        SerializedObject commandObject = new SerializedObject(command);
        commandObject.FindProperty("targetObject").objectReferenceValue = targetObject;
        commandObject.FindProperty("methodName").stringValue = methodName;
        commandObject.FindProperty("delay").floatValue = 0f;
        commandObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddMenu(Block block, string label, Block targetBlock)
    {
        Fungus.Menu command = AddCommand<Fungus.Menu>(block);
        SerializedObject commandObject = new SerializedObject(command);
        commandObject.FindProperty("text").stringValue = label;
        commandObject.FindProperty("targetBlock").objectReferenceValue = targetBlock;
        commandObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddLoadScene(Block block, string sceneName)
    {
        LoadScene command = AddCommand<LoadScene>(block);
        SerializedObject commandObject = new SerializedObject(command);
        SerializedProperty sceneNameProperty = commandObject.FindProperty("_sceneName");
        sceneNameProperty.FindPropertyRelative("stringVal").stringValue = sceneName;
        sceneNameProperty.FindPropertyRelative("stringRef").objectReferenceValue = null;
        commandObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static GameObject GetOrCreateRootObject(string name)
    {
        GameObject existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }

    private static RectTransform GetOrCreateRectTransform(string name, Transform parent)
    {
        return CreateOrReuseObject(name, parent).GetComponent<RectTransform>();
    }

    private static GameObject CreateOrReuseObject(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        return CreateUIObject(name, parent);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent<T>();

        return component;
    }

    private static void RemoveRootObjectIfPresent(string rootName)
    {
        GameObject existing = GameObject.Find(rootName);
        if (existing != null)
            Object.DestroyImmediate(existing);
    }
}
