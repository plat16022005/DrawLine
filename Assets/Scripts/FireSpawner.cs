using UnityEngine;
using System.Collections;

public class FireSpawner : MonoBehaviour
{
    [Header("Fire Settings")]
    [Tooltip("Prefab của lửa")]
    public GameObject firePrefab;

    [Tooltip("Các vị trí spawn lửa")]
    public Transform[] spawnPoints;

    [Header("Position Offset")]
    [Tooltip("Dịch vị trí spawn theo trục Y")]
    public float yOffset = 1.62f;

    [Header("Rotation Settings")]
    [Tooltip("Góc quay của lửa (VD: 0 = phải, 90 = lên, 180 = trái, -90 = xuống)")]
    public float fireAngle = 0f;

    [Header("Timing Settings")]
    [Tooltip("Thời gian lửa tồn tại")]
    public float fireDuration = 1f;

    [Tooltip("Thời gian chờ trước khi lửa xuất hiện lại")]
    public float respawnDelay = 3f;

    private void Start()
    {
        StartCoroutine(FireLoop());
    }

    IEnumerator FireLoop()
    {
        while (true)
        {
            GameObject[] spawnedFires = new GameObject[spawnPoints.Length];

            Quaternion fireRotation = Quaternion.Euler(0, 0, fireAngle);

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (firePrefab != null && spawnPoints[i] != null)
                {
                    // Tăng vị trí Y thêm 2.83
                    Vector3 spawnPos = spawnPoints[i].position + new Vector3(0f, yOffset, 0f);

                    spawnedFires[i] = Instantiate(
                        firePrefab,
                        spawnPos,
                        fireRotation
                    );
                }
            }

            Debug.Log($"Lửa xuất hiện! Góc: {fireAngle}, Y Offset: {yOffset}");

            yield return new WaitForSeconds(fireDuration);

            for (int i = 0; i < spawnedFires.Length; i++)
            {
                if (spawnedFires[i] != null)
                {
                    Destroy(spawnedFires[i]);
                }
            }

            Debug.Log("Lửa biến mất!");

            yield return new WaitForSeconds(respawnDelay);
        }
    }
}