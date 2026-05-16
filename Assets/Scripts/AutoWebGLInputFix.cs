using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if UNITY_WEBGL
using WebGLSupport;
#endif

public class AutoWebGLInputFix : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
#if UNITY_WEBGL
        SceneManager.sceneLoaded += OnSceneLoaded;
        ProcessAllInputFields();
#endif
    }

#if UNITY_WEBGL
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ProcessAllInputFields();
    }

    public static void ProcessAllInputFields()
    {
        TMP_InputField[] inputFields = Resources.FindObjectsOfTypeAll<TMP_InputField>();
        foreach (TMP_InputField inputField in inputFields)
        {
            // Bỏ qua các prefab (chỉ lấy các đối tượng nằm trong Scene)
            if (inputField.gameObject.scene.name == null) continue;

            if (inputField.gameObject.GetComponent<WebGLSupport.WebGLInput>() == null)
            {
                inputField.gameObject.AddComponent<WebGLSupport.WebGLInput>();
            }
        }
    }
#endif
}
