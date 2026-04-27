using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class Line : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public EdgeCollider2D edgeCollider;
    
    [Tooltip("Khoảng cách tối thiểu giữa 2 điểm để nét vẽ được tạo ra mượt mà mà không tốn quá nhiều hiệu năng")]
    public float pointsMinDistance = 0.1f;

    // Ghi nhớ loại đường để khi tách đoạn vẫn giữ nguyên tính chất
    [HideInInspector] public LineType myType;

    private List<Vector2> points;

    public void Initialize(LineType lineType)
    {
        myType = lineType;
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();
        points = new List<Vector2>();

        // Tắt collider ban đầu, vì EdgeCollider2D cần ít nhất 2 điểm để hoạt động
        edgeCollider.enabled = false;
        
        // --- THIẾT LẬP ĐỒ HỌA BẰNG CODE (Không cần phải cấu hình ở Inspector) ---
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.15f; 
        lineRenderer.endWidth = 0.15f;
        lineRenderer.numCapVertices = 5; // Bo tròn 2 đầu nét vẽ
        lineRenderer.numCornerVertices = 5; // Bo tròn các nếp gấp

        // Gán Material mặc định (Sprites/Default có sẵn trong mọi bản Unity) để không bị viền hồng
        Shader defaultShader = Shader.Find("Sprites/Default");
        if (defaultShader != null)
        {
            Material mat = new Material(defaultShader);
            
            if (lineType == LineType.Bouncy)
            {
                mat.color = Color.green; // Nét vẽ nảy có màu Xanh Lá
                
                // Cài đặt thuộc tính nảy vật lý bằng code
                PhysicsMaterial2D bouncyMaterial = new PhysicsMaterial2D("GreenBouncy");
                bouncyMaterial.bounciness = 1.3f; // Hệ số phản lực nảy cực mạnh
                bouncyMaterial.friction = 0.4f;
                edgeCollider.sharedMaterial = bouncyMaterial;
            }
            else if (lineType == LineType.Rubber)
            {
                mat.color = new Color(0.6f, 0.1f, 0.9f); // Nét vẽ cao su có màu Tím
                
                PhysicsMaterial2D rubberMaterial = new PhysicsMaterial2D("PurpleRubber");
                rubberMaterial.bounciness = 0.0f;
                rubberMaterial.friction = 0.6f;
                edgeCollider.sharedMaterial = rubberMaterial;
                
                // Thêm script RubberBehavior để xử lý đàn hồi
                gameObject.AddComponent<RubberBehavior>();
            }
            else if (lineType == LineType.SpeedBoost)
            {
                mat.color = Color.red; // Nét vẽ tăng tốc màu Đỏ
                
                PhysicsMaterial2D speedMaterial = new PhysicsMaterial2D("RedSpeed");
                speedMaterial.bounciness = 0.0f;
                speedMaterial.friction = 0.4f; // Không ma sát để giữ vận tốc
                edgeCollider.sharedMaterial = speedMaterial;
                
                // Thêm script SpeedBoostBehavior để buff tốc độ và giữ nguyên vận tốc
                gameObject.AddComponent<SpeedBoostBehavior>();
            }
            else if (lineType == LineType.ConstantSpeed)
            {
                mat.color = Color.blue; // Nét vẽ giữ vận tốc màu Xanh Dương
                
                PhysicsMaterial2D constantMaterial = new PhysicsMaterial2D("BlueIce");
                constantMaterial.bounciness = 0.0f;
                constantMaterial.friction = 0.0f; // Lực ma sát bằng 0
                edgeCollider.sharedMaterial = constantMaterial;
                
                // Thêm script ConstantSpeedBehavior để giữ nguyên tốc độ của bóng
                gameObject.AddComponent<ConstantSpeedBehavior>();
            }
            else if (lineType == LineType.SlowDown)
            {
                mat.color = new Color(0.5f, 0.25f, 0.0f); // Nét vẽ màu Nâu (Brown)
                
                PhysicsMaterial2D slowMaterial = new PhysicsMaterial2D("BrownMud");
                slowMaterial.bounciness = 0.0f;
                slowMaterial.friction = 10f; // Thêm ma sát vật lý cao
                edgeCollider.sharedMaterial = slowMaterial;
                
                // Thêm script để rút vận tốc thành 0 từ từ như đi vào bùn lầy
                gameObject.AddComponent<SlowDownBehavior>();
            }
            else
            {
                mat.color = Color.black; // Nét vẽ bình thường có màu Đen
                
                // Cài đặt vật liệu bình thường (không nảy)
                PhysicsMaterial2D normalMaterial = new PhysicsMaterial2D("BlackSolid");
                normalMaterial.bounciness = 0.0f;
                normalMaterial.friction = 0.4f;
                edgeCollider.sharedMaterial = normalMaterial;
            }

            lineRenderer.material = mat;
        }

        // Tự động gán layer Ground cho đường vẽ
        // Tự động gán layer Ground cho đường vẽ (trừ line xanh)
        if (lineType != LineType.Bouncy)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1)
            {
                gameObject.layer = groundLayer;
            }
            else
            {
                Debug.LogWarning("Chưa có layer 'Ground' trong Unity!");
            }
        }
    }

    // Hàm này được dùng nội bộ để tái tạo đường từ một danh sách điểm có sẵn
    // (dùng khi tách các mảnh sau khi bị tẩy)
    public void InitializeFromPoints(List<Vector2> existingPoints, LineType lineType)
    {
        Initialize(lineType);
        foreach (var p in existingPoints)
        {
            SetPoint(p);
        }
    }

    public void UpdateLine(Vector2 mousePos)
    {
        if (points == null)
            points = new List<Vector2>();

        // Nếu chưa có điểm nào, hoặc khoảng cách từ điểm cuối đến vị trí hiện tại của chuột lớn hơn khoảng cách tối thiểu
        if (points.Count == 0 || Vector2.Distance(points.Last(), mousePos) > pointsMinDistance)
        {
            SetPoint(mousePos);
        }
    }

    private void SetPoint(Vector2 point)
    {
        if (points == null) points = new List<Vector2>();
        points.Add(point);

        // Cập nhật hiển thị đồ họa
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPosition(points.Count - 1, point);

        // Cập nhật biên dạng vật lý
        if (points.Count > 1)
        {
            edgeCollider.points = points.ToArray();
            
            // Bật physics collider sau khi đã có 2 điểm trở lên
            if (!edgeCollider.enabled)
            {
                edgeCollider.enabled = true;
            }
        }
    }

    // --- CORE CỦA CHỨC NĂNG TẨY XÓA ---
    // Hàm này nhận vào vị trí cục tẩy và bán kính, xóa các điểm nằm trong vùng đó.
    // Nếu đường bị cắt làm nhiều đoạn, sẽ sinh ra các GameObject mới cho từng đoạn.
    // Trả về true nếu đối tượng này cần bị Destroy (rỗng/chỉ còn 1 điểm)
    // refundedLength = tổng độ dài các đoạn bị xóa (để hoàn mực)
    public bool EraseAt(Vector2 eraserPos, float radius, out float refundedLength)
    {
        refundedLength = 0f;
        if (points == null || points.Count == 0) return true;

        // 1. Đánh dấu các điểm nằm trong vòng tròn cục tẩy
        bool[] toErase = new bool[points.Count];
        bool anyErased = false;

        for (int i = 0; i < points.Count; i++)
        {
            if (Vector2.Distance(points[i], eraserPos) <= radius)
            {
                toErase[i] = true;
                anyErased = true;
            }
        }

        if (!anyErased) return false; // Không có gì bị xóa, giữ nguyên

        // Tính độ dài các đoạn bị xóa (để hoàn mực)
        // Một segment i→(i+1) bị mất nếu BẤT KỲ đầu nào của nó nằm trong vùng xóa
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (toErase[i] || toErase[i + 1])
                refundedLength += Vector2.Distance(points[i], points[i + 1]);
        }

        // 2. Tách danh sách thành các "mảnh" liên tiếp (các điểm không bị xóa)
        List<List<Vector2>> segments = new List<List<Vector2>>();
        List<Vector2> currentSegment = new List<Vector2>();

        for (int i = 0; i < points.Count; i++)
        {
            if (!toErase[i])
            {
                currentSegment.Add(points[i]);
            }
            else
            {
                if (currentSegment.Count > 0)
                {
                    segments.Add(currentSegment);
                    currentSegment = new List<Vector2>();
                }
            }
        }
        // Đừng quên đoạn cuối cùng
        if (currentSegment.Count > 0)
            segments.Add(currentSegment);

        // 3. Nếu không còn đoạn nào hợp lệ, xóa đối tượng này
        if (segments.Count == 0)
            return true;

        // 4. Dùng đoạn đầu tiên để cập nhật đối tượng hiện tại
        RebuildFromPoints(segments[0]);

        // 5. Với các đoạn còn lại (từ đoạn thứ 2 trở đi), tạo GameObject mới cho mỗi đoạn
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i].Count >= 2) // Cần ít nhất 2 điểm để tạo nét vẽ có ý nghĩa
            {
                GameObject newLineGO = new GameObject("Drawn Line (Split)");
                Line newLine = newLineGO.AddComponent<Line>();
                newLine.InitializeFromPoints(segments[i], myType);
            }
        }

        // Nếu đoạn đầu tiên không đủ điểm, báo xóa đối tượng này
        return segments[0].Count < 2;
    }

    // Tái cấu trúc đường dựa trên danh sách điểm mới (sau khi tẩy)
    private void RebuildFromPoints(List<Vector2> newPoints)
    {
        points = newPoints;

        lineRenderer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }

        if (points.Count >= 2)
        {
            edgeCollider.points = points.ToArray();
            edgeCollider.enabled = true;
        }
        else
        {
            edgeCollider.enabled = false;
        }
    }
}
