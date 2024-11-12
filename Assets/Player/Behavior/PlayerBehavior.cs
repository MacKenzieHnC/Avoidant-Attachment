using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviourScript : MonoBehaviour
{
    public float velocityParallel;
    public float velocityPerpendicular;
    public float velocityX;
    public float velocityY;

    public bool isSprinting = false;
    public int maxSpeed = 1000;
    public int baseSpeed = 1000;
    public int sprintSpeed = 2000;
    public int dashSpeed = 4000;
    public float acc = 100f;
    public int jumpSpeed = 1000;
    public int gravity = 60;
    public int terminalVelocity = -3000;
    public float gravityAccumulator = 0f;

    public SpriteRenderer sprite;
    private Rigidbody2D rb;

    public bool isGrounded = false;
    public bool isWallSliding = false;
    private float wallDirection = 1f;
    private float slideCountdown = 5;

    public PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;

    private InputAction dashAction;
    private bool canDash = false;
    private bool isDashing = false;
    private int dashFrames = 10;
    private int dashCounter = 0;
    private Vector3 dashDirection = Vector3.zero;

    private InputAction jumpAction;
    private int jumpCounter = 2;
    private bool isHoldingJump = false;
    private bool isJumping = false;
    private int jumpHoldFrames = 20;
    public int jumpHoldCounter = 0;

    private InputAction pauseAction;
    private CanvasGroup canvasGroup;
    private InputAction respawnAction;
    private bool isPaused = false;
    private bool pausePressed = false;

    private float timeScale = .5f;

    public Dictionary<string, int> contactBehaviorsDict = new Dictionary<string, int>() { { "NULL", 0 }, { "SETS_GROUNDED", 1 }, { "SETS_WALL_SLIDING", 2 } };
    public ContactPoint2D[] contactPoints = new ContactPoint2D[20];
    public Dictionary<int, int> contactIDsBehaviors = new Dictionary<int, int>() { };

    private ParticleSystem afterimage;

    public float percentSpeed = 0f;
    private readonly float WORLD_CONVERSION_UNIT = 49f;

    private void Start()
    {
        sprite = gameObject.GetComponent<SpriteRenderer>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        afterimage = gameObject.GetComponent<ParticleSystem>();

        jumpAction = InputSystem.actions.FindAction("Jump");
        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        dashAction = InputSystem.actions.FindAction("Dash");
        pauseAction = InputSystem.actions.FindAction("Pause");
    }

    public void HandleMove()
    {
        bool dashValue = dashAction.ReadValue<float>() > 0f;
        Vector2 moveValue = moveAction.ReadValue<Vector2>();

        if (dashValue)
        {
            if (!isDashing && canDash)
            {
                dashCounter = Mathf.RoundToInt(dashFrames / timeScale);
                isDashing = true;
                canDash = false;
            }


            if (isDashing && dashCounter > 0)
            {
                if (moveValue.magnitude == 0f)
                {
                    dashDirection = transform.forward;
                }
                else
                {
                    dashDirection = moveValue.normalized;
                }

                velocityParallel = dashDirection.x * baseSpeed * 4 / transform.right.x;
                velocityPerpendicular = dashDirection.y * baseSpeed * 2 / transform.up.y;
                Debug.Log(velocityPerpendicular);
                dashCounter--;
            }
            else
            {
                velocityParallel = Mathf.MoveTowards(velocityParallel, moveValue.x * maxSpeed, isWallSliding || isGrounded ? acc : acc / 10);
            }
        }
        else
        {
            isDashing = false;
            if (isGrounded || isWallSliding)
            {
                canDash = true;
            }
            if (!isWallSliding || isGrounded)
            {
                velocityParallel = Mathf.MoveTowards(velocityParallel, moveValue.x * maxSpeed, isGrounded ? acc : acc / 2);
            }
        }
    }

    public void HandleSprint()
    {
        bool sprintValue = sprintAction.ReadValue<float>() > 0f;
        if (sprintValue)
        {
            if (!isSprinting)
            {
                isSprinting = true;
                maxSpeed = baseSpeed * 2;
            }
        }
        else if (isSprinting)
        {
            isSprinting = false;
            maxSpeed = baseSpeed;
        }
    }

    private void HandleJump()
    {
        bool jumpValue = jumpAction.IsPressed();
        if (jumpValue)
        {
            if (!isHoldingJump && !isJumping && jumpCounter > 0)
            {
                jumpHoldCounter = Mathf.RoundToInt(jumpHoldFrames / timeScale);
                isHoldingJump = true;
                isJumping = true;
                jumpCounter--;
            }


            if (isHoldingJump && jumpHoldCounter > 0 && isJumping)
            {
                velocityPerpendicular = jumpSpeed;
                jumpHoldCounter--;
                if (isWallSliding && !isGrounded)
                {
                    velocityPerpendicular += jumpSpeed;
                    velocityParallel = baseSpeed / 2 * wallDirection;
                }
                gravityAccumulator = 0f;
            }
            else
            {
                isJumping = false;
            }
        }
        else
        {
            isHoldingJump = false;
            isJumping = false;
            if (isGrounded || isWallSliding)
            {
                jumpCounter = 2;
            }
            velocityPerpendicular = 0f;
        }
    }

    public void HandlePause()
    {
        if (pauseAction.IsPressed())
        {
            if (!pausePressed)
            {
                if (!isPaused)
                {
                    isPaused = true;
                    pausePressed = true;

                    Time.timeScale = 0f;
                    canvasGroup.alpha = 1f;
                    pauseAction.Enable();
                }
                else
                {
                    isPaused = false;
                    pausePressed = true;

                    Time.timeScale = 1f;
                    canvasGroup.alpha = 0f;
                    pauseAction.Enable();

                }
            }
        }
        else
        {
            pausePressed = false;
        }
    }

    public void OnRespawn(InputAction.CallbackContext context)
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    public void OnCollisionEnter2D(UnityEngine.Collision2D c)
    {
        int num = c.GetContacts(contactPoints);
        for (int i = 0; i < num; i++)
        {
            ContactPoint2D contactPoint = c.GetContact(i);

            float relativeAngle = Vector3.Angle(contactPoint.normal, Vector3.up);
            if (relativeAngle <= 45 && relativeAngle >= -45)
            {
                isGrounded = true;
                contactIDsBehaviors[contactPoint.collider.gameObject.GetInstanceID()] =
                    contactBehaviorsDict["SETS_GROUNDED"];
                transform.up = contactPoint.normal;
                gravityAccumulator = 0f;
            }
            else if (contactPoint.normal == Vector2.left || contactPoint.normal == Vector2.right)
            {
                isWallSliding = true;
                wallDirection = contactPoint.normal.x;
                contactIDsBehaviors[contactPoint.collider.gameObject.GetInstanceID()] =
                    contactBehaviorsDict["SETS_WALL_SLIDING"];
            }

            if (!isGrounded && isWallSliding)
            {
                jumpHoldCounter = 0;
                velocityPerpendicular = 0;
                velocityParallel = 0;
                slideCountdown = 5;
            }
        }
    }

    public void OnCollisionExit2D(Collision2D c)
    {
        isGrounded = false;
        isWallSliding = false;

        contactIDsBehaviors.Remove(c.gameObject.GetInstanceID());
        foreach (KeyValuePair<int, int> entry in contactIDsBehaviors)
        {
            if (entry.Value == contactBehaviorsDict["SETS_GROUNDED"])
            {
                isGrounded = true;
            }
            else if (entry.Value == contactBehaviorsDict["SETS_WALL_SLIDING"])
            {
                isWallSliding = true;
            }
        }
    }

    private void HandleAfterimage()
    {
        Gradient grad = new Gradient();
        grad.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(percentSpeed * percentSpeed / 2f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });

        var col = afterimage.main;
        col.startColor = new Color(255f, 255f, 255f, percentSpeed);
    }

    private void Update()
    {
        HandleAfterimage();
        HandlePause();
    }

    void FixedUpdate()
    {

        HandleSprint();
        HandleMove();
        HandleJump();

        velocityX = velocityParallel * transform.right.x +
            velocityPerpendicular * transform.up.x;
        velocityY = velocityParallel * transform.right.y +
            velocityPerpendicular * transform.up.y;

        if (!isGrounded && !isJumping)
        {
            gravityAccumulator = Mathf.MoveTowards(gravityAccumulator, terminalVelocity, gravity);
        }
        rb.linearVelocity = new Vector3(velocityX, velocityY + gravityAccumulator, 0.0f);


        percentSpeed = rb.linearVelocity.magnitude / dashSpeed;

        rb.linearVelocity *= timeScale * Time.deltaTime;
    }


}