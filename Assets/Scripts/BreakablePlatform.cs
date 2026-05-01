using UnityEngine;
using System.Collections;

public class BreakablePlatform : MonoBehaviour
{
    public float breakDelay = 1f;

    private bool isBreaking = false;

    // Lưu trạng thái ban đầu để có thể khôi phục khi StopSimulation
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        // Chụp vị trí và góc quay ban đầu ngay khi object được tạo ra
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isBreaking && collision.gameObject.CompareTag("Player"))
        {
            isBreaking = true;
            Debug.Log("Sắp vỡ");

            StartCoroutine(BreakAfterDelay());
        }
    }

    IEnumerator BreakAfterDelay()
    {
        yield return new WaitForSeconds(breakDelay);

        // Ẩn đi thay vì Destroy để có thể khôi phục khi StopSimulation
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Gọi khi StopSimulation để khôi phục platform về trạng thái ban đầu.
    /// </summary>
    public void ResetPlatform()
    {
        StopAllCoroutines();
        isBreaking = false;
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        gameObject.SetActive(true);
    }
}