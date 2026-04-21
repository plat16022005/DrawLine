using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class Line : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public EdgeCollider2D edgeCollider;
    
    [Tooltip("Khoảng cách tối thiểu giữa 2 điểm để nét vẽ được tạo ra mượt mà mà không tốn quá nhiều hiệu năng")]
    public float pointsMinDistance = 0.1f;

    private List<Vector2> points;

    public void Initialize(LineType lineType)
    {
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
                slowMaterial.friction = 5f; // Thêm ma sát vật lý cao
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
}
