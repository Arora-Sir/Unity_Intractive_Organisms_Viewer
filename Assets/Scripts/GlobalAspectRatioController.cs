using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Forces portrait mode and maintains 9:16 aspect ratio across all scenes
/// Works as a companion to NavigationController
/// </summary>
public class GlobalAspectRatioController : MonoBehaviour
{
    // Target aspect ratio (9:16 for portrait)
    [SerializeField] private float targetAspect = 9f / 16f;

    // Singleton instance
    private static GlobalAspectRatioController _instance;
    public static GlobalAspectRatioController Instance
    {
        get { return _instance; }
    }

    // Black overlay used for letterboxing/pillarboxing
    private GameObject topLetterbox;
    private GameObject bottomLetterbox;
    private GameObject leftPillarbox;
    private GameObject rightPillarbox;

    private void Awake()
    {
        // Singleton pattern implementation
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Register for scene change events
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Create black bars for letterboxing/pillarboxing
        CreateBlackBars();

        // Force portrait orientation
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        Screen.orientation = ScreenOrientation.Portrait;
#endif
    }

    private void Start()
    {
        // Apply aspect ratio on start
        ApplyAspectRatio();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply aspect ratio when a new scene loads
        ApplyAspectRatio();
    }

    private void CreateBlackBars()
    {
        // Create a canvas for our black bars that renders on top of everything
        GameObject canvasObj = new GameObject("AspectRatioCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // Highest possible sorting order

        // Make sure it covers the entire screen
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Create the black bars
        topLetterbox = CreateBlackBar(canvasObj.transform, "TopLetterbox");
        bottomLetterbox = CreateBlackBar(canvasObj.transform, "BottomLetterbox");
        leftPillarbox = CreateBlackBar(canvasObj.transform, "LeftPillarbox");
        rightPillarbox = CreateBlackBar(canvasObj.transform, "RightPillarbox");
    }

    private GameObject CreateBlackBar(Transform parent, string name)
    {
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(parent, false);

        RectTransform rectTransform = bar.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        UnityEngine.UI.Image image = bar.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;

        return bar;
    }

    public void ApplyAspectRatio()
    {
        // Determine the game window's current aspect ratio
        float windowAspect = (float)Screen.width / (float)Screen.height;

        // Reset all black bars
        topLetterbox.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bottomLetterbox.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        leftPillarbox.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        rightPillarbox.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // Calculate how to adjust the viewport
        if (windowAspect < targetAspect) // Screen is taller than needed
        {
            // Add letterboxing (black bars on top and bottom)
            float normalizedBarHeight = (1f - (windowAspect / targetAspect)) / 2f;
            float pixelBarHeight = Screen.height * normalizedBarHeight;

            RectTransform topRect = topLetterbox.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1 - normalizedBarHeight);
            topRect.anchorMax = Vector2.one;
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;

            RectTransform bottomRect = bottomLetterbox.GetComponent<RectTransform>();
            bottomRect.anchorMin = Vector2.zero;
            bottomRect.anchorMax = new Vector2(1, normalizedBarHeight);
            bottomRect.offsetMin = Vector2.zero;
            bottomRect.offsetMax = Vector2.zero;
        }
        else if (windowAspect > targetAspect) // Screen is wider than needed
        {
            // Add pillarboxing (black bars on left and right)
            float normalizedBarWidth = (1f - (targetAspect / windowAspect)) / 2f;
            float pixelBarWidth = Screen.width * normalizedBarWidth;

            RectTransform leftRect = leftPillarbox.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = new Vector2(normalizedBarWidth, 1);
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;

            RectTransform rightRect = rightPillarbox.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(1 - normalizedBarWidth, 0);
            rightRect.anchorMax = Vector2.one;
            rightRect.offsetMin = Vector2.zero;
            rightRect.offsetMax = Vector2.zero;
        }

        // Find all cameras in the scene and adjust their viewport rects
        AdjustAllCameras();
    }

    private void AdjustAllCameras()
    {
        Camera[] cameras = Camera.allCameras;
        float windowAspect = (float)Screen.width / (float)Screen.height;

        foreach (Camera cam in cameras)
        {
            if (windowAspect < targetAspect) // Screen is taller than needed
            {
                float scaleHeight = windowAspect / targetAspect;

                Rect rect = cam.rect;
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
                cam.rect = rect;
            }
            else // Screen is wider than needed
            {
                float scaleWidth = targetAspect / windowAspect;

                Rect rect = cam.rect;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
                cam.rect = rect;
            }
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        // Apply aspect ratio when screen size changes
        ApplyAspectRatio();
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Clear singleton reference if this is the current instance
        if (_instance == this)
        {
            _instance = null;
        }
    }
}