using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the visual appearance of connection lines between 3D objects and their UI labels.
/// Provides methods to highlight and reset the line's appearance when interacting with labels.
/// </summary>
public class LabelConnection : MonoBehaviour
{
    [Tooltip("Reference to the Image component used for drawing the connection line")]
    public Image lineImage;

    // Default appearance values
    private readonly Color defaultColor = Color.black;
    private readonly Color highlightColor = new Color(1f, 0.8f, 0f); // Bright yellow
    private readonly float defaultThickness = 10f;
    private readonly float highlightThickness = 15f;

    /// <summary>
    /// Highlights the connection line by changing its color and increasing thickness.
    /// Called when the associated label or object is hovered or selected.
    /// </summary>
    public void HighlightLine()
    {
        if (lineImage != null)
        {
            // Change line color to highlight color (bright yellow)
            lineImage.color = highlightColor;

            // Increase line thickness for better visibility
            RectTransform lineRect = lineImage.GetComponent<RectTransform>();
            if (lineRect != null)
            {
                lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, highlightThickness);
            }
        }
    }

    /// <summary>
    /// Resets the connection line to its default appearance.
    /// Called when the mouse leaves the associated label or object.
    /// </summary>
    public void ResetLine()
    {
        if (lineImage != null)
        {
            // Restore default color
            lineImage.color = defaultColor;

            // Restore default thickness
            RectTransform lineRect = lineImage.GetComponent<RectTransform>();
            if (lineRect != null)
            {
                lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, defaultThickness);
            }
        }
    }
}