using UnityEngine;

public class LineCreator : MonoBehaviour
{
    private Line activeLine;

    // Lưu trạng thái hiện tại đang chọn bút màu gì
    public LineType currentLineType = LineType.Normal;

    // Hàm gọi từ nút UI để đổi sang Bút Đen
    public void SelectNormalPen()
    {
        currentLineType = LineType.Normal;
        Debug.Log("Lựa chọn Bút: ĐEN (Đứng yên)");
    }

    // Hàm gọi từ nút UI để đổi sang Bút Xanh
    public void SelectBouncyPen()
    {
        currentLineType = LineType.Bouncy;
        Debug.Log("Lựa chọn Bút: XANH LÁ CÂY (Phản lực nhún nhảy)");
    }

    // Hàm gọi từ nút UI để đổi sang Bút Tím (Dây cao su)
    public void SelectRubberPen()
    {
        currentLineType = LineType.Rubber;
        Debug.Log("Lựa chọn Bút: MÀU TÍM (Dây cao su đàn hồi)");
    }

    void Update()
    {
        // Nếu trò chơi đã diễn ra (thời gian đã chạy), người chơi sẽ không được phép vẽ thêm nữa.
        if (GameController.isPlaying) return;

        // Khi nhấn chuột trái xuống
        if (Input.GetMouseButtonDown(0))
        {
            // Thay vì dùng Prefab như thông thường, ta tự sinh ra GameObject hoàn toàn bằng code!
            GameObject lineGO = new GameObject("Drawn Line");
            // Tự cài Component Line vào vật thể vừa sinh (Code nó sẽ tự thêm LineRenderer và EdgeCollider)
            activeLine = lineGO.AddComponent<Line>();
            activeLine.Initialize(currentLineType);
        }

        // Khi nhả chuột trái ra
        if (Input.GetMouseButtonUp(0))
        {
            activeLine = null;
        }

        // Nếu đang trong quá trình vẽ
        if (activeLine != null)
        {
            // Chuyển tọa độ màn hình (pixel) của chuột sang tọa độ thế giới (world space) trong 2D
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            activeLine.UpdateLine(mousePos);
        }
    }
}
