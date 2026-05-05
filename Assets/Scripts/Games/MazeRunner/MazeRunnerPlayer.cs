using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MazeRunnerPlayer : MonoBehaviour
{
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    bool inputEnabled = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 8f;
    }

    void FixedUpdate()
    {
        if (!inputEnabled)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input = input.normalized;
        rb.linearVelocity = input * moveSpeed;
    }

    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;
        if (!value && rb != null) rb.linearVelocity = Vector2.zero;
    }
}
