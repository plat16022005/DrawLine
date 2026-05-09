using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ kịch bản tutorial của một màn.
/// Tạo asset: chuột phải trong Project → Create → Tutorial → Scenario
/// Sau đó gán vào TutorialManager trong Inspector của scene đó.
/// </summary>
[CreateAssetMenu(fileName = "TutorialScenario", menuName = "Tutorial/Scenario", order = 0)]
public class TutorialScenario : ScriptableObject
{
    [Tooltip("ID duy nhất của kịch bản — dùng để lưu PlayerPrefs.\nVí dụ: 'Level1', 'Level2_Drawing'")]
    public string scenarioId = "Level1";

    [Tooltip("Nếu true, tutorial chỉ chạy 1 lần duy nhất (lưu vào PlayerPrefs)")]
    public bool playOnce = true;

    [Tooltip("Danh sách các bước hướng dẫn theo thứ tự")]
    public List<TutorialStepData> steps = new List<TutorialStepData>();
}
