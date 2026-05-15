using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Auth;
#endif
using System.Linq;

/// <summary>
/// Singleton quản lý toàn bộ luồng tutorial cho scene hiện tại.
///
/// SETUP trong Unity:
///  1. Tạo một GameObject "TutorialManager" trong Canvas.
///  2. Gán script này vào đó.
///  3. Tạo TutorialScenario asset (chuột phải → Create → Tutorial → Scenario).
///  4. Gán scenario asset vào field "Scenario" trong Inspector.
///  5. Tạo các UI child objects như mô tả trong region [UI Setup].
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────
    /// <summary>Phát mỗi khi chuyển sang bước mới. Tham số: (stepIndex, stepData)</summary>
    public static event Action<int, TutorialStepData> OnStepChanged;
    /// <summary>Phát khi tutorial kết thúc (hết bước).</summary>
    public static event Action OnTutorialComplete;

    // ── Inspector: Scenario ───────────────────────────────────────────────
    [Header("Scenario")]
    [Tooltip("Kịch bản tutorial hiện tại đang chạy (có thể gán sẵn hoặc set qua LoadScenario).")]
    public TutorialScenario scenario;

    [Tooltip("Danh sách tất cả kịch bản tutorial (dùng cho 1 scene chung).\n" +
             "Index 0 = màn tutorial 1, index 1 = màn tutorial 2,...\n" +
             "Gọi LoadScenarioByIndex(n) để bắt đầu kịch bản tương ứng.")]
    public TutorialScenario[] scenarioList;

    [Tooltip("Nếu true, tôn trọng playOnce: không chạy lại nếu đã hoàn thành.")]
    public bool respectPlayOnce = true;

    [Header("World Prefabs")]
    [Tooltip("Danh sách các Prefab thế giới tương ứng với từng bài tutorial. Kéo Prefab từ thư mục của bạn vào đây theo đúng thứ tự.")]
    public GameObject[] worldPrefabs;

    // ── Inspector: UI ─────────────────────────────────────────────────────
    [Header("UI — Root")]
    [Tooltip("GameObject gốc chứa toàn bộ UI tutorial. Script sẽ Activate/Deactivate cái này.")]
    public GameObject tutorialRoot;

    [Header("UI — Text Panel")]
    [Tooltip("RectTransform của TextPanel. Script sẽ di chuyển nó theo vị trí đã chọn trong từng bước.")]
    public RectTransform textPanel;
    public TextMeshProUGUI tutorialText;

    [Header("UI — Dark Overlay (4 panels tạo hiệu ứng spotlight)")]
    [Tooltip("Đặt 4 Image (màu đen/tối, alpha ~0.75) làm con của Canvas.\n" +
             "Script sẽ tự resize chúng để tạo lỗ sáng quanh highlightTarget.")]
    public RectTransform overlayTop;
    public RectTransform overlayBottom;
    public RectTransform overlayLeft;
    public RectTransform overlayRight;
    [Tooltip("Panel tối phủ toàn màn hình (dùng khi blockInteraction = true mà không có Highlight).")]
    public RectTransform fullBlockOverlay;
    public Color overlayColor = new Color(0f, 0f, 0f, 0.72f);
    [Tooltip("Padding mặc định (pixel) quanh vùng highlight nếu step không chỉ định.")]
    public float defaultHighlightPadding = 12f;

    [Header("UI — Congratulations")]
    [Tooltip("Panel hiện ra khi hoàn thành một Scenario kịch bản.")]
    public GameObject congratulationsPanel;

    [Header("UI — Hand Pointer")]
    [Tooltip("RectTransform của sprite bàn tay/mũi tên chỉ vào mục tiêu.")]
    public RectTransform handPointer;
    [Tooltip("Biên độ dao động của bàn tay (pixel)")]
    public float handBobAmplitude = 16f;
    [Tooltip("Tốc độ dao động (Hz — chu kỳ/giây)")]
    public float handBobFrequency = 1.6f;

    public Button BtnBack;

    // ── Private State ─────────────────────────────────────────────────────
    private int   _stepIndex    = -1;
    private int   _currentScenarioListIndex = -1; // Index của scenario đang chạy trong scenarioList
    private bool  _isActive     = false;
    private bool  _stepDone     = false;   // guard tránh AdvanceStep() gọi 2 lần
    private float _stepTimer    = 0f;
    private float _handBobPhase = 0f;
    private Vector2 _handBasePos;          // anchoredPosition gốc của hand pointer
    private bool  _drawConditionMet = false; // Ghi nhớ nếu đã vẽ trúng vùng (dùng cho DrawInRegion)
    private float _currentStrokeLength = 0f; // Độ dài của nét vẽ hiện tại
    private Vector2 _lastDrawPoint = Vector2.zero; // Điểm vẽ trước đó để tính khoảng cách
    private float _ignoreInputTimer = 0f; // Thời gian chờ để tránh bị click xuyên thấu khi vừa đổi bài

    private Canvas    _canvas;
    private RectTransform _canvasRect;
#if !UNITY_WEBGL || UNITY_EDITOR
    private FirebaseUser user;
#endif

    private TutorialStepData CurrentStep =>
        (scenario != null && _stepIndex >= 0 && _stepIndex < scenario.steps.Count)
            ? scenario.steps[_stepIndex] : null;

    // ─────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        user = FirebaseAuth.DefaultInstance.CurrentUser;
#endif
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _rectCorners = new Vector3[4];

        _canvas     = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
        if (_canvas != null)
        {
            _canvasRect = _canvas.GetComponent<RectTransform>();
            // Đảm bảo Tutorial luôn đè lên trên mọi UI khác để chặn tương tác
            _canvas.sortingOrder = 999;

            // Đảm bảo Canvas này không chặn Raycast nếu không cần thiết
            GraphicRaycaster gr = _canvas.GetComponent<GraphicRaycaster>();
            if (gr == null) gr = _canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        if (DataGame.instance.Tutorial == true)
        {
            BtnBack.gameObject.SetActive(true);
        }
        else
        {
            BtnBack.gameObject.SetActive(false);
        }
    }

    private void OnEnable()  => LineCreator.OnDrawPoint += HandleDrawPoint;
    private void OnDisable() => LineCreator.OnDrawPoint -= HandleDrawPoint;

    private void Start()
    {
        // Nếu đã gán sẵn scenario đơn lẻ thì ưu tiên chạy nó
        if (scenario != null && scenario.steps.Count > 0)
        {
            if (respectPlayOnce && scenario.playOnce && PlayerPrefs.GetInt(PrefKey(), 0) == 1)
            { HideAll(); return; }

            StartTutorial();
            return;
        }

        // Nếu không có scenario đơn lẻ, tự động lấy cái đầu tiên từ danh sách scenarioList
        if (scenarioList != null && scenarioList.Length > 0)
        {
            LoadScenarioByIndex(0);
        }
        else
        {
            HideAll();
        }
    }

    private void Update()
    {
        if (!_isActive || _stepDone || CurrentStep == null) return;

        // Giảm timer chờ input (để tránh click xuyên thấu khi đổi bài)
        if (_ignoreInputTimer > 0) _ignoreInputTimer -= Time.unscaledDeltaTime;

        if (CurrentStep.followTarget)
        {
            UpdateDynamicTracking();
        }

        AnimateHandPointer();
        CheckCurrentCondition();

        // Reset điểm vẽ cuối khi bắt đầu nhấn chuột mới (nhưng giữ nguyên tổng độ dài đã tích lũy)
        if (PointerJustDown())
        {
            _lastDrawPoint = Vector2.zero;
        }

        if (CurrentStep != null)
            ApplyStepTimeScale(CurrentStep);
    }

    private void UpdateDynamicTracking()
    {
        TutorialStepData step = CurrentStep;
        if (step == null) return;

        float padding = step.highlightPadding > 0f ? step.highlightPadding : defaultHighlightPadding;

        // 1. Cập nhật Highlight
        if (step.blockInteraction)
        {
            Transform targetT = TutorialTargetRegistry.Get(step.highlightTargetId);
            RectTransform highlightRt = targetT as RectTransform;

            if (step.useCustomUIPosition)
            {
                // Custom UI Position cố định, không cần theo dõi động
            }
            else if (highlightRt != null)
            {
                ApplyHighlight(highlightRt, padding, false);
            }
            else if (targetT != null || step.highlightWorldTarget)
            {
                GetWorldTargetInfo(step, out Vector2 pos, out float radius, out Vector2 size, out bool isRect);
                ApplyWorldHighlight(pos, radius, size, isRect, padding);
            }
        }

        // 2. Cập nhật Hand Pointer
        if (handPointer != null && step.showHandPointer)
        {
            RectTransform anchorRt = TutorialTargetRegistry.Get(step.handPointerAnchorId) as RectTransform;
            if (anchorRt != null || step.handPointerWorldPos != Vector2.zero)
            {
                _handBasePos = ResolveHandPosition(step) + step.handPointerOffset;
            }
        }
    }

    private void ApplyStepTimeScale(TutorialStepData step)
    {
        if (step.freezeTime)
        {
            Time.timeScale = 0f;
        }
        else
        {
            // Nếu không freeze, tuân theo trạng thái của GameController
            Time.timeScale = GameController.isPlaying ? 1f : 0f;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>Bắt đầu tutorial từ đầu.</summary>
    public void StartTutorial()
    {
        _stepIndex = -1;
        _isActive  = true;
        AdvanceStep(true); // Bắt đầu ngay lập tức
    }

    /// <summary>
    /// Gọi hàm này để hoàn thành bước hiện tại (chỉ hoạt động với điều kiện Manual).
    /// Có thể gán vào nút UI trong Inspector.
    /// </summary>
    public void NextStep()
    {
        if (!_isActive || CurrentStep == null) return;
        if (CurrentStep.completionCondition == TutorialCompletionCondition.Manual)
            AdvanceStep();
    }

    /// <summary>Bỏ qua bước hiện tại bất kể điều kiện.</summary>
    public void ForceNextStep() => AdvanceStep();

    /// <summary>Kết thúc tutorial ngay lập tức.</summary>
    public void SkipTutorial() => EndTutorial();

    /// <summary>Xóa trạng thái "đã hoàn thành" của scenario hiện tại.</summary>
    public void ResetTutorialProgress()
    {
        if (scenario != null) PlayerPrefs.DeleteKey(PrefKey());
    }

    /// <summary>
    /// [DÙNG CHO 1 SCENE CHUNG] Load và chạy một kịch bản tutorial bất kỳ.
    /// Gọi hàm này từ script quản lý level trước khi bắt đầu màn tutorial.
    /// <para>Ví dụ: TutorialManager.Instance.LoadScenario(myScenarioAsset);</para>
    /// </summary>
    public void LoadScenario(TutorialScenario newScenario)
    {
        Debug.Log($"[TutorialManager] --- Bắt đầu LoadScenario: {newScenario?.name} ---");
        
        // 1. Dừng tutorial đang chạy và dọn dẹp
        _isActive = false;
        _stepDone = false;
        
        // Đảm bảo thời gian không bị kẹt ở mức 0 của bài trước (trừ khi bài mới yêu cầu freeze ngay bước 1)
        Time.timeScale = GameController.isPlaying ? 1f : 0f;
        
        HideAll();

        // Xóa tất cả các đường vẽ hiện tại & khôi phục mực
        Line[] lines = FindObjectsOfType<Line>();
        foreach (Line l in lines) {
            if (l != null) Destroy(l.gameObject);
        }
        if (InkManager.Instance != null) {
            InkManager.Instance.ResetInk();
        }

        // Đặt lại công cụ mặc định là vẽ (bút đen)
        LineCreator lc = FindObjectOfType<LineCreator>();
        if (lc != null) {
            lc.SelectNormalPen();
        }

        // 2. Ẩn bảng chúc mừng
        if (congratulationsPanel != null) 
        {
            congratulationsPanel.SetActive(false);
        }

        scenario   = newScenario;
        _stepIndex = -1;

        if (newScenario == null || newScenario.steps.Count == 0)
        {
            Debug.Log("[TutorialManager] Scenario null hoặc rỗng — bỏ qua.");
            return;
        }

        if (respectPlayOnce && newScenario.playOnce &&
            PlayerPrefs.GetInt(PrefKey(), 0) == 1)
        {
            Debug.Log($"[TutorialManager] Scenario '{newScenario.scenarioId}' ĐÃ HOÀN THÀNH TRƯỚC ĐÓ — BỎ QUA.");
            CheckNextScenario(); 
            return;
        }

        // 3. Sử dụng Coroutine để bắt đầu bài mới sau 1 frame
        // Điều này cực kỳ quan trọng để các TutorialTarget trong Prefab mới kịp chạy OnEnable/Register
        StartCoroutine(StartScenarioRoutine());
    }

    private System.Collections.IEnumerator StartScenarioRoutine()
    {
        // Chờ đến cuối frame để đảm bảo tất cả Instantiate/Awake/OnEnable của bài mới đã xong
        yield return new WaitForEndOfFrame();

        _isActive = true;
        _ignoreInputTimer = 0.2f;

        Debug.Log($"[TutorialManager] Bắt đầu chạy bước đầu tiên của {scenario.name}");
        AdvanceStep(true);
    }

    /// <summary>Bắt đầu kịch bản theo chỉ mục trong mảng scenarioList.</summary>
    public void LoadScenarioByIndex(int index)
    {
        if (scenarioList == null || index < 0 || index >= scenarioList.Length) return;

        // Xóa vật thể cũ trước khi nạp cái mới (nếu đang chuyển bài)
        DestroyTutorialWorldObject(_currentScenarioListIndex);

        _currentScenarioListIndex = index;
        
        // Tạo vật thể mới cho Scenario này
        SpawnTutorialWorldObject(index);

        LoadScenario(scenarioList[index]);
    }

    private void SpawnTutorialWorldObject(int index)
    {
        if (worldPrefabs == null || index < 0 || index >= worldPrefabs.Length)
        {
            Debug.LogWarning($"[TutorialManager] Không tìm thấy Prefab tại Index {index} trong mảng worldPrefabs.");
            return;
        }

        GameObject prefab = worldPrefabs[index];
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            instance.name = "Tutorial" + index; 
            Debug.Log($"[TutorialManager] Đã tạo vật thể thế giới từ mảng: {instance.name}");
        }
    }

    private void DestroyTutorialWorldObject(int index)
    {
        if (index < 0) return;
        GameObject oldObj = GameObject.Find("Tutorial" + index);
        if (oldObj != null)
        {
            Destroy(oldObj);
            Debug.Log($"[TutorialManager] Đã xóa vật thể thế giới: Tutorial{index}");
        }
    }

    /// <summary>
    /// Kiểm tra một vị trí trong World có nằm trong vùng "vùng sáng" của tutorial không.
    /// Dùng để giới hạn vùng vẽ hoặc tương tác.
    /// </summary>
    public bool IsPositionAllowed(Vector2 worldPos)
    {
        // Nếu tutorial không chạy, mọi vị trí đều hợp lệ
        if (!_isActive || CurrentStep == null) return true;

        // Nếu bước này là vẽ trong vùng (DrawInRegion), chỉ cho phép trong vùng đó
        if (CurrentStep.completionCondition == TutorialCompletionCondition.DrawInRegion)
        {
            if (CurrentStep.useCustomUIPosition)
            {
                if (_canvasRect == null) return false;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localPos);
                return Vector2.Distance(localPos, CurrentStep.customUIPosition) <= CurrentStep.customUIRadius;
            }

            GetWorldTargetInfo(CurrentStep, out Vector2 targetPos, out float radius, out Vector2 size, out bool isRect);
            if (isRect)
            {
                Vector2 halfSize = size * 0.5f;
                return (worldPos.x >= targetPos.x - halfSize.x && worldPos.x <= targetPos.x + halfSize.x &&
                        worldPos.y >= targetPos.y - halfSize.y && worldPos.y <= targetPos.y + halfSize.y);
            }
            else
            {
                return Vector2.Distance(worldPos, targetPos) <= radius;
            }
        }

        // Nếu bước này có Highlight Target là World Object, cũng cho phép tại đó
        Transform targetT = TutorialTargetRegistry.Get(CurrentStep.highlightTargetId);
        if (targetT != null && !(targetT is RectTransform))
        {
            if (Vector2.Distance(worldPos, targetT.position) <= CurrentStep.targetWorldRadius)
                return true;
        }

        // Nếu bước này đang blockInteraction (phủ màn đen), mặc định chặn các vùng ngoài
        if (CurrentStep.blockInteraction) return false;

        return true;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Step Flow

    private void AdvanceStep(bool immediate = false)
    {
        if (_stepDone) return;
        _stepDone = true;

        // Ẩn bàn tay và toàn bộ overlay chặn ngay lập tức để giải phóng UI cho Button xử lý
        if (handPointer != null) handPointer.gameObject.SetActive(false);
        HideOverlay();

        // Xóa đường vẽ nếu bước hiện tại yêu cầu
        if (_stepIndex >= 0 && scenario != null && _stepIndex < scenario.steps.Count)
        {
            if (scenario.steps[_stepIndex].clearLinesOnComplete)
            {
                Line[] lines = FindObjectsOfType<Line>();
                foreach (Line l in lines)
                {
                    if (l != null) Destroy(l.gameObject);
                }
            }
        }

        _stepIndex++;
        if (scenario == null || _stepIndex >= scenario.steps.Count)
        {
            EndTutorial();
            return;
        }

        _stepTimer    = 0f;
        _stepDone     = false;
        _handBobPhase = 0f;
        _drawConditionMet = false; // Reset trạng thái vẽ cho bước mới
        _currentStrokeLength = 0f;
        _lastDrawPoint = Vector2.zero;

        ShowStep(scenario.steps[_stepIndex]);
        OnStepChanged?.Invoke(_stepIndex, scenario.steps[_stepIndex]);
    }



    private void ShowStep(TutorialStepData step)
    {
        // 1. Kích hoạt Raycaster để bắt đầu chặn tương tác theo kịch bản
        if (_canvas != null)
        {
            GraphicRaycaster gr = _canvas.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = true;
        }

        // 2. Dọn dẹp giao diện cũ trước khi hiện cái mới
        if (tutorialText != null) tutorialText.text = "";
        HideOverlay();

        // Kiểm tra an toàn cho tutorialRoot
        if (tutorialRoot != null) 
        {
            tutorialRoot.SetActive(true);
        }
        else
        {
            Debug.LogError("[TutorialManager] tutorialRoot bị NULL! Hãy kiểm tra xem bạn có lỡ tay xóa nó khi chuyển bài không.");
            return;
        }

        // 2. Cập nhật Text
        if (tutorialText != null) tutorialText.text = step.description;

        // 3. Cập nhật vị trí Panel & Thời gian
        MoveTextPanel(step);
        ApplyStepTimeScale(step);

        // 4. Highlight & Block Interaction
        float padding = step.highlightPadding > 0f ? step.highlightPadding : defaultHighlightPadding;
        Transform targetT = TutorialTargetRegistry.Get(step.highlightTargetId);
        RectTransform highlightRt = targetT as RectTransform;

        if (step.blockInteraction)
        {
            if (step.useCustomUIPosition)
            {
                ApplyCustomUIHighlight(step.customUIPosition, step.customUIRadius, padding);
            }
            else if (highlightRt != null)
            {
                ApplyHighlight(highlightRt, padding);
            }
            else if (targetT != null || (step.highlightWorldTarget && (step.completionCondition == TutorialCompletionCondition.PlayerReachPosition || 
                                                                       step.completionCondition == TutorialCompletionCondition.DrawInRegion ||
                                                                       step.completionCondition == TutorialCompletionCondition.EraseAll ||
                                                                       step.completionCondition == TutorialCompletionCondition.ErasePartial ||
                                                                       step.completionCondition == TutorialCompletionCondition.AutoTimer)))
            {
                GetWorldTargetInfo(step, out Vector2 pos, out float radius, out Vector2 size, out bool isRect);
                ApplyWorldHighlight(pos, radius, size, isRect, padding);
            }
            else
            {
                // Nếu muốn block mà không có highlight -> hiện màn đen full
                if (fullBlockOverlay != null) fullBlockOverlay.gameObject.SetActive(true);
            }
        }

        // 5. Kiểm tra và hiện Bàn tay
        if (handPointer != null && step.showHandPointer)
        {
            // Chỉ hiện bàn tay nếu tìm thấy Anchor hoặc có tọa độ World
            RectTransform anchorRt = TutorialTargetRegistry.Get(step.handPointerAnchorId) as RectTransform;
            if (anchorRt != null || step.handPointerWorldPos != Vector2.zero)
            {
                handPointer.gameObject.SetActive(true);
                _handBasePos = ResolveHandPosition(step) + step.handPointerOffset;
                handPointer.anchoredPosition = _handBasePos;
            }
        }
    }

    private void EndTutorial()
    {
        if (!_isActive) return; // Chặn gọi trùng lặp
        
        string scenarioName = scenario != null ? scenario.name : "Unknown";
        Debug.Log($"[TutorialManager] === EndTutorial được gọi cho Scenario: {scenarioName} ===");
        
        _isActive = false;
        Time.timeScale = GameController.isPlaying ? 1f : 0f;
        HideAll();

        if (scenario != null && scenario.playOnce)
            PlayerPrefs.SetInt(PrefKey(), 1);

        OnTutorialComplete?.Invoke();

        if (congratulationsPanel != null)
        {
            Debug.Log($"[TutorialManager] Hiển thị bảng chúc mừng của bài: {scenarioName}");
            if (tutorialRoot != null) tutorialRoot.SetActive(true);
            congratulationsPanel.SetActive(true);
            
            // Đảm bảo nút "Tiếp theo" có thể bấm được bằng cách bật lại Raycaster
            if (_canvas != null)
            {
                GraphicRaycaster gr = _canvas.GetComponent<GraphicRaycaster>();
                if (gr != null) gr.enabled = true;
            }

            // QUAN TRỌNG: Đảm bảo thời gian dừng để người chơi đọc bảng
            Time.timeScale = 0f; 
        }
        else
        {
            Debug.Log($"[TutorialManager] Bài {scenarioName} không có panel, tìm bài tiếp theo...");
            // Nếu không có panel, phải đảm bảo trả lại thời gian trước khi sang bài mới
            Time.timeScale = GameController.isPlaying ? 1f : 0f;
            CheckNextScenario();
        }
    }

    public void ContinueToNextScenario()
    {
        Debug.Log("[TutorialManager] Nút 'Tiếp theo' đã được nhấn!");
        if (congratulationsPanel != null) 
        {
            congratulationsPanel.SetActive(false);
            Debug.Log("[TutorialManager] Đã ẩn congratulationsPanel.");
        }
        
        CheckNextScenario();
    }

    private void CheckNextScenario()
    {
        // Lấy tên scene hiện tại (ví dụ: "Tutorial1")
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Tìm phần số trong tên scene
        string prefix = new string(currentSceneName.TakeWhile(c => !char.IsDigit(c)).ToArray());
        string numberPart = new string(currentSceneName.SkipWhile(c => !char.IsDigit(c)).ToArray());

        if (int.TryParse(numberPart, out int levelNumber))
        {
            // Nếu đã tới Tutorial4, khi nhấn Next sẽ về SampleScene
            if (levelNumber >= 4)
            {
                Debug.Log("[TutorialManager] Đã hoàn thành Tutorial 4. Chuyển sang SampleScene.");
                _isActive = false;
                Time.timeScale = 1f;

                // Đánh dấu đã hoàn thành Tutorial
                DataGame.instance.Tutorial = true;
#if !UNITY_WEBGL || UNITY_EDITOR
                FirebaseDataManager.instance.WriteDatabase("Tutorial", user.UserId, true);
#endif

                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
                return;
            }

            int nextLevelNumber = levelNumber + 1;
            string nextSceneName = prefix + nextLevelNumber;
            
            Debug.Log($"[TutorialManager] Chuyển từ {currentSceneName} sang {nextSceneName}");
            
            _isActive = false;
            Time.timeScale = 1f;

            // Kiểm tra xem scene tiếp theo có tồn tại trong Build Settings không
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.Log($"[TutorialManager] Đang chuyển sang: {nextSceneName}");
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.Log($"[TutorialManager] Không tìm thấy {nextSceneName} trong Build Settings. Hoàn tất Tutorial và về SampleScene.");
                
                // Đánh dấu đã hoàn thành Tutorial
                DataGame.instance.Tutorial = true;
#if !UNITY_WEBGL || UNITY_EDITOR
                FirebaseDataManager.instance.WriteDatabase("Tutorial", user.UserId, true);
#endif

                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
        }
        else
        {
            // Nếu tên scene không có số (ví dụ đang ở bài cuối hoặc bài đặc biệt)
            Debug.Log("[TutorialManager] Không xác định được số thứ tự bài, chuyển sang SampleScene.");
            _isActive = false;
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Condition Checking

    private void CheckCurrentCondition()
    {
        // Nếu đang trong thời gian chờ (vừa đổi bài), không kiểm tra các điều kiện click/vẽ
        if (_ignoreInputTimer > 0) return;

        TutorialStepData step = CurrentStep;
        switch (step.completionCondition)
        {
            case TutorialCompletionCondition.AutoTimer:
                _stepTimer += Time.unscaledDeltaTime;
                if (_stepTimer >= step.autoAdvanceDelay) AdvanceStep();
                break;

            case TutorialCompletionCondition.ClickRegion:
                if (PointerJustDown())
                {
                    Vector2 vp = ScreenToViewport(GetPointerScreen());
                    if (step.clickViewportRect.Contains(vp)) AdvanceStep();
                }
                break;

            case TutorialCompletionCondition.ClickObject:
                if (step.targetObject != null && PointerJustDown())
                    CheckClickObject(step.targetObject);
                break;

            case TutorialCompletionCondition.ClickUITarget:
                if (PointerJustUp())
                {
                    if (step.useCustomUIPosition)
                    {
                        if (_canvasRect != null)
                        {
                            Vector2 screenPos = GetPointerScreen();
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
                                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localClick);
                            if (Vector2.Distance(localClick, step.customUIPosition) <= step.customUIRadius)
                                AdvanceStep();
                        }
                    }
                    else if (!string.IsNullOrEmpty(step.highlightTargetId))
                    {
                        CheckClickUITarget(step.highlightTargetId);
                    }
                }
                break;

            case TutorialCompletionCondition.AnyClick:
                if (PointerJustUp()) AdvanceStep();
                break;

            case TutorialCompletionCondition.PlayerReachPosition:
                CheckPlayerReach(step);
                break;

            case TutorialCompletionCondition.DrawInRegion:
                // Nếu đã vẽ trúng vùng và bây giờ thả chuột -> Hoàn thành
                if (_drawConditionMet && PointerJustUp()) AdvanceStep();
                break;

            case TutorialCompletionCondition.EraseAll:
                if (!IsAnyLineInRegion(step)) AdvanceStep();
                break;

            case TutorialCompletionCondition.ErasePartial:
                if (GetTotalLineLengthInRegion(step) <= step.minDrawLength) AdvanceStep();
                break;
        }
    }

    private void CheckClickObject(GameObject target)
    {
        Vector2 screenPos = GetPointerScreen();
        Vector2 worldPos  = Camera.main.ScreenToWorldPoint(screenPos);
        Collider2D hit    = Physics2D.OverlapPoint(worldPos);
        if (hit != null && hit.gameObject == target) AdvanceStep();
    }

    private float GetTotalWorldLineLength()
    {
        float total = 0f;
        Line[] lines = FindObjectsOfType<Line>();
        foreach (var l in lines) total += l.CurrentLength;
        return total;
    }

    private float GetTotalLineLengthInRegion(TutorialStepData step)
    {
        float total = 0f;
        Line[] lines = FindObjectsOfType<Line>();

        if (step.useCustomUIPosition)
        {
            if (_canvasRect == null) return 0f;
            foreach (var l in lines)
            {
                if (l.Points == null || l.Points.Count < 2) continue;
                for (int i = 0; i < l.Points.Count - 1; i++)
                {
                    Vector2 p1 = l.Points[i];
                    Vector2 p2 = l.Points[i + 1];
                    Vector2 midW = (p1 + p2) * 0.5f;

                    Vector3 screenPos = Camera.main.WorldToScreenPoint(midW);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
                        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localPos);

                    if (Vector2.Distance(localPos, step.customUIPosition) <= step.customUIRadius)
                    {
                        total += Vector2.Distance(p1, p2);
                    }
                }
            }
        }
        else
        {
            GetWorldTargetInfo(step, out Vector2 center, out float radius, out Vector2 size, out bool isRect);
            foreach (var l in lines) 
                total += l.GetLengthInRegion(center, radius, size, isRect);
        }

        return total;
    }

    private bool IsAnyLineInRegion(TutorialStepData step)
    {
        Line[] lines = FindObjectsOfType<Line>();

        if (step.useCustomUIPosition)
        {
            if (_canvasRect == null) return false;
            foreach (var l in lines)
            {
                if (l.Points == null) continue;
                foreach (var p in l.Points)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(p);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
                        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localPos);
                    
                    if (Vector2.Distance(localPos, step.customUIPosition) <= step.customUIRadius)
                        return true;
                }
            }
            return false;
        }
        else
        {
            GetWorldTargetInfo(step, out Vector2 center, out float radius, out Vector2 size, out bool isRect);
            foreach (var l in lines)
            {
                if (l.HasPointInRegion(center, radius, size, isRect))
                    return true;
            }
            return false;
        }
    }

    private void CheckClickUITarget(string targetId)
    {
        RectTransform rt = TutorialTargetRegistry.Get(targetId) as RectTransform;
        if (rt == null) return;

        if (RectTransformUtility.RectangleContainsScreenPoint(rt, GetPointerScreen(), 
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera))
        {
            AdvanceStep();
        }
    }

    private void CheckPlayerReach(TutorialStepData step)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;
        
        GetWorldTargetInfo(step, out Vector2 targetPos, out float radius, out Vector2 size, out bool isRect);
        if (isRect)
        {
            Vector2 halfSize = size * 0.5f;
            Vector2 p = player.transform.position;
            if (p.x >= targetPos.x - halfSize.x && p.x <= targetPos.x + halfSize.x &&
                p.y >= targetPos.y - halfSize.y && p.y <= targetPos.y + halfSize.y)
                AdvanceStep();
        }
        else
        {
            if (Vector2.Distance(player.transform.position, targetPos) <= radius)
                AdvanceStep();
        }
    }

    /// <summary>Được gọi khi LineCreator vẽ một điểm mới.</summary>
    private void HandleDrawPoint(Vector2 worldPos)
    {
        if (!_isActive || _stepDone || CurrentStep == null) return;
        TutorialStepData step = CurrentStep;
        if (step.completionCondition != TutorialCompletionCondition.DrawInRegion) return;

        // Cộng dồn độ dài nét vẽ
        if (_lastDrawPoint != Vector2.zero)
        {
            _currentStrokeLength += Vector2.Distance(_lastDrawPoint, worldPos);
        }
        _lastDrawPoint = worldPos;

        bool inside = false;

        if (step.useCustomUIPosition)
        {
            if (_canvasRect != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localPos);
                if (Vector2.Distance(localPos, step.customUIPosition) <= step.customUIRadius)
                    inside = true;
            }
        }
        else
        {
            GetWorldTargetInfo(step, out Vector2 targetPos, out float radius, out Vector2 size, out bool isRect);
            if (isRect)
            {
                Vector2 halfSize = size * 0.5f;
                if (worldPos.x >= targetPos.x - halfSize.x && worldPos.x <= targetPos.x + halfSize.x &&
                    worldPos.y >= targetPos.y - halfSize.y && worldPos.y <= targetPos.y + halfSize.y)
                    inside = true;
            }
            else
            {
                if (Vector2.Distance(worldPos, targetPos) <= radius)
                    inside = true;
            }
        }

        if (inside)
        {
            // Chỉ đánh dấu đạt điều kiện nếu độ dài nét vẽ đã đủ yêu cầu
            if (_currentStrokeLength >= step.minDrawLength)
            {
                _drawConditionMet = true; 
            }
        }
    }

    private Vector2 GetStepWorldTarget(TutorialStepData step)
    {
        // 1. Ưu tiên tìm theo ID trong Registry (để gán từ Hierarchy vào Scenario asset)
        Transform t = null;
        if (Application.isPlaying) t = TutorialTargetRegistry.Get(step.worldTargetId);
        
        if (t != null) return t.position;

        // 2. Tiếp theo tìm theo tham chiếu GameObject trực tiếp (nếu có)
        if (step.worldTargetObject != null)
            return step.worldTargetObject.transform.position;

        // 3. Cuối cùng dùng tọa độ tay
        return step.targetWorldPosition;
    }

    private void GetWorldTargetInfo(TutorialStepData step, out Vector2 pos, out float radius, out Vector2 size, out bool isRect)
    {
        pos = GetStepWorldTarget(step);
        radius = step.targetWorldRadius;
        size = step.targetWorldSize;
        isRect = step.isRectangleTarget;

        Transform t = null;
        if (Application.isPlaying)
        {
            if (!string.IsNullOrEmpty(step.highlightTargetId))
                t = TutorialTargetRegistry.Get(step.highlightTargetId);
            
            if (t == null || t is RectTransform)
                t = TutorialTargetRegistry.Get(step.worldTargetId);
        }
        
        if (t == null && step.worldTargetObject != null)
            t = step.worldTargetObject.transform;

        if (t != null && !(t is RectTransform))
        {
            pos = t.position; 

            if (step.autoFitCollider)
            {
                Collider2D col = t.GetComponent<Collider2D>();
                if (col != null)
                {
                    pos = col.bounds.center;
                    if (col is CircleCollider2D circle)
                    {
                        isRect = false;
                        radius = Mathf.Max(circle.bounds.extents.x, circle.bounds.extents.y);
                    }
                    else
                    {
                        isRect = true;
                        size = col.bounds.size;
                    }
                }
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Highlight (4-Panel Spotlight)

    private Vector3[] _rectCorners;

    /// <summary>
    /// Định vị lại 4 panel overlay để tạo "lỗ sáng" quanh target.
    /// Cấu trúc:
    ///   [  overlayTop              ]
    ///   [left][ TARGET  ][  right ]
    ///   [  overlayBottom           ]
    /// </summary>
    private void ApplyHighlight(RectTransform target, float padding, bool forceUpdateLayout = true)
    {
        if (_canvasRect == null ||
            overlayTop == null || overlayBottom == null ||
            overlayLeft == null || overlayRight == null) return;

        // Bật 4 panel
        overlayTop.gameObject.SetActive(true);
        overlayBottom.gameObject.SetActive(true);
        overlayLeft.gameObject.SetActive(true);
        overlayRight.gameObject.SetActive(true);

        // Gán màu
        foreach (var rt in new[] { overlayTop, overlayBottom, overlayLeft, overlayRight })
        {
            Image img = rt.GetComponent<Image>();
            if (img != null) img.color = overlayColor;
        }

        // Lấy 4 góc của target trong local-space của Canvas
        if (forceUpdateLayout) Canvas.ForceUpdateCanvases();
        target.GetWorldCorners(_rectCorners);
        for (int i = 0; i < 4; i++)
            _rectCorners[i] = _canvasRect.InverseTransformPoint(_rectCorners[i]);

        // corners[0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right
        float left   = _rectCorners[0].x - padding;
        float bottom = _rectCorners[0].y - padding;
        float right  = _rectCorners[2].x + padding;
        float top    = _rectCorners[2].y + padding;

        Rect cr  = _canvasRect.rect;
        float hw = cr.width  * 0.5f;
        float hh = cr.height * 0.5f;

        // Dùng anchorMin/Max = (0.5, 0.5) và offsetMin/Max tính từ tâm canvas
        SetOverlayPanel(overlayTop,    left: -hw, right: hw, bot: top,    t: hh);
        SetOverlayPanel(overlayBottom, left: -hw, right: hw, bot: -hh,   t: bottom);
        SetOverlayPanel(overlayLeft,   left: -hw, right: left, bot: bottom, t: top);
        SetOverlayPanel(overlayRight,  left: right, right: hw, bot: bottom, t: top);
    }

    /// <summary>
    /// Định vị lại 4 panel overlay để tạo "lỗ sáng" tròn/vuông quanh tọa độ UI tùy chỉnh.
    /// </summary>
    private void ApplyCustomUIHighlight(Vector2 anchoredPosition, float radius, float padding)
    {
        if (_canvasRect == null ||
            overlayTop == null || overlayBottom == null ||
            overlayLeft == null || overlayRight == null) return;

        overlayTop.gameObject.SetActive(true);
        overlayBottom.gameObject.SetActive(true);
        overlayLeft.gameObject.SetActive(true);
        overlayRight.gameObject.SetActive(true);

        foreach (var rt in new[] { overlayTop, overlayBottom, overlayLeft, overlayRight })
        {
            Image img = rt.GetComponent<Image>();
            if (img != null) img.color = overlayColor;
        }

        float left   = anchoredPosition.x - radius - padding;
        float bottom = anchoredPosition.y - radius - padding;
        float right  = anchoredPosition.x + radius + padding;
        float top    = anchoredPosition.y + radius + padding;

        Rect cr  = _canvasRect.rect;
        float hw = cr.width  * 0.5f;
        float hh = cr.height * 0.5f;

        SetOverlayPanel(overlayTop,    left: -hw, right: hw, bot: top,    t: hh);
        SetOverlayPanel(overlayBottom, left: -hw, right: hw, bot: -hh,   t: bottom);
        SetOverlayPanel(overlayLeft,   left: -hw, right: left, bot: bottom, t: top);
        SetOverlayPanel(overlayRight,  left: right, right: hw, bot: bottom, t: top);
    }

    /// <summary>Highlight một vùng hình tròn hoặc chữ nhật trong World Space bằng spotlight.</summary>
    private void ApplyWorldHighlight(Vector2 worldPos, float radius, Vector2 size, bool isRect, float padding)
    {
        if (Camera.main == null || _canvasRect == null ||
            overlayTop == null || overlayBottom == null ||
            overlayLeft == null || overlayRight == null) return;

        // ... (phần bật panel giữ nguyên)
        overlayTop.gameObject.SetActive(true);
        overlayBottom.gameObject.SetActive(true);
        overlayLeft.gameObject.SetActive(true);
        overlayRight.gameObject.SetActive(true);
        foreach (var rt in new[] { overlayTop, overlayBottom, overlayLeft, overlayRight })
        {
            Image img = rt.GetComponent<Image>();
            if (img != null) img.color = overlayColor;
        }

        // Chuyển vị trí world sang screen
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        
        float localRadiusW, localRadiusH;

        if (isRect)
        {
            // Tính kích thước local cho hình chữ nhật
            Vector3 edgePosW = Camera.main.WorldToScreenPoint(worldPos + Vector2.right * size.x * 0.5f);
            Vector3 edgePosH = Camera.main.WorldToScreenPoint(worldPos + Vector2.up * size.y * 0.5f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, edgePosW, 
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localEdgeW);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, edgePosH, 
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localEdgeH);

            localRadiusW = Vector2.Distance(localCenter, localEdgeW);
            localRadiusH = Vector2.Distance(localCenter, localEdgeH);
        }
        else
        {
            // Tính bán kính local cho hình tròn
            Vector3 edgePos = Camera.main.WorldToScreenPoint(worldPos + Vector2.right * radius);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, edgePos, 
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localEdge);
            
            localRadiusW = localRadiusH = Vector2.Distance(localCenter, localEdge);
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, 
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 finalCenter);

        // Tính các cạnh
        float left   = finalCenter.x - localRadiusW - padding;
        float bottom = finalCenter.y - localRadiusH - padding;
        float right  = finalCenter.x + localRadiusW + padding;
        float top    = finalCenter.y + localRadiusH + padding;

        Rect cr  = _canvasRect.rect;
        float hw = cr.width  * 0.5f;
        float hh = cr.height * 0.5f;

        SetOverlayPanel(overlayTop,    left: -hw, right: hw, bot: top,    t: hh);
        SetOverlayPanel(overlayBottom, left: -hw, right: hw, bot: -hh,   t: bottom);
        SetOverlayPanel(overlayLeft,   left: -hw, right: left, bot: bottom, t: top);
        SetOverlayPanel(overlayRight,  left: right, right: hw, bot: bottom, t: top);
    }

    private void SetOverlayPanel(RectTransform rt,
        float left, float right, float bot, float t)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bot);
        rt.offsetMax = new Vector2(right, t);
    }

    private void HideOverlay()
    {
        if (overlayTop    != null) overlayTop.gameObject.SetActive(false);
        if (overlayBottom != null) overlayBottom.gameObject.SetActive(false);
        if (overlayLeft   != null) overlayLeft.gameObject.SetActive(false);
        if (overlayRight  != null) overlayRight.gameObject.SetActive(false);
        if (fullBlockOverlay != null) fullBlockOverlay.gameObject.SetActive(false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Text Panel Positioning

    /// <summary>
    /// Di chuyển TextPanel đến vị trí được chỉ định trong step.
    /// Preset tính dựa theo kích thước canvas và kích thước panel.
    /// </summary>
    private void MoveTextPanel(TutorialStepData step)
    {
        if (textPanel == null || _canvasRect == null) return;

        Vector2 pos;

        if (step.textPanelPreset == TextPanelPreset.Custom)
        {
            pos = step.textPanelCustomPos;
        }
        else
        {
            Rect cr      = _canvasRect.rect;
            float hw     = cr.width  * 0.5f;
            float hh     = cr.height * 0.5f;

            // Kích thước thực của panel (cần LayoutGroup rebuild trước, dùng sizeDelta)
            float pw     = textPanel.rect.width  * 0.5f;
            float ph     = textPanel.rect.height * 0.5f;

            // Margin an toàn (pixel) cách mép canvas
            const float margin = 20f;

            pos = step.textPanelPreset switch
            {
                TextPanelPreset.TopLeft      => new Vector2(-hw + pw + margin,  hh - ph - margin),
                TextPanelPreset.TopCenter    => new Vector2(0f,                 hh - ph - margin),
                TextPanelPreset.TopRight     => new Vector2( hw - pw - margin,  hh - ph - margin),
                TextPanelPreset.MiddleLeft   => new Vector2(-hw + pw + margin,  0f),
                TextPanelPreset.MiddleCenter => new Vector2(0f,                 0f),
                TextPanelPreset.MiddleRight  => new Vector2( hw - pw - margin,  0f),
                TextPanelPreset.BottomLeft   => new Vector2(-hw + pw + margin, -hh + ph + margin),
                TextPanelPreset.BottomCenter => new Vector2(0f,                -hh + ph + margin),
                TextPanelPreset.BottomRight  => new Vector2( hw - pw - margin, -hh + ph + margin),
                _                            => Vector2.zero
            };
        }

        // Áp dụng thêm offset tinh chỉnh
        textPanel.anchoredPosition = pos + step.textPanelOffset;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Hand Pointer Animation

    private void AnimateHandPointer()
    {
        if (handPointer == null || !handPointer.gameObject.activeSelf) return;
        _handBobPhase += Time.unscaledDeltaTime * handBobFrequency * Mathf.PI * 2f;
        float offset = Mathf.Sin(_handBobPhase) * handBobAmplitude;
        handPointer.anchoredPosition = _handBasePos + new Vector2(0f, offset);
    }

    /// <summary>Tính anchoredPosition của hand pointer từ dữ liệu bước.</summary>
    private Vector2 ResolveHandPosition(TutorialStepData step)
    {
        RectTransform anchorRt = TutorialTargetRegistry.Get(step.handPointerAnchorId) as RectTransform;
        if (anchorRt != null)
            return CanvasLocalOf(anchorRt, step.handPointerAlignment);

        // Dùng world position
        return WorldToCanvasLocal(step.handPointerWorldPos);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Coordinate Helpers

    /// <summary>Chuyển vị trí của một RectTransform sang local-space của Canvas root kèm alignment.</summary>
    private Vector2 CanvasLocalOf(RectTransform rt, HandPointerAlignment alignment = HandPointerAlignment.Center)
    {
        if (_canvasRect == null) return Vector2.zero;
        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // corners: 0: BottomLeft, 1: TopLeft, 2: TopRight, 3: BottomRight
        Vector3 targetPos = (corners[0] + corners[2]) * 0.5f; // Mặc định là Center

        switch (alignment)
        {
            case HandPointerAlignment.Top:
                targetPos = (corners[1] + corners[2]) * 0.5f;
                break;
            case HandPointerAlignment.Bottom:
                targetPos = (corners[0] + corners[3]) * 0.5f;
                break;
            case HandPointerAlignment.Left:
                targetPos = (corners[0] + corners[1]) * 0.5f;
                break;
            case HandPointerAlignment.Right:
                targetPos = (corners[2] + corners[3]) * 0.5f;
                break;
        }

        return _canvasRect.InverseTransformPoint(targetPos);
    }

    /// <summary>Chuyển World Position → anchoredPosition trong Canvas root.</summary>
    private Vector2 WorldToCanvasLocal(Vector2 worldPos)
    {
        if (Camera.main == null || _canvasRect == null) return Vector2.zero;
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 local);
        return local;
    }

    private Vector2 ScreenToViewport(Vector2 screenPos) =>
        new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);

    private bool PointerJustDown()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return Input.GetMouseButtonDown(0);
    }

    private bool PointerJustUp()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) return true;
        return Input.GetMouseButtonUp(0);
    }

    private Vector2 GetPointerScreen()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return Input.mousePosition;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region UI Helpers

    private void HideAll()
    {
        if (tutorialRoot != null) tutorialRoot.SetActive(false);
        HideOverlay();
        if (handPointer != null) handPointer.gameObject.SetActive(false);

        // Giải phóng hoàn toàn Raycast của Canvas Tutorial để không chặn UI bên dưới
        if (_canvas != null)
        {
            GraphicRaycaster gr = _canvas.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = _isActive; 
        }
    }

    private string PrefKey() => $"Tutorial_{scenario.scenarioId}_Done";

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Editor Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (scenario == null) return;
        foreach (TutorialStepData step in scenario.steps)
        {
            if (step.completionCondition == TutorialCompletionCondition.DrawInRegion ||
                step.completionCondition == TutorialCompletionCondition.PlayerReachPosition ||
                step.completionCondition == TutorialCompletionCondition.EraseAll ||
                step.completionCondition == TutorialCompletionCondition.ErasePartial)
            {
                GetWorldTargetInfo(step, out Vector2 targetPos, out float radius, out Vector2 size, out bool isRect);
                Gizmos.color = step.gizmoColor;
                
                if (isRect)
                {
                    Gizmos.DrawWireCube(targetPos, size);
                    Gizmos.DrawSphere(targetPos, 0.08f);
                }
                else
                {
                    Gizmos.DrawWireSphere(targetPos, radius);
                    Gizmos.DrawSphere(targetPos, 0.08f);
                }
            }
        }
    }
#endif

    #endregion
    public void ExitTutorial()
    {
        SceneManager.LoadScene("SampleScene");
    }
}