using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script nhỏ gắn vào Button ở bất kỳ scene nào để chuyển sang scene MakeMap.
///
/// == SETUP ==
/// 1. Tạo một Button trong scene (VD: MainMenu).
/// 2. Gắn script này vào Button (hoặc một GameObject bất kỳ).
/// 3. Trong Inspector của Button > OnClick() → kéo GameObject có script này vào
///    và chọn hàm MapEditorSceneLoader.OpenMapEditor().
/// 4. Đảm bảo scene "MakeMap" đã được thêm vào File > Build Settings.
/// </summary>
public class MapEditorSceneLoader : MonoBehaviour
{
    [Tooltip("Tên scene Map Editor (phải khớp với tên trong Build Settings)")]
    public string mapEditorSceneName = "MakeMap";

    /// <summary>
    /// Chuyển sang scene Map Editor.
    /// Gọi hàm này từ OnClick() của Button.
    /// </summary>
    public void OpenMapEditor()
    {
        Time.timeScale = 1f; // Đảm bảo time không bị đóng băng khi chuyển scene
        SceneManager.LoadScene(mapEditorSceneName);
    }
}
