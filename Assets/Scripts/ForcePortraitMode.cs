using UnityEngine;
//This Script only works for Non-Singleton files, so for better solution I've created the `GlobalAspectRatioController.cs` for potrait mode.
public class ForcePortraitMode : MonoBehaviour
{
    // Target aspect ratio (9:16 for portrait)
    public float targetAspect = 9f / 16f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Force portrait orientation on mobile
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL || UNITY_EDITOR
        Screen.orientation = ScreenOrientation.Portrait;
#endif

        ApplyAspectRatio();
    }

    void ApplyAspectRatio()
    {
        // Determine the game window's current aspect ratio
        float windowAspect = (float)Screen.width / (float)Screen.height;

        // Current viewport height should be scaled by this amount
        float scaleHeight = windowAspect / targetAspect;

        // If scaled height is less than current height, add letterbox
        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            cam.rect = rect;
        }
        else // Add pillarbox
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            cam.rect = rect;
        }
    }

    // Call this if the screen orientation changes
    void OnRectTransformDimensionsChange()
    {
        ApplyAspectRatio();
    }
}
