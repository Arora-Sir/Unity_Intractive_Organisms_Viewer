using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A custom line renderer for UI elements that draws a line between two points.
/// I created this because the standard LineRenderer doesn't work well with Canvas UI elements.
/// </summary>
public class UILineRenderer : MonoBehaviour
{
    // Core components needed for the line
    private RectTransform rectTransform;
    private Image lineImage;

    /// <summary>
    /// Sets up the necessary components for the line renderer.
    /// Should be called when the line is first created.
    /// </summary>
    public void Initialize()
    {
        // Get the RectTransform to position and size the line
        rectTransform = GetComponent<RectTransform>();

        // Get or create the Image component that will render the line
        lineImage = GetComponent<Image>();

        // If there's no Image component, add one with default settings
        if (lineImage == null)
        {
            lineImage = gameObject.AddComponent<Image>();
            lineImage.color = Color.black; // Default color
        }
    }

    /// <summary>
    /// Updates the line to connect the two specified points.
    /// This handles positioning, rotation, and scaling of the line.
    /// </summary>
    /// <param name="startPoint">Starting point in UI space</param>
    /// <param name="endPoint">Ending point in UI space</param>
    public void SetPoints(Vector2 startPoint, Vector2 endPoint)
    {
        // Calculate direction vector for rotation
        Vector2 direction = (endPoint - startPoint).normalized;

        // Calculate length of the line
        float distance = Vector2.Distance(startPoint, endPoint);

        // Position the line at the midpoint between start and end
        rectTransform.position = (startPoint + endPoint) / 2;

        // Set the width and height of the line
        rectTransform.sizeDelta = new Vector2(distance, 2f); // Line is 2 pixels thick by default

        // Calculate and apply the rotation angle to align with the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Changes the color of the line.
    /// </summary>
    /// <param name="color">The new color to apply</param>
    public void SetColor(Color color)
    {
        if (lineImage != null)
            lineImage.color = color;
    }

    /// <summary>
    /// Changes the thickness of the line.
    /// </summary>
    /// <param name="width">The new width in pixels</param>
    public void SetWidth(float width)
    {
        // Keep the current length, just update the height (thickness)
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, width);
    }
}