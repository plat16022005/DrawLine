using UnityEngine;

/// <summary>
/// Gắn script này vào GameObject cần quay tại chỗ.
/// Có thể chỉnh:
/// - Tốc độ quay (rotationSpeed)
/// - Góc quay ban đầu (startRotation)
/// </summary>
public class RotateObject : MonoBehaviour
{
    [Header("Cài đặt quay")]
    public float rotationSpeed = 50f; // độ/giây

    [Header("Góc quay ban đầu")]
    public Vector3 startRotation = Vector3.zero;

    [Header("Trái phải")]
    public float TraiPhai = 1f;

    private void Start()
    {
        // Đặt góc quay ban đầu
        transform.eulerAngles = startRotation;
    }

    private void Update()
    {
        // Quay quanh trục Y (có thể đổi thành X hoặc Z nếu muốn)
        transform.Rotate(0f, 0f, TraiPhai * rotationSpeed * Time.deltaTime);
    }

    public void ResetRotation()
    {
        transform.eulerAngles = startRotation;
    }
}