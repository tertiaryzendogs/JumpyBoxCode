using UnityEngine;

[ExecuteAlways]
// Portal updates visuals in-editor via OnValidate/ExecuteAlways and handles teleportation logic at runtime.
// Notes:
// - Visuals are updated in the Editor so changing port color/symbol updates the scene immediately.
// - Teleport preserves/rewrites Rigidbody2D.linearVelocity and uses a VelocityRecorder fallback if velocity is lost
//   just before teleport (e.g., when the object hits a surface behind the portal).
public class Portal : MonoBehaviour
{
    [Header("Portal settings")]
    [SerializeField]
    private Transform exitPortal;

    [SerializeField]
    private bool onlyExit;

    [SerializeField]
    private bool removeVelocityOnExit;

    [SerializeField]
    private float exitOffset = 1f; // Distance past the exit portal to prevent re-triggering.

    [SerializeField]
    private PortalColor portalColor;

    [SerializeField]
    private PortalSymbol portalSymbol;

    [Header("Portal Symbol Settings")]
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite triangleSprite;
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Sprite diamondSprite;
    // Scale is set so that the image of the symbol is not squashed or stretched too much on the portal.
    [SerializeField][Tooltip("Local scale for the symbol child.")] private Vector3 symbolLocalScale = new Vector3(0.3f, 0.1f, 0.5f);
    [SerializeField]
    [Tooltip("Vertical offset (local Y) for the two symbol instances (top & bottom).")]
    private float symbolVerticalOffset = 0.2f;

    // Minimum linear speed (in units/sec) considered valid when teleporting. If the object's current speed
    // is below this threshold the Portal will attempt to use a recent non-zero speed recorded by
    // a `VelocityRecorder` component on the object. This avoids the case where the object hits a surface
    // immediately before teleporation and ends up teleporting with near-zero velocity.
    [SerializeField]
    [Tooltip("Minimum speed considered valid at teleport. If below this, portal will attempt to use a recorded recent velocity.")]
    private float minTeleportSpeed = 0.1f;

    [SerializeField]
    [Tooltip("Number of FixedUpdates to reapply the transferred velocity (helps against other physics overrides).")]
    private int velocityReapplyCount = 3;

    public enum PortalColor
    {
        // Blue connects to orange.
        blue,
        orange,
        // Green connects to red.
        green,
        red,
        // Yellow connects to violet.
        yellow,
        violet
    }

    public enum PortalSymbol
    {
        // Shapes for portal identification so that more than two portals with the same colors can exist.
        Star,
        Circle,
        Triangle,
        Square,
        Diamond
    }

    // At runtime ensure visuals are initialized (also runs in the Editor thanks to ExecuteAlways and OnValidate).
    void Start()
    {
        UpdateVisuals();
    }

    // Called in the editor when a serialized value changes or the script is loaded.
    // Together with [ExecuteAlways] this makes Inspector changes update immediately in the Scene view.
    private void OnValidate()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Update the portal colour on the main SpriteRenderer.
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = GetColorFromEnum(portalColor);

        // Ensure the symbol children match the selected enum and assigned sprites.
        Sprite sprite = GetSpriteFromEnum(portalSymbol);

        // Remove any legacy single Symbol child to avoid duplication.
        Transform legacy = transform.Find("Symbol");
        if (legacy != null)
        {
            if (Application.isPlaying) Destroy(legacy.gameObject);
            //else DestroyImmediate(legacy.gameObject);
            // Uncomment this if you go back to making more levels in the editor.
        }

        if (sprite == null)
        {
            // No sprite assigned: remove top/bottom symbol children if they exist.
            Transform tTop = transform.Find("Symbol_Top");
            Transform tBottom = transform.Find("Symbol_Bottom");
            if (tTop != null)
            {
                if (Application.isPlaying) Destroy(tTop.gameObject);
                //else DestroyImmediate(tTop.gameObject);
                // Uncomment this if you go back to making more levels in the editor.
            }
            if (tBottom != null)
            {
                if (Application.isPlaying) Destroy(tBottom.gameObject);
                //else DestroyImmediate(tBottom.gameObject);
                // Uncomment this if you go back to making more levels in the editor.
            }
            return;
        }

        // Ensure top/bottom symbol instances exist and are configured. They are kept as children so they follow portal rotation/scale.
        SpriteRenderer topSr = EnsureSymbolInstance("Symbol_Top", new Vector3(0f, symbolVerticalOffset, 0f), sprite, sr);
        SpriteRenderer bottomSr = EnsureSymbolInstance("Symbol_Bottom", new Vector3(0f, -symbolVerticalOffset, 0f), sprite, sr);
    }

    private SpriteRenderer EnsureSymbolInstance(string childName, Vector3 localPosition, Sprite sprite, SpriteRenderer portalSr)
    {
        Transform t = transform.Find(childName);
        GameObject symbol;
        if (t == null)
        {
            symbol = new GameObject(childName);
            symbol.transform.SetParent(transform, false);
        }
        else
        {
            symbol = t.gameObject;
        }

        SpriteRenderer sr = symbol.GetComponent<SpriteRenderer>();
        if (sr == null) sr = symbol.AddComponent<SpriteRenderer>();

        symbol.transform.localPosition = localPosition;
        symbol.transform.localRotation = Quaternion.identity;
        symbol.transform.localScale = symbolLocalScale;

        sr.sprite = sprite;
        if (portalSr != null)
        {
            sr.sortingLayerID = portalSr.sortingLayerID;
            sr.sortingOrder = portalSr.sortingOrder + 1;
        }
        else
        {
            sr.sortingOrder = 1;
        }
        sr.color = Color.white;

        return sr;
    }

    private Sprite GetSpriteFromEnum(PortalSymbol symbol)
    {
        switch (symbol)
        {
            case PortalSymbol.Star: return starSprite;
            case PortalSymbol.Circle: return circleSprite;
            case PortalSymbol.Triangle: return triangleSprite;
            case PortalSymbol.Square: return squareSprite;
            case PortalSymbol.Diamond: return diamondSprite;
            default: return null;
        }
    }

    private System.Collections.IEnumerator ApplyVelocityNextFixed(Rigidbody2D rb, Vector2 velocity, int repeats)
    {
        // Repeat reapplication over several FixedUpdates to reduce the chance that other physics callbacks (collisions/forces)
        // overwrite our intended velocity right after teleport. This is a pragmatic safeguard that keeps the transfer simple.
        int times = Mathf.Max(1, repeats);
        for (int i = 0; i < times; i++)
        {
            yield return new WaitForFixedUpdate();
            if (rb == null) yield break;
            rb.linearVelocity = velocity;
        }
        // End of repeated reapplication. We do not attempt to merge future impulses here; this is intended to faithfully transfer
        // the player's momentum across the teleport while avoiding complex interaction with other physics systems.
    }

    private Color GetColorFromEnum(PortalColor portalColor)
    {
        switch (portalColor)
        {
            case PortalColor.blue:
                return Color.blue;
            case PortalColor.orange:
                return new Color(1f, 0.5f, 0f); // RGB for orange
            case PortalColor.green:
                return Color.green;
            case PortalColor.red:
                return Color.red;
            case PortalColor.yellow:
                return Color.yellow;
            case PortalColor.violet:
                return new Color(0.5f, 0f, 1f); // RGB for violet
            default:
                return Color.white;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (onlyExit) return; // This portal is exit-only
        if (exitPortal == null) return; // No exit portal assigned
        // Check if the object is entering from the right side
        Vector2 directionToCollider = (collision.transform.position - transform.position).normalized;
        float dotProduct = Vector2.Dot(directionToCollider, transform.right);

        if (dotProduct < 0) return; // Only allow entry from the right side

        // Only teleport if the object's center has passed the portal's center
        Vector3 directionTraveled = collision.transform.position - transform.position;
        float distanceBeyondCenter = Vector3.Dot(directionTraveled, transform.right);

        if (distanceBeyondCenter < 0) return; // Object hasn't passed center yet

        // Calculate the vertical offset from the entrance portal's center
        float verticalOffset = Vector3.Dot(directionTraveled, transform.up);

        // Teleport to exit portal, preserving the entry point and positioned beyond it
        Vector3 exitPosition = exitPortal.position + exitPortal.up * verticalOffset + exitPortal.right * exitOffset;
        collision.transform.position = exitPosition;

        // Play teleport sound
        AudioManager.Instance.PlayTeleportSound(exitPosition);

        // Carry over velocity, transformed to match the exit portal's orientation
        if (collision.gameObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            if (exitPortal == null) return; // safety check

            // Use Vector2 math and the Rigidbody2D linearVelocity for consistent behaviour
            Vector2 velocity = rb.linearVelocity;
            // If the velocity is nearly zero (because the object hit a surface right before teleport),
            // try to use the last recorded non-zero velocity from a VelocityRecorder component if present.
            // This is a defensive fallback for the case where a collision right before teleport reduces velocity to zero.
            if (velocity.sqrMagnitude < minTeleportSpeed * minTeleportSpeed)
            {
                // This looks for a pre-attached VelocityRecorder on the object.
                var recorder = rb.GetComponent<VelocityRecorder>();
                if (recorder != null && recorder.LastNonZero.sqrMagnitude > minTeleportSpeed * minTeleportSpeed)
                {
                    // Use the recorder's last known non-zero velocity instead of the near-zero current velocity.
                    velocity = recorder.LastNonZero;
                }
                // If no recorder or recorder has no recent non-zero velocity, we keep the small velocity as-is.
            }

            Vector2 enterRight = (Vector2)transform.right;
            Vector2 enterUp = (Vector2)transform.up;

            float rightComponent = Vector2.Dot(velocity, enterRight);
            float upComponent = Vector2.Dot(velocity, enterUp);

            Vector2 newVelocity = (Vector2)exitPortal.right * rightComponent + (Vector2)exitPortal.up * upComponent;

            // Ensure the velocity has a positive component moving away from the exit portal.
            // This prevents the object from being sent backwards into the exit portal or stuck inside geometry.
            // We flip only the forward component (along exitPortal.right) so lateral components remain unchanged.
            float forwardComponent = Vector2.Dot(newVelocity, (Vector2)exitPortal.right);
            if (forwardComponent < 0f)
            {
                // Reverse the forward component while keeping the rest of `newVelocity` intact.
                newVelocity = newVelocity - 2f * forwardComponent * (Vector2)exitPortal.right;
                if (newVelocity.sqrMagnitude < 1e-6f)
                {
                    // If flipping loses magnitude (rare), fallback to reversing the original incoming velocity.
                    newVelocity = -velocity;
                }
            }

            if (removeVelocityOnExit)
            {
                newVelocity = Vector2.zero;
            }

            // Apply now and again after physics step to avoid other physics callbacks overwriting it.
            // We set the transformed velocity immediately and reapply it over several FixedUpdates as a robust measure.
            rb.linearVelocity = newVelocity;
            StartCoroutine(ApplyVelocityNextFixed(rb, newVelocity, velocityReapplyCount));
        }
    }
}
