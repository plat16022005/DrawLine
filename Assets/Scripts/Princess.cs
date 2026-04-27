using UnityEngine;
using System;

public class Princess : MonoBehaviour
{
    public static event Action<Vector3> OnPlayerWin;

    private bool isTriggered = false;
    public Sprite princessHappy;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckCollision(collision.gameObject, collision.contacts[0].point);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        CheckCollision(collider.gameObject, collider.ClosestPoint(transform.position));
    }

    private void CheckCollision(GameObject other, Vector2 contactPoint)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;

            OnPlayerWin?.Invoke(contactPoint);

            // Đổi sprite
            if (sr != null && princessHappy != null)
            {
                sr.sprite = princessHappy;
            }
        }
    }
}