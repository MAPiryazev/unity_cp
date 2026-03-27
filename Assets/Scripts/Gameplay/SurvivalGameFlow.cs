using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SurvivalGameFlow : MonoBehaviour
{
    [SerializeField] Health playerHealth;
    [SerializeField] SimpleWaveSpawner waveSpawner;
    [SerializeField] bool pauseGameOnDeath = true;

    [Header("HUD")]
    [SerializeField] Vector2 statsAnchorMin = new Vector2(0.38f, 0.93f);
    [SerializeField] Vector2 statsAnchorMax = new Vector2(0.62f, 0.99f);
    [SerializeField] Color statsTextColor = new Color(0.95f, 0.97f, 1f, 1f);
    [SerializeField] int statsFontSize = 20;

    [Header("Game Over")]
    [SerializeField] Color overlayColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] Color panelColor = new Color(0.08f, 0.1f, 0.15f, 0.95f);
    [SerializeField] Color buttonColor = new Color(0.2f, 0.35f, 0.95f, 1f);
    [SerializeField] Color buttonTextColor = Color.white;

    Transform _uiRoot;
    Canvas _canvas;
    Text _statsText;
    GameObject _gameOverOverlay;
    Text _gameOverStatsText;
    float _survivalTime;
    bool _isGameOver;
    Font _font;
    int _currentWave = 1;

    void Awake()
    {
        ResolveReferences();
        BuildUiIfNeeded();
    }

    void Start()
    {
        EnsurePlayerHealthBar();
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.Died += HandlePlayerDied;
        if (waveSpawner != null)
            waveSpawner.WaveStarted += HandleWaveStarted;

        UpdateStatsText();
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.Died -= HandlePlayerDied;
        if (waveSpawner != null)
            waveSpawner.WaveStarted -= HandleWaveStarted;
    }

    void OnDestroy()
    {
        if (_uiRoot != null)
            Destroy(_uiRoot.gameObject);

        if (Time.timeScale <= 0f)
            Time.timeScale = 1f;
    }

    void Update()
    {
        if (_isGameOver)
            return;

        _survivalTime += Time.unscaledDeltaTime;
        UpdateStatsText();
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    void HandlePlayerDied(DamageInfo _)
    {
        if (_isGameOver)
            return;

        _isGameOver = true;
        if (waveSpawner != null)
            waveSpawner.enabled = false;
        if (pauseGameOnDeath)
            Time.timeScale = 0f;

        if (_gameOverOverlay != null)
            _gameOverOverlay.SetActive(true);
        if (_gameOverStatsText != null)
            _gameOverStatsText.text = $"Wave: {_currentWave}\nTime: {FormatTime(_survivalTime)}";
    }

    void HandleWaveStarted(int waveNumber)
    {
        _currentWave = Mathf.Max(1, waveNumber);
        UpdateStatsText();
    }

    void UpdateStatsText()
    {
        if (_statsText == null)
            return;

        _statsText.text = $"Wave {_currentWave}   Time {FormatTime(_survivalTime)}";
    }

    void ResolveReferences()
    {
        if (playerHealth == null)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                playerHealth = player.GetComponent<Health>() ?? player.GetComponentInChildren<Health>(true);
        }

        if (waveSpawner == null)
            waveSpawner = FindFirstObjectByType<SimpleWaveSpawner>();

        if (waveSpawner != null)
            _currentWave = Mathf.Max(1, waveSpawner.CurrentWaveIndex);
    }

    void EnsurePlayerHealthBar()
    {
        if (playerHealth == null)
            return;

        if (playerHealth.GetComponent<PlayerHealthBarUI>() == null)
            playerHealth.gameObject.AddComponent<PlayerHealthBarUI>();
    }


    void BuildUiIfNeeded()
    {
        if (_canvas != null)
            return;

        EnsureEventSystemExists();

        _font = ResolveFont();
        var rootObject = new GameObject("SurvivalHUD");
        _uiRoot = rootObject.transform;
        _canvas = rootObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 300;
        rootObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootObject.AddComponent<GraphicRaycaster>();

        CreateStatsPanel(rootObject.transform);
        _gameOverOverlay = CreateGameOverOverlay(rootObject.transform);
        _gameOverOverlay.SetActive(false);
    }

    static void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();
    }

    void CreateStatsPanel(Transform parent)
    {
        var panelObject = new GameObject("StatsPanel");
        panelObject.transform.SetParent(parent, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = statsAnchorMin;
        panelRect.anchorMax = statsAnchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        _statsText = panelObject.AddComponent<Text>();
        _statsText.font = _font;
        _statsText.fontSize = statsFontSize;
        _statsText.alignment = TextAnchor.MiddleCenter;
        _statsText.color = statsTextColor;
    }

    GameObject CreateGameOverOverlay(Transform parent)
    {
        var overlayObject = new GameObject("GameOverOverlay");
        overlayObject.transform.SetParent(parent, false);
        var overlayRect = overlayObject.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.sprite = UiSprites.White;
        overlayImage.color = overlayColor;

        var panelObject = new GameObject("GameOverPanel");
        panelObject.transform.SetParent(overlayObject.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.33f, 0.3f);
        panelRect.anchorMax = new Vector2(0.67f, 0.7f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = UiSprites.White;
        panelImage.color = panelColor;

        var titleText = CreateText(panelObject.transform, "Title", "Game Over", 42, TextAnchor.MiddleCenter, Color.white);
        Stretch(titleText.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.92f), new Vector2(12f, 0f), new Vector2(-12f, 0f));

        _gameOverStatsText = CreateText(panelObject.transform, "Stats", string.Empty, 24, TextAnchor.MiddleCenter, statsTextColor);
        Stretch(_gameOverStatsText.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.62f), new Vector2(12f, 0f), new Vector2(-12f, 0f));

        var restartButtonObject = new GameObject("RestartButton");
        restartButtonObject.transform.SetParent(panelObject.transform, false);
        var buttonRect = restartButtonObject.AddComponent<RectTransform>();
        Stretch(buttonRect, new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.3f), Vector2.zero, Vector2.zero);
        var buttonImage = restartButtonObject.AddComponent<Image>();
        buttonImage.sprite = UiSprites.White;
        buttonImage.color = buttonColor;
        var button = restartButtonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(RestartRun);

        var buttonLabel = CreateText(restartButtonObject.transform, "Label", "Restart", 26, TextAnchor.MiddleCenter, buttonTextColor);
        Stretch(buttonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return overlayObject;
    }

    Text CreateText(Transform parent, string objectName, string value, int size, TextAnchor alignment, Color color)
    {
        var textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = _font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static string FormatTime(float seconds)
    {
        int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = whole / 60;
        int remainingSeconds = whole % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    static Font ResolveFont()
    {
        try
        {
            var legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyFont != null)
                return legacyFont;
        }
        catch (System.ArgumentException)
        {
        }

        try
        {
            var arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (arialFont != null)
                return arialFont;
        }
        catch (System.ArgumentException)
        {
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }
}
