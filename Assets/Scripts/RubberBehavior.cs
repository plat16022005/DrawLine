using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RubberBehavior : MonoBehaviour
{
    private Line line;
    private bool ropeCreated = false;
    private bool isBreaking = false;
    private List<Transform> ropeNodes = new List<Transform>();
    
    // Variables for breakpoint
    private int breakIndex = -1;
    private LineRenderer lineRenderer2;

    void Start()
    {
        line = GetComponent<Line>();
    }

    void Update()
    {
        // Khi game bắt đầu chạy, lập tức chuyển đổi nét vẽ thành dây
        if (GameController.isPlaying && !ropeCreated)
        {
            CreatePhysicsRope();
            ropeCreated = true;
        }

        // Cập nhật LineRenderer đi theo các đốt dây liên tục
        if (ropeCreated)
        {
            if (breakIndex == -1) // Dây chưa đứt
            {
                for (int i = 0; i < ropeNodes.Count; i++)
                {
                    if (ropeNodes[i] != null) line.lineRenderer.SetPosition(i, ropeNodes[i].position);
                }
            }
            else // Dây đã đứt làm 2 đoạn
            {
                // Cập nhật đoạn 1
                for (int i = 0; i < breakIndex; i++)
                {
                    if (ropeNodes[i] != null) line.lineRenderer.SetPosition(i, ropeNodes[i].position);
                }
                
                // Cập nhật đoạn 2
                if (lineRenderer2 != null)
                {
                    for (int i = breakIndex; i < ropeNodes.Count; i++)
                    {
                        if (ropeNodes[i] != null) lineRenderer2.SetPosition(i - breakIndex, ropeNodes[i].position);
                    }
                }
            }
        }
    }

    private void CreatePhysicsRope()
    {
        // 1. Tắt EdgeCollider tĩnh để tránh xung đột
        if (line.edgeCollider != null)
        {
            line.edgeCollider.enabled = false;
        }

        int count = line.lineRenderer.positionCount;
        if (count < 2) return;

        Rigidbody2D prevRb = null;

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = line.lineRenderer.GetPosition(i);
            
            GameObject node = new GameObject("RubberNode_" + i);
            node.transform.position = pos;
            node.transform.SetParent(this.transform);
            
            ropeNodes.Add(node.transform);

            Rigidbody2D rb = node.AddComponent<Rigidbody2D>();
            
            // Điểm bắt đầu và kết thúc ghim cứng lại để tạo thành dây treo giữa không trung
            if (i == 0 || i == count - 1)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = 0.2f;
                rb.drag = 0.5f; // Giảm xóc cho dây nảy nhẹ
            }

            // Tạo collider cho từng đốt để bóng rớt trúng được
            CircleCollider2D col = node.AddComponent<CircleCollider2D>();
            col.radius = line.lineRenderer.startWidth / 2f;
            
            // Lấy luôn thuộc tính ma sát/nảy của đường
            if (line.edgeCollider != null)
            {
                col.sharedMaterial = line.edgeCollider.sharedMaterial;
            }

            // Script bắt sự kiện gắn trên mỗi cục
            RubberNode nodeScript = node.AddComponent<RubberNode>();
            nodeScript.parentRubber = this;

            // Kéo khớp (Joint) bằng Lò Xo để tạo dây CAO SU đàn hồi
            if (prevRb != null)
            {
                SpringJoint2D spring = node.AddComponent<SpringJoint2D>();
                spring.connectedBody = prevRb;
                spring.autoConfigureDistance = true; // Tự gán khoảng cách dãn mặc định
                spring.dampingRatio = 0.7f; // Ít nhún lắc dữ dội
                spring.frequency = 10f; // Tần số cứng vừa phải để dây thun kéo giãn được
            }

            prevRb = rb;
        }
    }

    public void OnNodeHitBall()
    {
        if (isBreaking) return;
        isBreaking = true;
        StartCoroutine(BreakAfterDelay(5f));
    }

    private IEnumerator BreakAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Hết 3 giây -> Xác định vị trí võng sâu nhất để làm đứt dây
        float minY = float.MaxValue;
        int lowestIndex = -1;
        
        // Bỏ qua điểm 0 và điểm cuối vì chúng bị ghim
        for (int i = 1; i < ropeNodes.Count - 1; i++) 
        {
            if (ropeNodes[i] != null && ropeNodes[i].position.y < minY)
            {
                minY = ropeNodes[i].position.y;
                lowestIndex = i;
            }
        }

        if (lowestIndex != -1)
        {
            // Cắt đứt vật lý bằng cách hủy khớp nối lò xo ở điểm nối đó
            SpringJoint2D breakJoint = ropeNodes[lowestIndex].GetComponent<SpringJoint2D>();
            if (breakJoint != null)
            {
                Destroy(breakJoint);
            }

            // Lưu dấu vị trí cắt đứt
            breakIndex = lowestIndex;

            // Xử lý đồ hoạ: Đoạn gốc 1 chỉ vẽ đến đoạn đứt
            line.lineRenderer.positionCount = breakIndex;

            // Tạo line renderer mới để vẽ phần đứt còn lại
            GameObject half2 = new GameObject("BrokenLine2");
            half2.transform.SetParent(this.transform);
            lineRenderer2 = half2.AddComponent<LineRenderer>();
            
            // Sao chép toàn bộ thuộc tính hiển thị (chất liệu, kích cỡ)
            lineRenderer2.useWorldSpace = true;
            lineRenderer2.startWidth = line.lineRenderer.startWidth;
            lineRenderer2.endWidth = line.lineRenderer.endWidth;
            lineRenderer2.numCapVertices = line.lineRenderer.numCapVertices;
            lineRenderer2.numCornerVertices = line.lineRenderer.numCornerVertices;
            lineRenderer2.material = line.lineRenderer.material;
            
            // Số lượng điểm ảnh đoạn 2
            lineRenderer2.positionCount = ropeNodes.Count - breakIndex;
        }
    }
}

// Lớp phụ để bắt va chạm cho từng đốt dây
public class RubberNode : MonoBehaviour
{
    public RubberBehavior parentRubber;

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.attachedRigidbody != null)
        {
            // Báo lại cho cha để khởi động đếm ngược tự hủy
            if (parentRubber != null)
            {
                parentRubber.OnNodeHitBall();
            }
        }
    }
}
