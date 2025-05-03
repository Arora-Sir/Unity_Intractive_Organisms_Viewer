using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages navigation buttons (Home and Power) that persist across scenes.
/// Implemented as a singleton to ensure only one instance exists throughout the application.
/// The Home button is only shown in game scenes, not in the menu.
/// </summary>
public class NavigationController : MonoBehaviour
{
    [Header("Button References")]
    public Button homeButton;    // Reference to the UI button that returns to the main menu
    public Button powerButton;   // Reference to the UI button that quits the application

    [Header("Configuration")]
    public string menuSceneName = "Menu";       // Name of the menu scene to return to
    public string gameSceneNames = "Game";      // Name of the game scene (determines when Home button is visible)
    public float transitionFadeDuration = 0.5f; // Duration for scene transition fade effects

    // Singleton instance
    private static NavigationController _instance;
    public static NavigationController Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Sets up the singleton pattern and ensures this object persists between scenes.
    /// </summary>
    private void Awake()
    {
        // Singleton pattern implementation - destroy duplicates
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Set this as the singleton instance
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Register for scene change events to update button visibility
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Initializes button listeners and visibility on start.
    /// </summary>
    private void Start()
    {
        // Set up button click listeners
        SetupButtons();

        // Set initial button visibility based on the current scene
        UpdateButtonVisibility(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Attaches click listeners to the navigation buttons.
    /// </summary>
    private void SetupButtons()
    {
        if (homeButton != null)
        {
            homeButton.onClick.AddListener(ReturnToMenu);
        }
        else
        {
            Debug.LogWarning("Home button reference not set in NavigationController");
        }

        if (powerButton != null)
        {
            powerButton.onClick.AddListener(QuitApplication);
        }
        else
        {
            Debug.LogWarning("Power button reference not set in NavigationController");
        }
    }

    /// <summary>
    /// Called when a new scene is loaded to update button visibility.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Update button visibility based on the newly loaded scene
        UpdateButtonVisibility(scene.name);
    }

    /// <summary>
    /// Shows or hides buttons based on the current scene.
    /// </summary>
    /// <param name="sceneName">The name of the current scene</param>
    private void UpdateButtonVisibility(string sceneName)
    {
        // Home button is only visible in game scenes, not in menu
        if (homeButton != null)
        {
            bool isGameScene = (sceneName == "Game");
            homeButton.gameObject.SetActive(isGameScene);
        }

        // Power button is always visible
        if (powerButton != null)
        {
            powerButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Returns to the menu scene when home button is clicked.
    /// </summary>
    public void ReturnToMenu()
    {
        Debug.Log("Returning to menu scene: " + menuSceneName);

        // Play click sound if audio manager exists
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }

        // Start transition to menu scene
        StartCoroutine(TransitionToScene(menuSceneName));
    }

    /// <summary>
    /// Quits the application when power button is clicked.
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("Quitting application");

        // Play click sound if audio manager exists
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }

        // Quit the application (different handling for editor vs build)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Handles scene transition with fade effect.
    /// </summary>
    /// <param name="sceneName">The name of the scene to transition to</param>
    private IEnumerator TransitionToScene(string sceneName)
    {
        // Disable buttons during transition to prevent multiple clicks
        if (homeButton != null) homeButton.interactable = false;
        if (powerButton != null) powerButton.interactable = false;

        // Small delay before scene transition
        yield return new WaitForSeconds(0.2f);

        // Load the target scene
        SceneManager.LoadScene(sceneName);

        // Re-enable buttons after scene load
        if (homeButton != null) homeButton.interactable = true;
        if (powerButton != null) powerButton.interactable = true;
    }

    /// <summary>
    /// Clean up event subscriptions and references when destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Remove button listeners to prevent memory leaks
        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(ReturnToMenu);
        }

        if (powerButton != null)
        {
            powerButton.onClick.RemoveListener(QuitApplication);
        }

        // Clear singleton reference if this is the current instance
        if (_instance == this)
        {
            _instance = null;
        }
    }
}