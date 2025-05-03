using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Data structure for each interactive label on a microorganism image
/// </summary>
[System.Serializable]
public class LabelData
{
    /// <summary>The text displayed on the label button</summary>
    public string labelText;

    /// <summary>Detailed information shown when the label is clicked</summary>
    public string infoText;

    /// <summary>Position of the label in normalized coordinates (0-1) relative to the image</summary>
    public Vector2 normalizedPos; // Used as fallback when specific positions aren't available
}

/// <summary>
/// Data structure representing a microorganism image and its associated labels
/// </summary>
[System.Serializable]
public class ImageData
{
    /// <summary>URL to the image file</summary>
    public string imageUrl;

    /// <summary>List of interactive labels for this image</summary>
    public List<LabelData> labels;
}

/// <summary>
/// Main controller for the interactive microorganism viewer.
/// Handles loading images, creating interactive labels, managing UI transitions,
/// and organism switching animations.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main image display for the microorganism")]
    public RawImage mainImage;

    [Tooltip("Prefab for interactive label buttons")]
    public GameObject labelButtonPrefab;

    [Tooltip("Parent transform for all label buttons")]
    public Transform labelParent;

    [Tooltip("Panel that displays detailed information when a label is clicked")]
    public GameObject infoPanel;

    [Tooltip("Title text in the info panel")]
    public TextMeshProUGUI infoTitleText;

    [Tooltip("Description text in the info panel")]
    public TextMeshProUGUI infoDescText;

    [Tooltip("Container for the main image (used for animations)")]
    public RectTransform imageContainer;

    [Tooltip("Loading indicator shown while images are loading")]
    public GameObject loadingIndicator;

    [Tooltip("Dropdown menu for selecting different organisms")]
    public TMPro.TMP_Dropdown organismSelector;

    [Tooltip("Button to close the info panel")]
    public Button closeInfoButton;

    [Tooltip("Prefab for drawing connecting lines between labels and features")]
    public GameObject linePrefab;

    [Header("Animation Settings")]
    [Tooltip("Duration of the slide animation when switching organisms")]
    public float slideAnimationDuration = 0.5f;

    [Tooltip("Distance offscreen for slide animations")]
    public float offscreenPositionX = 1500f;

    [Header("Organism Position Configurations")]
    [Tooltip("List of organism-specific label managers")]
    public List<OrganismLabelManager> organismManagers = new List<OrganismLabelManager>();

    [Header("Fallback and Transition")]
    [Tooltip("Default image shown when loading fails")]
    public RawImage defaultImage;

    [Tooltip("Timeout duration for image loading in seconds")]
    public float imageLoadTimeout = 7.0f;

    [Header("Internet Connection Handling")]
    [Tooltip("Popup panel to show when internet connection is unavailable")]
    public GameObject noInternetPopup;

    [Tooltip("Text component in the no internet popup")]
    public TextMeshProUGUI noInternetText;

    // Track internet connection status
    private bool hasInternetConnection = true;
    private bool isUsingDefaultImage = false;
    private Coroutine internetCheckCoroutine;
    private Coroutine initialInternetCheckCoroutine; // New coroutine for initial check with delay

    // Internal state tracking
    private bool imageLoadSuccess = false;
    private Coroutine timeoutCoroutine;
    private bool isTransitioningFromMenu = false;
    private bool hasInitialLoad = false;
    private Texture2D loadedTexture;
    private bool isLoading = false;
    private Camera mainCamera;

    // For organism switching animation
    private RawImage previousImage;
    private Transform previousLabelsContainer;
    private bool isAnimatingSwitch = false;

    /// <summary>
    /// Initializes the UI, sets up event handlers, and loads the initial organism.
    /// </summary>
    void Start()
    {
        // Existing debug code
        Debug.Log("GameManager Start called");

        // Check if images are properly set up
        if (mainImage == null)
        {
            Debug.LogError("Main image is not assigned in the inspector!");
        }
        else
        {
            Debug.Log("Main image reference OK");
        }

        if (defaultImage == null)
        {
            Debug.LogError("Default image is not assigned in the inspector!");
        }
        else if (defaultImage.texture == null)
        {
            Debug.LogError("Default image has no texture assigned in the inspector!");
        }
        else
        {
            Debug.Log("Default image reference OK with texture: " + defaultImage.texture.name);
        }

        // Log the available organism managers for debugging
        DebugOrganismManagers();

        // Get reference to main camera for positioning calculations
        mainCamera = Camera.main;

        // Show home button if NavigationController exists
        if (NavigationController.Instance != null)
        {
            NavigationController.Instance.homeButton.gameObject.SetActive(true);
        }

        // Initialize the no internet popup
        if (noInternetPopup != null)
        {
            noInternetPopup.SetActive(false);
        }

        // Set up UI initial state
        infoPanel.SetActive(false);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(true); // Show loading indicator initially

        if (closeInfoButton != null)
            closeInfoButton.onClick.AddListener(HideInfoPanel);

        // Ensure images have CanvasGroup components for fading
        if (defaultImage != null && defaultImage.GetComponent<CanvasGroup>() == null)
            defaultImage.gameObject.AddComponent<CanvasGroup>();

        if (mainImage != null && mainImage.GetComponent<CanvasGroup>() == null)
            mainImage.gameObject.AddComponent<CanvasGroup>();

        // Initially hide both images until we determine which to show
        defaultImage.gameObject.SetActive(false);
        mainImage.gameObject.SetActive(false);

        // Set up organism selector dropdown
        if (organismSelector != null)
        {
            organismSelector.ClearOptions();
            List<TMPro.TMP_Dropdown.OptionData> options = new List<TMPro.TMP_Dropdown.OptionData>();
            options.Add(new TMPro.TMP_Dropdown.OptionData("Amoeba"));
            options.Add(new TMPro.TMP_Dropdown.OptionData("Euglena"));
            options.Add(new TMPro.TMP_Dropdown.OptionData("Paramecium"));
            organismSelector.AddOptions(options);

            // IMPORTANT: Set to Amoeba (index 0) first, then add the listener
            organismSelector.value = 0;

            // Set initial value based on DataManager if it exists
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
            }
        }

        // Show/hide organism managers based on selection
        UpdateOrganismManagerVisibility();

        // Start initial internet check with delay
        initialInternetCheckCoroutine = StartCoroutine(InitialInternetCheckWithDelay());

        // Add listener after setting initial value and loading the image
        if (organismSelector != null)
        {
            organismSelector.onValueChanged.AddListener(OnOrganismChanged);
        }
    }

    /// <summary>
    /// Performs an initial internet check with a delay before showing the default image
    /// </summary>
    private IEnumerator InitialInternetCheckWithDelay()
    {
        Debug.Log("Starting initial internet check with delay");

        // Check internet connection
        UnityWebRequest initialRequest = UnityWebRequest.Head("https://www.google.com");
        yield return initialRequest.SendWebRequest();

        hasInternetConnection = initialRequest.result != UnityWebRequest.Result.ConnectionError &&
                               initialRequest.result != UnityWebRequest.Result.ProtocolError;

        Debug.Log($"Initial internet check result after delay: {hasInternetConnection}");

        // Wait for 0.5 seconds to give time for internet connection to establish
        yield return new WaitForSeconds(0.5f);

        // Now that we've checked internet, load the appropriate image
        if (hasInternetConnection)
        {
            // Internet is available, load the online image
            Debug.Log("Internet available, loading online Amoeba image");
            ForceLoadAmoebaImage();
        }
        else
        {
            // No internet, show default image and Amoeba labels
            Debug.Log("No internet available after delay, showing default Amoeba image");

            // Force dropdown to Amoeba (index 0)
            if (organismSelector != null && organismSelector.value != 0)
            {
                organismSelector.value = 0;
            }

            // Make sure we're using the Amoeba data set
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
            }

            // Update organism manager visibility
            UpdateOrganismManagerVisibility();

            // Show default image
            ShowDefaultImage();

            // Show popup
            ShowNoInternetPopup();

            // Create Amoeba labels
            StartCoroutine(CreateAmoebaLabels());

            // Mark that we've done the initial load
            hasInitialLoad = true;
        }

        // Start the regular internet check coroutine
        internetCheckCoroutine = StartCoroutine(CheckInternetConnection());

        // Hide loading indicator now that we've determined what to show
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    // New method to force load the Amoeba image directly
    private void ForceLoadAmoebaImage()
    {
        Debug.Log("ForceLoadAmoebaImage called");

        // Set data manager
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
        }

        // Mark that we've done the initial load
        hasInitialLoad = true;

        // Try to load the image directly from StreamingAssets
        string filePath = Path.Combine(Application.streamingAssetsPath, "amoeba.json");
        Debug.Log("Loading Amoeba from: " + filePath);

        // Start a coroutine to load the image
        StartCoroutine(ForceLoadAmoebaImageCoroutine(filePath));
    }

    // Coroutine to load the Amoeba image directly
    private IEnumerator ForceLoadAmoebaImageCoroutine(string filePath)
    {
        Debug.Log("Starting ForceLoadAmoebaImageCoroutine");

        ImageData data = null;

        // Handle loading differently based on platform
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // WebGL and some mobile platforms need UnityWebRequest
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = www.downloadHandler.text;
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                    Debug.Log("Successfully parsed Amoeba JSON data");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing JSON: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("Error loading JSON file: " + www.error);
            }
        }
        else
        {
            // Desktop platforms can use File.ReadAllText
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                    Debug.Log("Successfully parsed Amoeba JSON data");
                }
                else
                {
                    Debug.LogError("File not found: " + filePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading JSON file: " + e.Message);
            }
        }

        // If we got the data, load the image
        if (data != null && !string.IsNullOrEmpty(data.imageUrl))
        {
            Debug.Log("Loading Amoeba image from URL: " + data.imageUrl);

            // Load image from URL
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(data.imageUrl);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Amoeba image loaded successfully");

                // Apply texture to the image
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(www);

                // Store the loaded texture
                loadedTexture = downloadedTexture;

                // Set the texture and make the image visible
                mainImage.texture = downloadedTexture;
                mainImage.gameObject.SetActive(true);
                defaultImage.gameObject.SetActive(false);

                // Mark that we're not using the default image
                isUsingDefaultImage = false;

                // Adjust aspect ratio
                AdjustImageAspectRatio(downloadedTexture);

                Debug.Log("Amoeba image is now displayed");

                // Create labels
                if (data.labels != null)
                {
                    Debug.Log($"Creating {data.labels.Count} labels for Amoeba");

                    // Clear any existing labels
                    foreach (Transform child in labelParent)
                    {
                        Destroy(child.gameObject);
                    }

                    // Create labels with a small delay between each for a nicer effect
                    for (int i = 0; i < data.labels.Count; i++)
                    {
                        yield return new WaitForSeconds(0.1f);
                        CreateLabelButton(data.labels[i]);
                    }
                    Debug.Log("Amoeba labels created: " + data.labels.Count);
                }

                // Hide loading indicator
                if (loadingIndicator != null)
                    loadingIndicator.SetActive(false);
            }
            else
            {
                Debug.LogError("Amoeba image load failed: " + www.error);

                // Show the default image
                ShowDefaultImage();

                // Create labels based on the Amoeba data
                yield return StartCoroutine(CreateAmoebaLabels());
            }
        }
        else
        {
            Debug.LogError("Failed to load Amoeba data or image URL is empty");
            ShowDefaultImage();
        }
    }

    /// <summary>
    /// This will Load the Default Amoeba when the Menu Scene loads.
    /// </summary>
    private void LoadAmoeba()
    {
        Debug.Log("LoadAmoeba called - forcing Amoeba load");

        // Force dropdown to Amoeba
        if (organismSelector != null)
        {
            organismSelector.value = 0;
        }

        // Set data manager
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
        }

        // Update organism manager visibility
        UpdateOrganismManagerVisibility();

        // Directly load the amoeba.json file
        StartCoroutine(LoadImageData("amoeba.json"));

        // Mark that we've done the initial load
        hasInitialLoad = true;
    }


    /// <summary>
    /// Called when the organism dropdown value changes.
    /// Handles switching between different organisms with animation.
    /// </summary>
    /// <param name="index">The index of the selected organism</param>
    void OnOrganismChanged(int index)
    {
        // If this is the first call during initialization, ignore it
        if (!hasInitialLoad)
        {
            return;
        }

        // If already animating, don't start another animation
        if (isAnimatingSwitch)
        {
            Debug.Log("Animation already in progress, ignoring change request");
            return;
        }

        Debug.Log($"OnOrganismChanged called with index: {index}");

        // Store the selected index for reference
        int selectedIndex = index;

        // If no internet connection, force back to Amoeba (index 0)
        if (!hasInternetConnection)
        {
            Debug.Log("No internet connection - forcing Amoeba selection");

            // Update popup message
            if (noInternetPopup != null && noInternetText != null)
            {
                noInternetText.text = "No internet connection available.\n\nShowing default Amoeba image with Amoeba labels.";
            }

            // Show the popup if it's not already showing
            if (noInternetPopup != null && !noInternetPopup.activeSelf)
            {
                ShowNoInternetPopup();
            }

            // Force dropdown back to Amoeba (index 0) without triggering this callback again
            if (organismSelector != null && organismSelector.value != 0)
            {
                // Remove the listener temporarily
                organismSelector.onValueChanged.RemoveListener(OnOrganismChanged);

                // Set value back to Amoeba
                organismSelector.value = 0;

                // Add the listener back
                organismSelector.onValueChanged.AddListener(OnOrganismChanged);
            }

            // Make sure we're using the Amoeba data set
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
            }

            // Update organism manager visibility to show Amoeba
            UpdateOrganismManagerVisibility();

            // Make sure we're using the default Amoeba image
            ShowDefaultImage();

            // Clear existing labels
            foreach (Transform child in labelParent)
            {
                Destroy(child.gameObject);
            }

            // Create Amoeba labels specifically
            StartCoroutine(CreateAmoebaLabels());

            return; // Skip the animation
        }

        // For normal internet connection, proceed with data set change
        if (DataManager.Instance != null)
        {
            // Make sure we're setting the correct data set
            switch (selectedIndex)
            {
                case 0:
                    DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
                    break;
                case 1:
                    DataManager.Instance.SetDataSet(DataManager.DataSet.Euglena);
                    break;
                case 2:
                    DataManager.Instance.SetDataSet(DataManager.DataSet.Paramecium);
                    break;
            }
        }

        // For normal internet connection, proceed with animation
        // Before loading the new organism, save the current one for animation
        if (mainImage.gameObject.activeSelf && mainImage.texture != null)
        {
            Debug.Log("Creating previous image copy for animation");
            // Existing code for creating previous image...
        }
        else
        {
            Debug.Log("No previous image to animate");
        }

        // Update which organism manager is visible
        UpdateOrganismManagerVisibility();

        // Load the new organism with animation
        StartCoroutine(AnimateOrganismSwitch());
    }

    /// <summary>
    /// Creates labels specifically for Amoeba when no internet is available
    /// </summary>
    IEnumerator CreateAmoebaLabels()
    {
        // Load the amoeba.json file specifically
        string filePath = Path.Combine(Application.streamingAssetsPath, "amoeba.json");
        Debug.Log("Loading Amoeba label data from: " + filePath);

        ImageData data = null;

        // Handle loading differently based on platform
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // WebGL and some mobile platforms need UnityWebRequest
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = www.downloadHandler.text;
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                    Debug.Log("Successfully parsed Amoeba JSON data");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing JSON: " + e.Message);
                }
            }
        }
        else
        {
            // Desktop platforms can use File.ReadAllText
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                    Debug.Log("Successfully parsed Amoeba JSON data");
                }
                else
                {
                    Debug.LogError("Amoeba JSON file not found: " + filePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading Amoeba JSON file: " + e.Message);
            }
        }

        // Create labels if we have data
        if (data != null && data.labels != null)
        {
            Debug.Log($"Creating {data.labels.Count} Amoeba labels for default image");

            // Make sure labelParent is empty before creating new labels
            foreach (Transform child in labelParent)
            {
                Destroy(child.gameObject);
            }

            // Create labels with a small delay between each for a nicer effect
            for (int i = 0; i < data.labels.Count; i++)
            {
                yield return new WaitForSeconds(0.1f);
                CreateLabelButton(data.labels[i]);
            }
            Debug.Log("Amoeba labels created: " + data.labels.Count);
        }
        else
        {
            Debug.LogError("Failed to load Amoeba label data");
        }

        // Hide loading indicator if it's still visible
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    /// <summary>
    /// Handles the animation when switching between organisms.
    /// Slides the current organism out to the left while sliding the new one in from the right.
    /// </summary>
    IEnumerator AnimateOrganismSwitch()
    {
        isAnimatingSwitch = true;
        Debug.Log("Starting organism switch animation");

        // Store the current image data for label creation after animation
        ImageData currentImageData = null;
        string fileName;
        int currentIndex = organismSelector.value;
        string selectedOption = organismSelector.options[currentIndex].text;

        // Determine which file to load
        switch (selectedOption)
        {
            case "Amoeba": fileName = "amoeba.json"; break;
            case "Euglena": fileName = "euglena.json"; break;
            case "Paramecium": fileName = "paramecium.json"; break;
            default: fileName = "amoeba.json"; break;
        }

        // Position the previous image at the center
        if (previousImage != null)
        {
            previousImage.rectTransform.anchoredPosition = Vector2.zero;
            Debug.Log("Previous image positioned at center");
        }

        // Make sure previous labels container has a RectTransform
        if (previousLabelsContainer != null)
        {
            RectTransform prevLabelsRect = previousLabelsContainer.GetComponent<RectTransform>();
            if (prevLabelsRect == null)
            {
                prevLabelsRect = previousLabelsContainer.gameObject.AddComponent<RectTransform>();
                Debug.Log("Added RectTransform to previous labels container");
            }

            // Position at center
            prevLabelsRect.anchoredPosition = Vector2.zero;
        }

        // Hide the main image initially
        if (mainImage.gameObject.activeSelf)
        {
            mainImage.gameObject.SetActive(false);
            Debug.Log("Main image hidden for animation");
        }

        // IMPORTANT: Clear the label parent before loading new organism
        // This ensures we don't have any leftover labels
        foreach (Transform child in labelParent)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("Cleared label parent");

        // IMPORTANT: Find and destroy any orphaned labels/lines in the scene
        LabelConnection[] orphanedConnections = FindObjectsOfType<LabelConnection>();
        foreach (var connection in orphanedConnections)
        {
            Debug.Log($"Destroying orphaned label: {connection.name}");
            Destroy(connection.gameObject);
        }

        // Also find and destroy any orphaned line objects that might not have LabelConnection components
        Image[] allImages = FindObjectsOfType<Image>();
        foreach (var img in allImages)
        {
            // Check if this might be a line object without a proper parent
            if (img.gameObject.name.Contains("Line") && img.transform.parent != labelParent)
            {
                Debug.Log($"Destroying orphaned line: {img.name}");
                Destroy(img.gameObject);
            }
        }

        // IMPORTANT: Clean up any previousLabels objects that might be accumulating
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name == "PreviousLabels" || obj.name.Contains("PreviousLabels"))
            {
                Debug.Log($"Destroying leftover previousLabels object: {obj.name}");
                Destroy(obj);
            }
        }

        // Start loading the new organism
        Debug.Log("Loading new organism");
        LoadCurrentOrganism();

        // Wait for the new organism to load
        Debug.Log("Waiting for organism to load");
        while (isLoading)
        {
            yield return null;
        }
        Debug.Log("New organism loaded");

        // Load the JSON data to get label information
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log("Loading label data from: " + filePath);

        // Load the JSON data
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // WebGL and some mobile platforms need UnityWebRequest
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = www.downloadHandler.text;
                    currentImageData = JsonUtility.FromJson<ImageData>(jsonText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing JSON: " + e.Message);
                }
            }
        }
        else
        {
            // Desktop platforms can use File.ReadAllText
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    currentImageData = JsonUtility.FromJson<ImageData>(jsonText);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading JSON file: " + e.Message);
            }
        }

        // IMPORTANT: Position the new image offscreen to the right BEFORE making it visible
        // Use a larger offscreen value to ensure it's completely off-screen
        mainImage.rectTransform.anchoredPosition = new Vector2(offscreenPositionX * 1.5f, 0);
        Debug.Log($"Positioned main image at x={offscreenPositionX * 1.5f}");

        // Now make it visible after positioning
        mainImage.gameObject.SetActive(true);
        Debug.Log("Main image activated");

        // Animate both images
        float elapsed = 0;
        Vector2 previousStartPos = Vector2.zero;
        // Use a larger negative value to ensure it moves completely off-screen
        Vector2 previousEndPos = new Vector2(-offscreenPositionX * 1.5f, 0);

        Vector2 newStartPos = new Vector2(offscreenPositionX * 1.5f, 0);
        Vector2 newEndPos = Vector2.zero;

        Debug.Log("Starting animation tween");
        while (elapsed < slideAnimationDuration)
        {
            float t = elapsed / slideAnimationDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // Move previous image to the left
            if (previousImage != null)
            {
                previousImage.rectTransform.anchoredPosition = Vector2.Lerp(previousStartPos, previousEndPos, smoothT);
            }

            // Also move the previous labels container
            if (previousLabelsContainer != null)
            {
                RectTransform prevLabelsRect = previousLabelsContainer.GetComponent<RectTransform>();
                if (prevLabelsRect != null)
                {
                    prevLabelsRect.anchoredPosition = Vector2.Lerp(previousStartPos, previousEndPos, smoothT);
                }
                else
                {
                    Debug.LogError("Previous labels container missing RectTransform during animation");
                }
            }

            // Move new image from right to center
            mainImage.rectTransform.anchoredPosition = Vector2.Lerp(newStartPos, newEndPos, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.Log("Animation tween complete");

        // IMPORTANT: Add a small delay before cleanup to ensure animation completes visually
        yield return new WaitForSeconds(0.1f);

        // Ensure final positions
        if (previousImage != null)
        {
            previousImage.rectTransform.anchoredPosition = previousEndPos;
        }

        if (previousLabelsContainer != null)
        {
            RectTransform prevLabelsRect = previousLabelsContainer.GetComponent<RectTransform>();
            if (prevLabelsRect != null)
            {
                prevLabelsRect.anchoredPosition = previousEndPos;
            }
        }

        mainImage.rectTransform.anchoredPosition = newEndPos;

        // Clean up previous image and labels
        Debug.Log("Cleaning up previous objects");
        if (previousImage != null)
        {
            Destroy(previousImage.gameObject);
            previousImage = null;
        }

        if (previousLabelsContainer != null)
        {
            Destroy(previousLabelsContainer.gameObject);
            previousLabelsContainer = null;
        }

        Debug.Log("Animation complete");

        // IMPORTANT: Now that the animation is complete and the new image is centered,
        // create the labels
        if (currentImageData != null && currentImageData.labels != null)
        {
            Debug.Log($"Creating {currentImageData.labels.Count} labels after animation");

            // Add a small pause before creating labels
            yield return new WaitForSeconds(0.2f);

            // Create labels with a small delay between each for a nicer effect
            for (int i = 0; i < currentImageData.labels.Count; i++)
            {
                yield return new WaitForSeconds(0.1f);
                CreateLabelButton(currentImageData.labels[i]);
            }

            Debug.Log("Labels created after animation");
        }
        else
        {
            Debug.LogError("No label data available after animation");
        }

        isAnimatingSwitch = false;
    }

    /// <summary>
    /// Shows only the currently selected organism's manager and hides others.
    /// </summary>
    void UpdateOrganismManagerVisibility()
    {
        int currentIndex = organismSelector.value;
        Debug.Log($"Updating organism visibility for dropdown index: {currentIndex}");

        for (int i = 0; i < organismManagers.Count; i++)
        {
            if (organismManagers[i] != null)
            {
                bool shouldBeActive = (i == currentIndex);
                organismManagers[i].gameObject.SetActive(shouldBeActive);
                Debug.Log($"Organism manager {i} ({organismManagers[i].organismName}) active: {shouldBeActive}");
            }
        }
    }

    /// <summary>
    /// Loads the currently selected organism's data and image.
    /// </summary>
    void LoadCurrentOrganism()
    {
        if (isLoading) return;

        string fileName;
        int currentIndex = organismSelector.value;
        string selectedOption = organismSelector.options[currentIndex].text;

        Debug.Log($"LoadCurrentOrganism called with option: {selectedOption}");

        // Use the text instead of index
        switch (selectedOption)
        {
            case "Amoeba": fileName = "amoeba.json"; break;
            case "Euglena": fileName = "euglena.json"; break;
            case "Paramecium": fileName = "paramecium.json"; break;
            default: fileName = "amoeba.json"; break;
        }

        Debug.Log($"Loading organism file: {fileName}");

        // During animation, we only want to load the image, not create labels yet
        if (isAnimatingSwitch)
        {
            StartCoroutine(LoadImageOnly(fileName));
        }
        else
        {
            StartCoroutine(LoadImageData(fileName));
        }
    }

    /// <summary>
    /// Loads only the image during animation without creating labels.
    /// </summary>
    /// <param name="jsonFileName">The JSON file containing image URL</param>
    IEnumerator LoadImageOnly(string jsonFileName)
    {
        isLoading = true;

        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // Hide info panel
        infoPanel.SetActive(false);

        ImageData data = null;
        string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        Debug.Log("Loading data from: " + filePath);

        // Handle loading differently based on platform
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // WebGL and some mobile platforms need UnityWebRequest
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = www.downloadHandler.text;
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing JSON: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("Error loading JSON file: " + www.error);
            }
        }
        else
        {
            // Desktop platforms can use File.ReadAllText
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                }
                else
                {
                    Debug.LogError("File not found: " + filePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading JSON file: " + e.Message);
            }
        }

        if (data != null)
        {
            // Load only the image, not the labels
            Debug.Log("Loading image from URL: " + data.imageUrl);

            // Reset image load status
            imageLoadSuccess = false;

            // Start timeout coroutine
            if (timeoutCoroutine != null)
                StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = StartCoroutine(ImageLoadTimeout());

            // Load image from URL
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(data.imageUrl);
            yield return www.SendWebRequest();

            // Hide loading indicator
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Image loaded successfully");

                // Mark as successful before applying texture
                imageLoadSuccess = true;

                // Cancel timeout coroutine since we succeeded
                if (timeoutCoroutine != null)
                    StopCoroutine(timeoutCoroutine);

                // Apply texture to the image
                loadedTexture = DownloadHandlerTexture.GetContent(www);

                // Set the texture without showing it yet
                mainImage.texture = loadedTexture;
                AdjustImageAspectRatio(loadedTexture);
                Debug.Log("Set texture for animation");
            }
            else
            {
                Debug.LogError("Image load failed: " + www.error);
                ShowDefaultImage();
            }
        }

        isLoading = false;
    }

    /// <summary>
    /// Gets the current organism manager based on dropdown selection.
    /// </summary>
    /// <returns>The active OrganismLabelManager or null if not found</returns>
    private OrganismLabelManager GetCurrentOrganismManager()
    {
        int currentIndex = organismSelector.value;
        Debug.Log($"Getting organism manager for dropdown index: {currentIndex}");

        if (currentIndex >= 0 && currentIndex < organismManagers.Count)
        {
            OrganismLabelManager manager = organismManagers[currentIndex];
            Debug.Log($"Found manager: {manager.organismName}");
            return manager;
        }
        Debug.LogError("No organism manager found for index: " + currentIndex);
        return null;
    }

    /// <summary>
    /// Handles the zoom animation when transitioning from the menu scene.
    /// </summary>
    IEnumerator ZoomInImage()
    {
        // Target position and scale
        Vector3 targetPos = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Vector3 targetScale = Vector3.one;

        float duration = 1.5f;
        float elapsed = 0;

        Vector3 startPos = imageContainer.position;
        Vector3 startScale = imageContainer.localScale;

        Debug.Log($"Starting zoom from {startPos} to {targetPos}");

        // Animate zoom
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Use easing for smoother animation
            float smoothT = Mathf.SmoothStep(0, 1, t);

            imageContainer.position = Vector3.Lerp(startPos, targetPos, smoothT);
            imageContainer.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final position and scale
        imageContainer.position = targetPos;
        imageContainer.localScale = targetScale;

        Debug.Log("Zoom animation complete");

        // Mark transition as complete
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.CompleteTransition();
    }

    /// <summary>
    /// Loads image data and sets up labels for the selected organism.
    /// </summary>
    /// <param name="jsonFileName">The JSON file containing organism data</param>
    IEnumerator LoadImageData(string jsonFileName)
    {
        isLoading = true;

        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // Clear existing labels (if not animating)
        if (!isAnimatingSwitch)
        {
            foreach (Transform child in labelParent)
                Destroy(child.gameObject);
        }

        // Hide info panel
        infoPanel.SetActive(false);

        ImageData data = null;
        string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        Debug.Log("Loading data from: " + filePath);

        // Handle loading differently based on platform
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // WebGL and some mobile platforms need UnityWebRequest
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = www.downloadHandler.text;
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing JSON: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("Error loading JSON file: " + www.error);
            }
        }
        else
        {
            // Desktop platforms can use File.ReadAllText
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                }
                else
                {
                    Debug.LogError("File not found: " + filePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading JSON file: " + e.Message);
            }
        }

        // Now load the image and setup labels
        yield return StartCoroutine(LoadImageAndSetupLabels(data));

        isLoading = false;
    }

    /// <summary>
    /// Loads the image and creates interactive labels.
    /// </summary>
    /// <param name="data">The ImageData containing URL and label information</param>
    IEnumerator LoadImageAndSetupLabels(ImageData data)
    {
        // Safety check for null data
        if (data == null)
        {
            Debug.LogError("ImageData is null in LoadImageAndSetupLabels");
            ShowDefaultImage();
            yield return StartCoroutine(CreateAmoebaLabels());
            yield break;
        }

        Debug.Log("Loading image from URL: " + data.imageUrl);

        // Reset image load status
        imageLoadSuccess = false;

        // Start timeout coroutine
        if (timeoutCoroutine != null)
            StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(ImageLoadTimeout());

        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // If we already know there's no internet, don't even try to load the image
        if (!hasInternetConnection)
        {
            Debug.Log("No internet connection available, using default Amoeba image and labels");

            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            // Force dropdown to Amoeba (index 0) without triggering callback
            if (organismSelector != null && organismSelector.value != 0)
            {
                // Remove the listener temporarily
                organismSelector.onValueChanged.RemoveListener(OnOrganismChanged);

                // Set value back to Amoeba
                organismSelector.value = 0;

                // Add the listener back
                organismSelector.onValueChanged.AddListener(OnOrganismChanged);
            }

            // Make sure we're using the Amoeba data set
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
            }

            // Update organism manager visibility
            UpdateOrganismManagerVisibility();

            // Show the default Amoeba image
            ShowDefaultImage();

            // Create Amoeba labels specifically
            yield return StartCoroutine(CreateAmoebaLabels());

            yield break;
        }

        // Load image from URL
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(data.imageUrl);
        yield return www.SendWebRequest();

        // Hide loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Image loaded successfully");

            // Mark as successful before applying texture
            imageLoadSuccess = true;

            // Cancel timeout coroutine since we succeeded
            if (timeoutCoroutine != null)
                StopCoroutine(timeoutCoroutine);

            // Apply texture to the image
            Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(www);

            // Store the loaded texture
            loadedTexture = downloadedTexture;

            // Show the loaded image
            if (isAnimatingSwitch)
            {
                // For animation, just set the texture without fading
                mainImage.texture = downloadedTexture;
                AdjustImageAspectRatio(downloadedTexture);
                Debug.Log("Set texture for animation");
            }
            else
            {
                // Normal fade effect for initial load
                yield return StartCoroutine(ShowLoadedImage(downloadedTexture));

                // Make sure labelParent is empty before creating new labels
                foreach (Transform child in labelParent)
                {
                    Destroy(child.gameObject);
                }
                Debug.Log("Cleared label parent before creating new labels");

                // Create labels with a small delay between each for a nicer effect
                if (data.labels != null)
                {
                    Debug.Log($"Creating {data.labels.Count} labels");
                    for (int i = 0; i < data.labels.Count; i++)
                    {
                        yield return new WaitForSeconds(0.1f);
                        CreateLabelButton(data.labels[i]);
                    }
                    Debug.Log("Labels created: " + data.labels.Count);
                }
                else
                {
                    Debug.LogError("No labels data available");
                }
            }
        }
        else
        {
            Debug.LogError("Image load failed: " + www.error);

            // Let the timeout handle showing the default image
            imageLoadSuccess = false;

            if (timeoutCoroutine != null)
                StopCoroutine(timeoutCoroutine);

            ShowDefaultImage();

            // Create labels based on the selected organism
            yield return StartCoroutine(CreateLabelsForCurrentOrganism());
        }
    }

    /// <summary>
    /// Creates labels for the currently selected organism
    /// </summary>
    IEnumerator CreateLabelsForCurrentOrganism()
    {
        // Determine which file to load based on dropdown selection
        string fileName;
        int currentIndex = organismSelector.value;
        string selectedOption = organismSelector.options[currentIndex].text;

        switch (selectedOption)
        {
            case "Amoeba": fileName = "amoeba.json"; break;
            case "Euglena": fileName = "euglena.json"; break;
            case "Paramecium": fileName = "paramecium.json"; break;
            default: fileName = "amoeba.json"; break;
        }

        // Load the JSON data
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log("Loading label data from: " + filePath);

        ImageData data = null;

        // Handle loading differently based on platform
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // WebGL and some mobile platforms need UnityWebRequest
            UnityWebRequest www = UnityWebRequest.Get(filePath);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = www.downloadHandler.text;
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing JSON: " + e.Message);
                }
            }
        }
        else
        {
            // Desktop platforms can use File.ReadAllText
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    data = JsonUtility.FromJson<ImageData>(jsonText);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading JSON file: " + e.Message);
            }
        }

        // Create labels if we have data
        if (data != null && data.labels != null)
        {
            Debug.Log($"Creating {data.labels.Count} labels for default image");

            // Make sure labelParent is empty before creating new labels
            foreach (Transform child in labelParent)
            {
                Destroy(child.gameObject);
            }

            // Create labels with a small delay between each for a nicer effect
            for (int i = 0; i < data.labels.Count; i++)
            {
                yield return new WaitForSeconds(0.1f);
                CreateLabelButton(data.labels[i]);
            }
            Debug.Log("Labels created: " + data.labels.Count);
        }
    }

    /// <summary>
    /// Handles timeout for image loading and shows default image if needed.
    /// </summary>
    IEnumerator ImageLoadTimeout()
    {
        yield return new WaitForSeconds(imageLoadTimeout);

        // If image hasn't loaded successfully by now, show default image
        if (!imageLoadSuccess)
        {
            Debug.LogWarning("Image loading timed out after " + imageLoadTimeout + " seconds. Using default image.");
            ShowDefaultImage();
        }
    }

    /// <summary>
    /// Transitions from default image to loaded image with fade effect.
    /// </summary>
    /// <param name="loadedTexture">The successfully loaded texture</param>
    IEnumerator TransitionFromDefaultToLoaded(Texture2D loadedTexture)
    {
        // Apply texture to main image but keep it invisible
        mainImage.texture = loadedTexture;
        mainImage.gameObject.SetActive(true);

        // Adjust aspect ratio
        AdjustImageAspectRatio(loadedTexture);

        // Get canvas groups
        CanvasGroup defaultCG = defaultImage.GetComponent<CanvasGroup>();
        CanvasGroup mainCG = mainImage.GetComponent<CanvasGroup>();

        if (defaultCG != null && mainCG != null)
        {
            // Make sure main image starts invisible
            mainCG.alpha = 0;

            // Fade out default while fading in main
            float duration = 0.75f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                defaultCG.alpha = Mathf.Lerp(1, 0, t);
                mainCG.alpha = Mathf.Lerp(0, 1, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure final state
            defaultCG.alpha = 0;
            mainCG.alpha = 1;
        }

        // Hide default image
        defaultImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// Adjusts the image aspect ratio to match the original texture.
    /// </summary>
    /// <param name="texture">The texture to match aspect ratio with</param>
    void AdjustImageAspectRatio(Texture2D texture)
    {
        // Adjust aspect ratio if needed
        float aspectRatio = (float)texture.width / texture.height;
        RectTransform mainImageRect = mainImage.GetComponent<RectTransform>();
        if (mainImageRect != null)
        {
            float width = mainImageRect.rect.width;
            mainImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width / aspectRatio);
        }
    }

    /// <summary>
    /// Shows the default image when loading fails or times out.
    /// </summary>
    void ShowDefaultImage()
    {
        Debug.Log("ShowDefaultImage called - showing default Amoeba image");

        // Make sure default image has a texture
        if (defaultImage.texture == null)
        {
            Debug.LogError("Default image has no texture assigned!");
        }

        // Hide main image and show default
        mainImage.gameObject.SetActive(false);
        defaultImage.gameObject.SetActive(true);

        // Mark that we're using the default image
        isUsingDefaultImage = true;

        // Animate the appearance
        CanvasGroup defaultCG = defaultImage.GetComponent<CanvasGroup>();
        if (defaultCG != null)
        {
            defaultCG.alpha = 0;
            StartCoroutine(FadeCanvasGroup(defaultCG, 0, 1, 0.5f));
        }
        else
        {
            Debug.LogWarning("Default image has no CanvasGroup component");
            // Add one if missing
            defaultCG = defaultImage.gameObject.AddComponent<CanvasGroup>();
            defaultCG.alpha = 1;
        }

        // If no internet connection, show the popup
        if (!hasInternetConnection)
        {
            ShowNoInternetPopup();
        }

        Debug.Log("Default Amoeba image shown");
    }

    /// <summary>
    /// Shows the successfully loaded image with fade animation.
    /// </summary>
    /// <param name="texture">The texture to display</param>
    IEnumerator ShowLoadedImage(Texture2D texture)
    {
        Debug.Log("ShowLoadedImage called with texture: " + (texture != null ? texture.width + "x" + texture.height : "null"));

        if (texture == null)
        {
            Debug.LogError("Attempted to show null texture");
            ShowDefaultImage();
            yield break;
        }

        // Apply texture to the image
        mainImage.texture = texture;

        // Make sure main image is active and default is hidden
        mainImage.gameObject.SetActive(true);
        defaultImage.gameObject.SetActive(false);

        // Mark that we're no longer using the default image
        isUsingDefaultImage = false;

        // Adjust aspect ratio
        AdjustImageAspectRatio(texture);

        // Animate the appearance
        CanvasGroup mainCG = mainImage.GetComponent<CanvasGroup>();
        if (mainCG != null)
        {
            mainCG.alpha = 0;
            yield return StartCoroutine(FadeCanvasGroup(mainCG, 0, 1, 0.5f));
        }
        else
        {
            Debug.LogWarning("Main image has no CanvasGroup component");
        }

        Debug.Log("Showing loaded image complete");
    }

    /// <summary>
    /// Generic fade animation for CanvasGroup components.
    /// </summary>
    /// <param name="cg">The CanvasGroup to fade</param>
    /// <param name="from">Starting alpha value</param>
    /// <param name="to">Target alpha value</param>
    /// <param name="duration">Duration of fade in seconds</param>
    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cg.alpha = to;
    }

    /// <summary>
    /// Creates an interactive label button at the specified position.
    /// </summary>
    /// <param name="label">The label data containing position and text information</param>
    void CreateLabelButton(LabelData label)
    {
        // Create button instance and ensure it's properly parented
        GameObject btnObj = Instantiate(labelButtonPrefab, labelParent);
        btnObj.transform.SetParent(labelParent, false); // Explicitly set parent again to be sure

        // Get the current organism manager
        OrganismLabelManager manager = GetCurrentOrganismManager();

        // Set the label text
        TMPro.TextMeshProUGUI tmpText = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        Text standardText = btnObj.GetComponentInChildren<Text>();

        if (tmpText != null)
            tmpText.text = label.labelText;
        else if (standardText != null)
            standardText.text = label.labelText;
        else
            Debug.LogError("No text component found on label button");

        // Get image rect for positioning
        RectTransform imgRect = mainImage.GetComponent<RectTransform>();
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        RectTransform canvasRect = labelParent.GetComponent<RectTransform>();

        // Position the button
        Vector2 btnPos;

        if (manager != null && manager.HasLabelPosition(label.labelText))
        {
            // Use position from the manager if available
            btnPos = manager.GetLabelPosition(label.labelText, canvasRect, mainCamera);
        }
        else
        {
            // Fallback to normalized position from JSON
            btnPos = new Vector2(
                (label.normalizedPos.x - 0.5f) * imgRect.rect.width,
                (label.normalizedPos.y - 0.5f) * imgRect.rect.height
            );
        }

        btnRect.anchoredPosition = btnPos;

        // Get line start position
        Vector2 lineStartPos;

        if (manager != null && manager.HasLineStartPosition(label.labelText))
        {
            // Use position from the manager if available
            lineStartPos = manager.GetLineStartPosition(label.labelText, canvasRect, mainCamera);
        }
        else
        {
            // Fallback to a position near the center of the image
            lineStartPos = new Vector2(0, 0);
            Debug.LogWarning($"No line start position found for {label.labelText}. Using default.");
        }

        // Draw line from start position to button and ensure it's properly parented
        GameObject lineObj = DrawLine(lineStartPos, btnPos, labelParent);

        // Ensure the line is properly parented
        if (lineObj.transform.parent != labelParent)
        {
            lineObj.transform.SetParent(labelParent, false);
        }

        // Make sure the line has a Canvas Group component
        CanvasGroup lineCanvasGroup = lineObj.GetComponent<CanvasGroup>();
        if (lineCanvasGroup == null)
        {
            lineCanvasGroup = lineObj.AddComponent<CanvasGroup>();
        }
        lineCanvasGroup.alpha = 0; // Start invisible

        // Get the line image component
        Image lineImage = lineObj.GetComponent<Image>();
        if (lineImage == null)
        {
            Debug.LogWarning("Line object missing Image component, adding one");
            lineImage = lineObj.AddComponent<Image>();
        }

        // Store reference to the line
        LabelConnection connection = btnObj.AddComponent<LabelConnection>();
        connection.lineImage = lineImage;

        // Add click event
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            string title = label.labelText;
            string info = label.infoText;
            btn.onClick.AddListener(() => ShowInfoPanel(title, info));
        }

        // IMPORTANT: Set the sibling index to ensure line is behind button
        // Lower sibling index means rendered earlier (behind)
        lineObj.transform.SetSiblingIndex(lineObj.transform.GetSiblingIndex() - 1);

        // Or even more safely, make sure line is first (bottom-most) and button is last (top-most)
        lineObj.transform.SetAsFirstSibling();
        btnObj.transform.SetAsLastSibling();

        // Animate the button and line appearance
        StartCoroutine(AnimateLabelAppearance(btnObj, lineObj));
    }

    /// <summary>
    /// Animates the appearance of a label and its connecting line.
    /// </summary>
    /// <param name="labelObj">The label button GameObject</param>
    /// <param name="lineObj">The connecting line GameObject</param>
    IEnumerator AnimateLabelAppearance(GameObject labelObj, GameObject lineObj)
    {
        // Safety check - if objects were destroyed, exit early
        if (labelObj == null || lineObj == null)
        {
            Debug.LogWarning("Attempted to animate destroyed label or line objects");
            yield break;
        }

        // Get components
        CanvasGroup labelCanvasGroup = labelObj.GetComponent<CanvasGroup>();
        if (labelCanvasGroup == null)
        {
            labelCanvasGroup = labelObj.AddComponent<CanvasGroup>();
        }

        // Make sure the line object has a canvas group
        CanvasGroup lineCanvasGroup = null;
        if (lineObj != null) // Check again to be safe
        {
            lineCanvasGroup = lineObj.GetComponent<CanvasGroup>();
            if (lineCanvasGroup == null)
            {
                lineCanvasGroup = lineObj.AddComponent<CanvasGroup>();
            }

            // Set initial alpha to 0 for the line
            lineCanvasGroup.alpha = 0;
        }

        // Set initial states
        if (labelCanvasGroup != null)
        {
            labelCanvasGroup.alpha = 0;
        }

        RectTransform labelRect = null;
        Vector3 originalScale = Vector3.one;

        if (labelObj != null) // Check again to be safe
        {
            labelRect = labelObj.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                originalScale = labelRect.localScale;
                labelRect.localScale = originalScale * 0.5f;
            }
        }

        // Get the line's RectTransform and Image
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        Image lineImage = lineObj.GetComponent<Image>();

        if (lineRect == null || lineImage == null)
        {
            Debug.LogWarning("Line missing RectTransform or Image component");
            yield break;
        }

        // Store original line properties
        Vector2 originalSize = lineRect.sizeDelta;
        Vector2 originalPos = lineRect.anchoredPosition;
        Quaternion originalRotation = lineRect.rotation;

        // Calculate the start and end positions from the line's current configuration
        Vector2 startPos = lineRect.anchoredPosition;
        float angle = lineRect.rotation.eulerAngles.z * Mathf.Deg2Rad;
        float distance = lineRect.sizeDelta.x;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 endPos = startPos + direction * distance;

        // Set initial line size to zero length but keep the position and rotation
        lineRect.sizeDelta = new Vector2(0, originalSize.y);

        // Make the line visible immediately (but with zero length)
        lineCanvasGroup.alpha = 1;

        // First animate the line growing
        float lineDuration = 0.4f;
        float elapsed = 0;

        while (elapsed < lineDuration)
        {
            // Safety check - if objects were destroyed during animation, exit early
            if (lineObj == null || lineCanvasGroup == null || lineRect == null)
            {
                Debug.LogWarning("Line object was destroyed during animation");
                yield break;
            }

            float t = elapsed / lineDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // Animate the line length
            lineRect.sizeDelta = new Vector2(originalSize.x * smoothT, originalSize.y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Safety check again
        if (lineCanvasGroup != null && lineRect != null)
        {
            // Ensure final line state
            lineRect.sizeDelta = originalSize;
            lineCanvasGroup.alpha = 1;
        }

        // Then animate the button
        float buttonDuration = 0.3f;
        elapsed = 0;

        while (elapsed < buttonDuration)
        {
            // Safety check - if objects were destroyed during animation, exit early
            if (labelObj == null || labelCanvasGroup == null || labelRect == null)
            {
                Debug.LogWarning("Label object was destroyed during animation");
                yield break;
            }
            float t = elapsed / buttonDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            labelCanvasGroup.alpha = smoothT;
            labelRect.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final safety check before setting final state
        if (labelCanvasGroup != null && labelRect != null)
        {
            // Ensure final state
            labelCanvasGroup.alpha = 1;
            labelRect.localScale = originalScale;
        }
    }

    /// <summary>
    /// Shows the info panel with label details and highlights the selected label.
    /// </summary>
    /// <param name="title">The title to display</param>
    /// <param name="info">The detailed information to display</param>
    public void ShowInfoPanel(string title, string info)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }

        infoTitleText.text = title;
        infoDescText.text = info;

        // Show the panel
        infoPanel.SetActive(true);

        // Reset all lines
        LabelConnection[] connections = FindObjectsOfType<LabelConnection>();
        foreach (var connection in connections)
        {
            connection.ResetLine();
        }

        // Find and highlight the selected label's line
        foreach (var connection in connections)
        {
            TMPro.TextMeshProUGUI tmpText = connection.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Text standardText = connection.GetComponentInChildren<Text>();

            string buttonText = "";
            if (tmpText != null)
                buttonText = tmpText.text;
            else if (standardText != null)
                buttonText = standardText.text;

            if (buttonText == title)
            {
                connection.HighlightLine();
                break;
            }
        }

        // Animate the panel appearance
        CanvasGroup cg = infoPanel.GetComponent<CanvasGroup>();
        if (cg != null)
            StartCoroutine(FadeInPanel(cg));
    }

    /// <summary>
    /// Hides the info panel when the close button is clicked.
    /// </summary>
    public void HideInfoPanel()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }
        infoPanel.SetActive(false);
    }

    /// <summary>
    /// Fades in the info panel with animation.
    /// </summary>
    /// <param name="cg">The CanvasGroup component of the panel</param>
    IEnumerator FadeInPanel(CanvasGroup cg)
    {
        cg.alpha = 0;
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            cg.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cg.alpha = 1;
    }

    /// <summary>
    /// Creates a line between two points in UI space.
    /// </summary>
    /// <param name="startPos">Starting position of the line</param>
    /// <param name="endPos">Ending position of the line</param>
    /// <param name="parent">Parent transform for the line</param>
    /// <param name="thickness">Thickness of the line</param>
    /// <param name="color">Color of the line</param>
    /// <returns>The created line GameObject</returns>
    private GameObject DrawLine(Vector2 startPos, Vector2 endPos, Transform parent, float thickness = 10f, Color color = default)
    {
        // Use black as default color if not specified
        if (color == default)
            color = Color.black;

        // Create line object
        GameObject lineObj = Instantiate(linePrefab);

        // Explicitly set parent with worldPositionStays = false
        lineObj.transform.SetParent(parent, false);

        // Verify parent was set correctly
        if (lineObj.transform.parent != parent)
        {
            Debug.LogError($"Line parenting failed! Current parent: {lineObj.transform.parent?.name ?? "null"}");
            // Force parent again
            lineObj.transform.parent = parent;
        }

        // Ensure the line has a RectTransform
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        if (lineRect == null)
        {
            lineRect = lineObj.AddComponent<RectTransform>();
        }

        // Ensure the line has an Image component
        Image lineImage = lineObj.GetComponent<Image>();
        if (lineImage == null)
        {
            lineImage = lineObj.AddComponent<Image>();
        }

        // Ensure the line has a Canvas Group component
        CanvasGroup canvasGroup = lineObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = lineObj.AddComponent<CanvasGroup>();
        }

        // Set color
        if (lineImage != null)
            lineImage.color = color;

        // Position at start
        lineRect.anchoredPosition = startPos;

        // Calculate distance and angle
        Vector2 direction = endPos - startPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Set size and rotation
        lineRect.sizeDelta = new Vector2(distance, thickness);
        lineRect.rotation = Quaternion.Euler(0, 0, angle);

        return lineObj;
    }

    /// <summary>
    /// Logs information about available organism managers for debugging.
    /// </summary>
    void DebugOrganismManagers()
    {
        Debug.Log("=== Organism Managers ===");
        for (int i = 0; i < organismManagers.Count; i++)
        {
            if (organismManagers[i] != null)
            {
                Debug.Log($"Manager {i}: {organismManagers[i].organismName}");
            }
            else
            {
                Debug.Log($"Manager {i}: null");
            }
        }
        Debug.Log("=== Dropdown Options ===");
        for (int i = 0; i < organismSelector.options.Count; i++)
        {
            Debug.Log($"Option {i}: {organismSelector.options[i].text}");
        }
        Debug.Log("========================");
    }
    void OnDisable()
    {
        // Stop the internet check coroutine
        if (internetCheckCoroutine != null)
        {
            StopCoroutine(internetCheckCoroutine);
            internetCheckCoroutine = null;
        }

        // Stop the initial internet check coroutine if it's still running
        if (initialInternetCheckCoroutine != null)
        {
            StopCoroutine(initialInternetCheckCoroutine);
            initialInternetCheckCoroutine = null;
        }

        // Clean up any orphaned objects when the component is disabled or scene is unloaded
        if (previousImage != null)
        {
            Destroy(previousImage.gameObject);
            previousImage = null;
        }

        if (previousLabelsContainer != null)
        {
            Destroy(previousLabelsContainer.gameObject);
            previousLabelsContainer = null;
        }

        // Find and destroy any orphaned labels/lines
        LabelConnection[] connections = FindObjectsOfType<LabelConnection>();
        foreach (var connection in connections)
        {
            Destroy(connection.gameObject);
        }

        // Find and destroy any orphaned line objects
        Image[] allImages = FindObjectsOfType<Image>();
        foreach (var img in allImages)
        {
            if (img.gameObject.name.Contains("Line") && img.transform.parent != labelParent)
            {
                Destroy(img.gameObject);
            }
        }

        // Find and destroy any previousLabels objects
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name == "PreviousLabels" || obj.name.Contains("PreviousLabels"))
            {
                Destroy(obj);
            }
        }
    }

    void OnDestroy()
    {
        // Clean up any orphaned labels/lines
        LabelConnection[] connections = FindObjectsOfType<LabelConnection>();
        foreach (var connection in connections)
        {
            Destroy(connection.gameObject);
        }
    }

    #region Internet Availability
    /// <summary>
    /// Periodically checks for internet connection
    /// </summary>
    IEnumerator CheckInternetConnection()
    {
        WaitForSeconds waitInterval = new WaitForSeconds(5f); // Check every 5 seconds

        // Continue checking periodically
        while (true)
        {
            bool previousConnectionState = hasInternetConnection;

            // Check connection by trying to reach a reliable server
            UnityWebRequest request = UnityWebRequest.Head("https://www.google.com");
            yield return request.SendWebRequest();

            hasInternetConnection = request.result != UnityWebRequest.Result.ConnectionError &&
                                   request.result != UnityWebRequest.Result.ProtocolError;

            Debug.Log($"Internet connection check: {hasInternetConnection}");

            // If connection state changed
            if (previousConnectionState != hasInternetConnection)
            {
                if (hasInternetConnection)
                {
                    // Internet is back - hide popup
                    Debug.Log("Internet connection restored");
                    HideNoInternetPopup();

                    // If we were using default image, reload the current organism with actual image
                    if (isUsingDefaultImage)
                    {
                        Debug.Log("Reloading organism with actual image");
                        LoadCurrentOrganism();
                    }
                }
                else
                {
                    // Lost internet - force back to Amoeba
                    Debug.Log("Internet connection lost");

                    // Force dropdown to Amoeba (index 0) without triggering callback
                    if (organismSelector != null && organismSelector.value != 0)
                    {
                        // Remove the listener temporarily
                        organismSelector.onValueChanged.RemoveListener(OnOrganismChanged);

                        // Set value back to Amoeba
                        organismSelector.value = 0;

                        // Add the listener back
                        organismSelector.onValueChanged.AddListener(OnOrganismChanged);
                    }

                    // Make sure we're using the Amoeba data set
                    if (DataManager.Instance != null)
                    {
                        DataManager.Instance.SetDataSet(DataManager.DataSet.Amoeba);
                    }

                    // Update organism manager visibility
                    UpdateOrganismManagerVisibility();

                    // Show default image
                    ShowDefaultImage();

                    // Show popup
                    ShowNoInternetPopup();

                    // Clear existing labels
                    foreach (Transform child in labelParent)
                    {
                        Destroy(child.gameObject);
                    }

                    // Create Amoeba labels
                    StartCoroutine(CreateAmoebaLabels());
                }
            }

            yield return waitInterval;
        }
    }

    /// <summary>
    /// Shows the no internet connection popup
    /// </summary>
    void ShowNoInternetPopup()
    {
        if (noInternetPopup != null)
        {
            noInternetPopup.SetActive(true);

            // Always show message about Amoeba
            if (noInternetText != null)
            {
                noInternetText.text = "No internet connection available.\n\nShowing default Amoeba image with Amoeba labels.";
            }

            // Animate the popup appearance
            CanvasGroup cg = noInternetPopup.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                StartCoroutine(FadeCanvasGroup(cg, 0, 1, 0.5f));
            }
            else
            {
                // Add canvas group if needed
                cg = noInternetPopup.AddComponent<CanvasGroup>();
                cg.alpha = 1;
            }
        }
    }

    /// <summary>
    /// Hides the no internet connection popup
    /// </summary>
    void HideNoInternetPopup()
    {
        if (noInternetPopup != null && noInternetPopup.activeSelf)
        {
            // Animate the popup disappearance
            CanvasGroup cg = noInternetPopup.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                StartCoroutine(FadeOutAndDisable(noInternetPopup, cg));
            }
            else
            {
                noInternetPopup.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Fades out a canvas group and then disables the GameObject
    /// </summary>
    IEnumerator FadeOutAndDisable(GameObject target, CanvasGroup cg)
    {
        yield return StartCoroutine(FadeCanvasGroup(cg, cg.alpha, 0, 0.5f));
        target.SetActive(false);
    }
    #endregion
}
