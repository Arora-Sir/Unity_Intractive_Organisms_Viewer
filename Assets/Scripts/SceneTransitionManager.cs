using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages smooth transitions between scenes, preserving information about the selected organism.
/// Implemented as a singleton to persist between scene loads.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    // Singleton instance accessible from anywhere
    public static SceneTransitionManager Instance { get; private set; }

    // Properties to track transition state and organism data
    public bool IsTransitioning { get; private set; }

    // Store the position and scale of the amoeba to recreate it in the next scene
    public Vector3 AmoebaStartPosition { get; set; }
    public Vector3 AmoebaStartScale { get; set; }

    /// <summary>
    /// Set up the singleton pattern on awake.
    /// Only one instance of this manager should exist across the entire game.
    /// </summary>
    void Awake()
    {
        // If this is the first instance, make it the singleton
        if (Instance == null)
        {
            Instance = this;

            // Keep this object when loading new scenes
            DontDestroyOnLoad(gameObject);
        }
        // If an instance already exists, destroy this duplicate
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Begin transitioning to the game scene, storing the organism's initial properties.
    /// Called when a user selects an organism from the selection menu.
    /// </summary>
    /// <param name="startPos">The position where the organism should appear</param>
    /// <param name="startScale">The initial scale of the organism</param>
    public void StartTransition(Vector3 startPos, Vector3 startScale)
    {
        // Mark that we're in a transition
        IsTransitioning = true;

        // Store the organism's starting properties to use in the next scene
        AmoebaStartPosition = startPos;
        AmoebaStartScale = startScale;

        // Load the main game scene
        // Using Single mode to replace the current scene entirely
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    /// <summary>
    /// Finalizes the transition after the new scene has fully loaded and initialized.
    /// Called by the GameManager once it's ready to take control.
    /// </summary>
    public void CompleteTransition()
    {
        // Mark transition as complete
        IsTransitioning = false;
    }
}