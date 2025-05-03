using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class PortraitCanvasEnforcer : MonoBehaviour
{
    void Start()
    {
        // Force portrait settings on this canvas
        CanvasScaler scaler = GetComponent<CanvasScaler>();

        // Set to portrait resolution
        scaler.referenceResolution = new Vector2(1080, 1920);

        // Match by height for portrait mode
        scaler.matchWidthOrHeight = 1;

        // Force portrait orientation on mobile
#if UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Portrait;
#endif
    }
}