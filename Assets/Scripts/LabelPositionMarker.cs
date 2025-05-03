using UnityEngine;

/// <summary>
/// Marks positions in 3D space where UI labels and their connecting lines should be positioned.
/// This component is attached to empty GameObjects that serve as visual anchors for the UI system.
/// </summary>
public class LabelPositionMarker : MonoBehaviour
{
    [Tooltip("Label name (will use parent GameObject name if left empty)")]
    [HideInInspector]
    public string labelName;

    [Tooltip("If true, this marks where the connecting line starts (on the 3D model)")]
    public bool isLineStart = false;

    /// <summary>
    /// Initializes the label name if not explicitly set.
    /// </summary>
    private void Awake()
    {
        // If label name is empty, use the parent GameObject's name
        // This allows markers to be placed as children of the parts they label
        if (string.IsNullOrEmpty(labelName) && transform.parent != null)
        {
            labelName = transform.parent.name;
        }
    }

    /// <summary>
    /// Draws editor-only visual indicators to help with positioning markers.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw a visible sphere in the editor to help with positioning
        // Green for line start points, blue for label positions
        Gizmos.color = isLineStart ? Color.green : Color.blue;
        Gizmos.DrawSphere(transform.position, 5f);

#if UNITY_EDITOR
        // Display the marker name and type in the scene view
        string displayName = string.IsNullOrEmpty(labelName) ? gameObject.name : labelName;
        UnityEditor.Handles.Label(transform.position, displayName + (isLineStart ? " (Line Start)" : " (Label)"));
#endif
    }
}