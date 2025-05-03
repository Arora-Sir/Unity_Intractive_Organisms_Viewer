using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages the main menu UI and handles scene transitions when the play button is clicked.
/// Controls animations, sound effects, and UI fading during the transition to gameplay.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main play button that starts the game")]
    public Button playButton;

    [Tooltip("The amoeba UI element used for zoom transition effect")]
    public RectTransform amoebaRect;

    [Tooltip("Canvas group containing all menu elements (for fading)")]
    public CanvasGroup menuCanvasGroup;

    [Header("Animation Settings")]
    [Tooltip("How long the zoom/fade transition should take")]
    public float zoomDuration = 1.0f;

    /// <summary>
    /// Sets up button listeners when the menu is initialized.
    /// </summary>
    void Start()
    {
        // Register the play button click handler
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        else
        {
            Debug.LogError("Play button reference not set in MenuManager");
        }
    }

    /// <summary>
    /// Handles the play button click event, initiating the transition to the game scene.
    /// </summary>
    void OnPlayButtonClicked()
    {
        // Prevent multiple clicks during transition
        playButton.interactable = false;

        // Play the frog sound effect if AudioManager exists
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFrogSound();
        }

        // Fade out the entire menu UI
        if (menuCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(menuCanvasGroup, 1, 0, zoomDuration));
        }

        // Create the SceneTransitionManager if it doesn't already exist
        // This manager will handle the visual transition between scenes
        if (FindObjectOfType<SceneTransitionManager>() == null)
        {
            GameObject transitionManager = new GameObject("SceneTransitionManager");
            transitionManager.AddComponent<SceneTransitionManager>();
        }

        // Start the transition sequence after a small delay
        // This gives time for the fade to begin before the zoom starts
        StartCoroutine(DelayedTransition());
    }

    /// <summary>
    /// Delays the scene transition slightly to allow fade effects to begin.
    /// </summary>
    IEnumerator DelayedTransition()
    {
        // Small delay to let fade animation start
        yield return new WaitForSeconds(0.2f);

        // Store the current position and scale of the amoeba UI element
        // These will be used as the starting point for the zoom transition
        Vector3 worldPos = amoebaRect.position;
        Vector3 scale = amoebaRect.localScale;

        // Trigger the scene transition with the amoeba as the focal point
        SceneTransitionManager.Instance.StartTransition(worldPos, scale);
    }

    /// <summary>
    /// Smoothly fades a canvas group from one alpha value to another.
    /// </summary>
    /// <param name="cg">The canvas group to fade</param>
    /// <param name="from">Starting alpha value (0-1)</param>
    /// <param name="to">Target alpha value (0-1)</param>
    /// <param name="duration">How long the fade should take in seconds</param>
    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;

        // Gradually adjust alpha over time
        while (elapsed < duration)
        {
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we reach the exact target value
        cg.alpha = to;
    }
}