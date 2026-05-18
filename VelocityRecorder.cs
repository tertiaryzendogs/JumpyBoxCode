using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VelocityRecorder : MonoBehaviour
{
    [Tooltip("Minimum speed to remember as 'non-zero'.")]
    [SerializeField] private float minSpeed = 0.1f;

    private Rigidbody2D rb;

    // Public read-only last non-zero velocity
    public Vector2 LastNonZero { get; private set; } = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > minSpeed * minSpeed)
        {
            LastNonZero = v;
        }
    }
}
