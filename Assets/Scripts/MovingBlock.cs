using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    [Header("Khoảng cách di chuyển")]
    [Tooltip("Di chuyển ngang")]
    public float moveX = 0f;

    [Tooltip("Di chuyển dọc")]
    public float moveY = 0f;

    [Header("Tốc độ")]
    public float speed = 2f;

    private Vector3 startPos;
    private Vector3 targetPos;

    void Awake()
    {
        // Lưu vị trí ban đầu (dùng Awake để ResetBlock() có thể khôi phục đúng khi StopSimulation)
        startPos = transform.position;

        // Tính vị trí đích dựa trên X và Y nhập vào
        targetPos = startPos + new Vector3(moveX, moveY, 0f);
    }

    void Update()
    {
        // Di chuyển qua lại liên tục giữa startPos và targetPos
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        // Khi tới đích thì đổi hướng quay lại
        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            // Đổi vị trí đích giữa điểm đầu và điểm cuối
            if (targetPos == startPos)
                targetPos = startPos + new Vector3(moveX, moveY, 0f);
            else
                targetPos = startPos;
        }
    }

    /// <summary>
    /// Gọi khi StopSimulation để đưa block về vị trí ban đầu.
    /// </summary>
    public void ResetBlock()
    {
        transform.position = startPos;
        targetPos = startPos + new Vector3(moveX, moveY, 0f);
    }
}