using UnityEngine;

public class WebGLZoomBridge : MonoBehaviour
{
    private static WebGLZoomBridge instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (instance == null)
        {
            var go = new GameObject("WebGLZoomBridge");
            instance = go.AddComponent<WebGLZoomBridge>();
            DontDestroyOnLoad(go);
        }
#endif
    }

    // Called from Javascript via UnityInstance.SendMessage
    public void OnPinchZoom(string deltaStr)
    {
        if (float.TryParse(deltaStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float delta))
        {
            if (CameraPinchZoom.instance != null)
            {
                CameraPinchZoom.instance.ZoomFromJS(delta);
            }
        }
    }
}
