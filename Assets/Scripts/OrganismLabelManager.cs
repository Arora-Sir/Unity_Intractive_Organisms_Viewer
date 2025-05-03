using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the label positions for organism parts, handling both the label placement 
/// and the connection lines between parts and their labels.
/// </summary>
public class OrganismLabelManager : MonoBehaviour
{
    // The name of this organism for debugging and identification
    public string organismName;

    // Dictionaries to quickly look up markers by label name
    private Dictionary<string, LabelPositionMarker> labelMarkers = new Dictionary<string, LabelPositionMarker>();
    private Dictionary<string, LabelPositionMarker> lineStartMarkers = new Dictionary<string, LabelPositionMarker>();

    /// <summary>
    /// Re-initializes markers when the object is enabled.
    /// Useful for when organisms are swapped or reactivated.
    /// </summary>
    private void OnEnable()
    {
        // Re-initialize markers when enabled to ensure we have the latest positions
        InitializeMarkers();
    }

    /// <summary>
    /// Initializes all label markers by finding them in the children hierarchy.
    /// This can be called manually if markers are added/removed at runtime.
    /// </summary>
    public void InitializeMarkers()
    {
        // Clear existing markers to avoid duplicates
        labelMarkers.Clear();
        lineStartMarkers.Clear();

        // Find all label position markers in children (including inactive ones)
        LabelPositionMarker[] markers = GetComponentsInChildren<LabelPositionMarker>(true);

        foreach (var marker in markers)
        {
            // Use the parent's name as the identifier for this marker
            string partName = marker.transform.parent.name;

            if (marker.isLineStart)
            {
                Debug.Log($"Added line start marker for: {partName}");
                lineStartMarkers[partName] = marker;
            }
            else
            {
                Debug.Log($"Added label marker for: {partName}");
                labelMarkers[partName] = marker;
            }
        }

        Debug.Log($"[{organismName}] Found {labelMarkers.Count} label markers and {lineStartMarkers.Count} line start markers");
    }

    /// <summary>
    /// Initial setup of markers when the component is created.
    /// </summary>
    private void Awake()
    {
        // Find all markers in children and organize them
        LabelPositionMarker[] markers = GetComponentsInChildren<LabelPositionMarker>();
        foreach (var marker in markers)
        {
            // Determine the label key - either use the marker's explicit name or the parent object's name
            string labelKey;

            if (string.IsNullOrEmpty(marker.labelName) && marker.transform.parent != null)
            {
                labelKey = marker.transform.parent.name;
            }
            else
            {
                labelKey = marker.labelName;
            }

            // Sort markers into appropriate dictionaries
            if (marker.isLineStart)
            {
                lineStartMarkers[labelKey] = marker;
                Debug.Log($"Added line start marker for: {labelKey}");
            }
            else
            {
                labelMarkers[labelKey] = marker;
                Debug.Log($"Added label marker for: {labelKey}");
            }
        }
    }

    /// <summary>
    /// Validates the marker setup and logs warnings for any missing pairs.
    /// </summary>
    private void Start()
    {
        // Log the total count of markers found
        Debug.Log($"[{organismName}] Found {labelMarkers.Count} label markers and {lineStartMarkers.Count} line start markers");

        // Check for any labels that don't have corresponding line starts
        foreach (var key in labelMarkers.Keys)
        {
            if (!lineStartMarkers.ContainsKey(key))
            {
                Debug.LogWarning($"[{organismName}] Missing line start for label: {key}");
            }
        }
    }

    /// <summary>
    /// Converts a world position of a label marker to canvas space.
    /// </summary>
    /// <param name="labelName">The name of the label/part</param>
    /// <param name="canvasRect">The canvas RectTransform</param>
    /// <param name="camera">The camera used for UI rendering</param>
    /// <returns>Position in canvas space for the label</returns>
    public Vector2 GetLabelPosition(string labelName, RectTransform canvasRect, Camera camera)
    {
        if (labelMarkers.TryGetValue(labelName, out LabelPositionMarker marker))
        {
            // Convert from world space to screen space
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camera, marker.transform.position);

            // Convert from screen space to canvas local space
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, camera, out localPos);
            return localPos;
        }
        Debug.LogWarning($"No label position found for {labelName}");
        return Vector2.zero;
    }

    /// <summary>
    /// Converts a world position of a line start marker to canvas space.
    /// </summary>
    /// <param name="labelName">The name of the label/part</param>
    /// <param name="canvasRect">The canvas RectTransform</param>
    /// <param name="camera">The camera used for UI rendering</param>
    /// <returns>Position in canvas space for the start of the connection line</returns>
    public Vector2 GetLineStartPosition(string labelName, RectTransform canvasRect, Camera camera)
    {
        if (lineStartMarkers.TryGetValue(labelName, out LabelPositionMarker marker))
        {
            // Convert from world space to screen space
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camera, marker.transform.position);

            // Convert from screen space to canvas local space
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, camera, out localPos);
            return localPos;
        }
        Debug.LogWarning($"No line start position found for {labelName}");
        return Vector2.zero;
    }

    /// <summary>
    /// Checks if a label position exists for the given name.
    /// </summary>
    public bool HasLabelPosition(string labelName)
    {
        return labelMarkers.ContainsKey(labelName);
    }

    /// <summary>
    /// Checks if a line start position exists for the given name.
    /// </summary>
    public bool HasLineStartPosition(string labelName)
    {
        // Debug logging to help troubleshoot missing markers
        Debug.Log($"Checking for line start position for {labelName}. Available markers: {string.Join(", ", lineStartMarkers.Keys)}");

        return lineStartMarkers.ContainsKey(labelName);
    }
}