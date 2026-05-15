using System;
using UnityEngine;

/// <summary>
/// Các loại bẫy có trong game.
/// </summary>
public enum TrapType
{
    FireSpawner,
    MovingBlock,
    BreakablePlatform,
    SlowDown,
    SpeedBoost
}

/// <summary>
/// Dữ liệu một bẫy được đặt trong map.
/// Chứa đầy đủ thông số của từng loại bẫy.
/// </summary>
[Serializable]
public class TrapData
{
    public TrapType type;
    public Vector3 position;

    // ── FireSpawner ─────────────────────────────
    /// <summary>Thời gian lửa tồn tại (giây).</summary>
    public float fireDuration = 1f;
    /// <summary>Thời gian chờ trước khi lửa xuất hiện lại.</summary>
    public float respawnDelay = 3f;
    /// <summary>Góc quay của lửa (0=phải, 90=lên, 180=trái, -90=xuống).</summary>
    public float fireAngle = 0f;
    /// <summary>Offset Y spawn lửa.</summary>
    public float yOffset = 1.62f;

    // ── MovingBlock ──────────────────────────────
    /// <summary>Khoảng di chuyển theo trục X.</summary>
    public float moveX = 0f;
    /// <summary>Khoảng di chuyển theo trục Y.</summary>
    public float moveY = 0f;
    /// <summary>Tốc độ di chuyển.</summary>
    public float speed = 2f;

    // ── BreakablePlatform ───────────────────────
    /// <summary>Thời gian trễ trước khi nền vỡ (giây).</summary>
    public float breakDelay = 1f;

    // ── SlowDown ─────────────────────────────────
    /// <summary>Hệ số làm chậm.</summary>
    public float slowRate = 10f;

    // SpeedBoost không có tham số bổ sung
}
