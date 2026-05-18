using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
[RequireComponent(typeof(LineRenderer))]

public class PlayerControl : MonoBehaviour
{
    #region Variables
    private InputAction jumpAction;
    private InputAction jumpPosition;
    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private LineRenderer lineR;
    private float g;
    private InputActionAsset inputActions;
    public int resolution = 15;
    public int extraJumps = 1;
    public int maxExtraJumps = 1;
    public bool touchingASurface = false;
    // removed: private bool wasTouchingSurface = false;
    public bool canStick = true;
    public bool jumping = false;
    public bool jumpHeld = false;
    public bool failedJump = false;
    public bool alternativeJumpMode = false; // if true, jump direction is relative to the touch direction
    public int jumps = 0;
    public int[] scoreBreakpoints = new int[3];
    public Vector2 beforeJumpPosition;
    public Vector2 beforeJumpVelocity;
    public Vector2 maxVelocity = new Vector2(20f, 20f);
    public float multiplier = 1f;
    public Vector3 startPos;
    private int surfaceContacts = 0;
    private int blackSurfaceContacts = 0;
    private int blackSurfaceUnderContacts = 0;
    private float blackSurfaceTimer = 0f;
    private int bouncyContacts = 0;
    private float bouncySurfaceTimer = 0f;
    private float lastBouncySurfaceContactTime = 0f;
    private int bouncyLandingCount = 0;
    private HashSet<Collider2D> blackSurfaceUnderneath = new HashSet<Collider2D>();

    [Header("Arc Indicator")]
    public Transform arrowTip; // Assign a small arrow sprite/transform that points right (x+)
    public float arrowTipScale = 1f;

    #endregion

    void Awake()
    {
        // Get the components.
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        lineR = GetComponent<LineRenderer>();
        lineR.enabled = false;
        if (arrowTip != null)
        {
            arrowTip.gameObject.SetActive(false);
            arrowTip.localScale = Vector3.one * arrowTipScale;
        }
        beforeJumpPosition = body.position;
        beforeJumpVelocity = body.linearVelocity;
        inputActions = MainMenuController.Instance.inputActions;
        jumpAction = inputActions.FindActionMap("Player").FindAction("Jump");
        jumpAction.started += OnJumpStarted;
        jumpAction.canceled += OnJumpReleased;
        jumpPosition = inputActions.FindActionMap("Player").FindAction("JumpPosition");
        g = Mathf.Abs(Physics2D.gravity.y);
        alternativeJumpMode = MainMenuController.Instance.alternativeJumpMode;
    }

    // Update is called once per frame
    void Update()
    {
        body.freezeRotation = true;

        // Update arc preview while jump is held
        if (jumpHeld)
        {
            // Set line color based on whether player can jump
            Color arcColor = (extraJumps == 0 && !touchingASurface) ? Color.red : Color.green;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(arcColor, 0.0f), new GradientColorKey(arcColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            );
            lineR.colorGradient = gradient;

            Vector2 predictedVelocity = CalculateJumpVelocity() * multiplier;
            ShowArc(predictedVelocity);
        }
        else
        {
            // Ensure the arc/arrow is hidden when not holding jump
            if (lineR.enabled)
                lineR.enabled = false;

            if (arrowTip != null && arrowTip.gameObject.activeSelf)
                arrowTip.gameObject.SetActive(false);
        }

        // Check for restart on black surface floor (underneath player) when horizontal movement has stopped
        bool isOnBlackSurfaceFloor = blackSurfaceUnderContacts > 0;
        if (isOnBlackSurfaceFloor && extraJumps == 0 && Mathf.Abs(body.linearVelocity.x) < 0.25f)
        {
            blackSurfaceTimer += Time.deltaTime;
            if (blackSurfaceTimer >= 0.2f)
            {
                // Restart the game
                AudioManager.Instance.PlayDeathSound();
                MainMenuController.Instance.ChangeGameScreen(MainMenuController.Instance.gameMenu, MainMenuController.Instance.levelExitMenu, true);
                MainMenuController.Instance.quitToMainMenuButton.style.display = DisplayStyle.Flex;
                MainMenuController.Instance.retryLevelButton1.style.display = DisplayStyle.Flex;
                Time.timeScale = 0f;
                MainMenuController.Instance.inputActions.FindActionMap("Player").Disable();
                MainMenuController.Instance.UpdateEndScreen(false);
                this.gameObject.SetActive(false);
            }
        }
        else
        {
            blackSurfaceTimer = 0f;
        }

        // Check for restart on bouncy surface with only vertical movement
        // Use lastBouncySurfaceContactTime to persist timer through brief contact breaks (bounces)
        bool wasRecentlyOnBouncySurface = Time.time - lastBouncySurfaceContactTime < 1.0f;

        if (wasRecentlyOnBouncySurface)
        {
        }
        if (wasRecentlyOnBouncySurface && bouncyLandingCount >= 2 && extraJumps == 0 && Mathf.Abs(body.linearVelocity.x) < 0.25f)
        {
            bouncySurfaceTimer += Time.deltaTime;
            if (bouncySurfaceTimer >= 0.2f)
            {
                // Restart the game
                AudioManager.Instance.PlayDeathSound();
                MainMenuController.Instance.ChangeGameScreen(MainMenuController.Instance.gameMenu, MainMenuController.Instance.levelExitMenu, true);
                MainMenuController.Instance.quitToMainMenuButton.style.display = DisplayStyle.Flex;
                MainMenuController.Instance.retryLevelButton1.style.display = DisplayStyle.Flex;
                Time.timeScale = 0f;
                MainMenuController.Instance.inputActions.FindActionMap("Player").Disable();
                MainMenuController.Instance.UpdateEndScreen(false);
                this.gameObject.SetActive(false);
            }
        }
        else
        {
            bouncySurfaceTimer = 0f;
        }
    }

    Vector2 CalculateJumpVelocity()
    {
        // Calculate jump direction and force based on current cursor position
        // (screen space) relative to the player's screen position.
        Vector2 touchPos = jumpPosition.ReadValue<Vector2>();
        // Clamp to screen edges to avoid off-screen wrap/flip issues.
        touchPos.x = Mathf.Clamp(touchPos.x, 0f, Screen.width);
        touchPos.y = Mathf.Clamp(touchPos.y, 0f, Screen.height);

        Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        float x = (touchPos.x - playerScreenPos.x) / 33.3f;
        // The +25 is to offset for the player's pivot being in the center of the sprite.
        float y = (touchPos.y - playerScreenPos.y + 25f) / 33.3f;

        Vector2 velocity = new Vector2(Math.Clamp(x, -12.5f, 12.5f), Math.Clamp(y, -12.5f, 12.5f));

        // Invert the jump direction in the normal game mode so the arc appears opposite the touch.
        // If alternativeJumpMode is true, we jump in the direction of where the touch is.
        if (!alternativeJumpMode)
        {
            velocity = -velocity;
        }

        return velocity;
    }

    void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (this == null)
        {
            return;
        }
        jumpHeld = true;
    }

    void OnJumpReleased(InputAction.CallbackContext context)
    {
        if (this == null)
        {
            return;
        }
        jumpHeld = false;
        Jump();
    }

    void Jump()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }
        if (extraJumps > 0 && !touchingASurface)
        {
            extraJumps--;
        }
        else if (extraJumps == 0 && !touchingASurface)
        {
            // Out of jumps - play sound and return
            AudioManager.Instance.PlayOutOfJumpsSound();
            lineR.enabled = false;
            failedJump = true;
            return;
        }
        failedJump = false;
        jumping = true;
        jumps++;
        MainMenuController.Instance.UpdateJumps();
        StartCoroutine(StickyCooldown());
        body.gravityScale = 1f;
        body.freezeRotation = false;
        beforeJumpPosition = body.position;
        beforeJumpVelocity = body.linearVelocity;
        Vector2 velocity = CalculateJumpVelocity();
        body.linearVelocity = velocity * multiplier;
        // Play jump sound
        AudioManager.Instance.PlayJumpSound();
        // Play the sound that shows the player that they are getting off the sticky surface.
        if (!canStick)
        {
            AudioManager.Instance.PlayStickyOffSound();
        }
        // Hide arc after jump is executed
        lineR.enabled = false;
        if (arrowTip != null)
            arrowTip.gameObject.SetActive(false);
    }

    // function to undo the jump that was made when pressing the options button ingame
    public void ResetPositionAndJump()
    {
        if (failedJump)
        {
            return;
        }
        body.position = beforeJumpPosition;
        body.linearVelocity = beforeJumpVelocity;
        ShowArc(beforeJumpVelocity);
        if (beforeJumpVelocity.magnitude == 0 && touchingASurface || extraJumps >= 0)
        {
            if (extraJumps != maxExtraJumps)
            {
                extraJumps++;
            }
            jumps--;
        }
    }

    // Shows the jump arc preview based on the current cursor/touch position and predicted jump velocity
    void ShowArc(Vector2 velocity)
    {
        lineR.enabled = true;
        lineR.positionCount = resolution + 1;

        // Set line color based on whether player can jump
        Color arcColor = (extraJumps == 0 && !touchingASurface) ? Color.red : Color.green;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(arcColor, 0.0f), new GradientColorKey(arcColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        lineR.colorGradient = gradient;

        Vector2 startPos = body.position;
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector2 position = startPos + t * velocity;
            position.y = position.y - (0.5f * g * t * t);
            lineR.SetPosition(i, position);
        }

        // Place an arrow tip at the end of the arc
        if (arrowTip != null && lineR.positionCount >= 2)
        {
            Vector3 tipPos = lineR.GetPosition(lineR.positionCount - 1);
            Vector3 prevPos = lineR.GetPosition(lineR.positionCount - 2);
            Vector3 dir = (tipPos - prevPos).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowTip.position = tipPos;
            arrowTip.rotation = Quaternion.Euler(0f, 0f, angle);
            arrowTip.gameObject.SetActive(true);
        }
    }

    // sticky cooldown coroutine
    IEnumerator StickyCooldown()
    {
        canStick = false;
        yield return new WaitForSeconds(0.01f);
        canStick = true;
    }

    #region collision callbacks

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider == null) return;

        // only care about surface layer
        if (collision.collider.gameObject.layer != LayerMask.NameToLayer("Surface"))
            return;

        surfaceContacts++;
        bool isBlack = collision.collider.CompareTag("BlackSurface");
        bool isBouncy = collision.collider.CompareTag("BouncySurface");

        if (isBlack)
        {
            blackSurfaceContacts++;
            // Check if contact normal points upward (floor contact)
            if (collision.contactCount > 0)
            {
                ContactPoint2D contact = collision.GetContact(0);
                if (contact.normal.y > 0.5f)
                {
                    blackSurfaceUnderContacts++;
                    blackSurfaceUnderneath.Add(collision.collider);
                }
            }
        }
        else if (isBouncy)
        {
            bouncyContacts++;
            lastBouncySurfaceContactTime = Time.time;
            bouncyLandingCount++;
        }
        else
        {
            touchingASurface = true;
            extraJumps = maxExtraJumps;
            bouncyLandingCount = 0; // Reset bouncy landing count when landing on normal surface
            HandleSticky(collision.collider);
        }
        touchingASurface = (surfaceContacts - blackSurfaceContacts - bouncyContacts) > 0;

        // impact sound from relative velocity (any direction)
        float impact = collision.relativeVelocity.magnitude;
        if (impact > 3f)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            AudioManager.Instance.PlaySurfaceHitSound(contactPoint, impact);
        }
    }
    //a
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider == null) return;

        if (collision.collider.gameObject.layer != LayerMask.NameToLayer("Surface"))
            return;

        bool isBlack = collision.collider.CompareTag("BlackSurface");
        bool isBouncy = collision.collider.CompareTag("BouncySurface");
        // Only restore jumps if touching a non-black, non-bouncy surface
        if (!isBlack && !isBouncy)
        {
            touchingASurface = (surfaceContacts - blackSurfaceContacts - bouncyContacts) > 0;
            extraJumps = maxExtraJumps;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == null) return;

        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Surface"))
        {
            surfaceContacts--;
            if (collision.collider.CompareTag("BlackSurface"))
            {
                blackSurfaceContacts--;
                if (blackSurfaceUnderneath.Contains(collision.collider))
                {
                    blackSurfaceUnderContacts--;
                    blackSurfaceUnderneath.Remove(collision.collider);
                }
            }
            else if (collision.collider.CompareTag("BouncySurface"))
            {
                bouncyContacts--;
                if (bouncyContacts == 0)
                {
                    // Don't reset landingCount here, let it persist through brief bounce separations
                }
            }
            touchingASurface = (surfaceContacts - blackSurfaceContacts - bouncyContacts) > 0;
        }
    }

    private void HandleSticky(Collider2D col)
    {
        var mat = col.sharedMaterial;
        if (mat != null && mat.name == "Sticky" && canStick)
        {
            canStick = false;
            body.linearVelocity = Vector2.zero;
            body.freezeRotation = true;
            body.gravityScale = 0f;
            AudioManager.Instance.PlayStickyOnSound();
        }
    }

    #endregion
}