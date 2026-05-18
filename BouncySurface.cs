using UnityEngine;

public class BouncySurface : MonoBehaviour
{
    [SerializeField] private float wallBounceUpwardBoost = 5f; // Upward velocity to add when bouncing off walls
    [SerializeField] private bool isSuperBouncy = false; // If true, multiplies the bounce velocity by superBounceMultiplier
    [SerializeField] private float superBounceMultiplier = 2f; // Multiplier for bounce velocity when isSuperBouncy is true
    [SerializeField] private bool bounceAlongSurface = false; // If true, bounce along surface angle; if false, use reflection
    [SerializeField] private bool alwaysBounceUp = false; // If true, always bounce the player up if they hit the surface at all

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.Log($"No rigidbody found on {collision.gameObject.name}");
            return;
        }

        // Play bounce sound
        AudioManager.Instance.PlayBounceSound(transform.position);

        // Get the contact normal from the collision
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal;

        // Use relativeVelocity to get the actual impact velocity
        Vector2 relativeVel = collision.relativeVelocity;

        // Ensure normal points away from the surface (towards the incoming object)
        // by checking if it opposes the relative velocity direction
        if (Vector2.Dot(normal, relativeVel) > 0)
        {
            normal = -normal;
        }

        float dotProduct = Vector2.Dot(relativeVel, normal);

        Vector2 reflectedRelativeVelocity;

        if (bounceAlongSurface)
        {
            // Bounce perpendicular to the surface (along surface angle)
            // regardless of input direction
            // Preserve the speed of the incoming velocity
            float speed = relativeVel.magnitude;

            // Bounce along the normal direction (perpendicular to surface)
            // Normal already points away from surface, so just use it directly
            reflectedRelativeVelocity = normal * speed;
            if (alwaysBounceUp)
            {
                reflectedRelativeVelocity = new Vector2(normal.x, 10f + normal.y); // Example fixed bounce velocity along surface normal
            }
        }
        else
        {
            // Reflect relative velocity: v' = v - 2(v·n)n
            // This preserves speed while changing direction based on surface orientation
            reflectedRelativeVelocity = relativeVel - 2f * dotProduct * normal;

            // Check if this is a VERTICAL wall (normal is almost purely horizontal)
            // Only apply upward boost for near-vertical surfaces, not angled ones
            // For a wall normal to be purely vertical: y component should be near 0
            if (Mathf.Abs(normal.y) < 0.2f && Mathf.Abs(normal.x) > 0.8f)
            {
                // Add upward boost for wall bounces
                reflectedRelativeVelocity.y = Mathf.Max(reflectedRelativeVelocity.y, wallBounceUpwardBoost);
            }
        }

        // Apply super bounce multiplier if enabled
        if (isSuperBouncy)
        {
            reflectedRelativeVelocity *= superBounceMultiplier;
        }

        // Apply the reflected velocity
        rb.linearVelocity = reflectedRelativeVelocity;
    }
}
