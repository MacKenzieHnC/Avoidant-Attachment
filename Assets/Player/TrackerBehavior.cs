using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem.Android;

public class TrackerBehavior : MonoBehaviour
{

    public GameObject trackedObject;
    private Rigidbody2D rb;
    private PlayerBehaviourScript playerScript;
    public float bezierTime = 1f / 5f;

    public bool isTrackingX = false;
    private float targetX = 0f;
    public float counterX = 0f;
    private float velocityX = 0f;
    private float directionX = 0;
    private float allowedDistanceX = 15f;

    public bool isTrackingY = false;
    private float targetY = 0f;
    private float counterY = 0f;
    private float velocityY = 0f;
    private float directionY = 0;
    public float allowedDistanceY = 11f;

    public bool playerIsAirborne = true;

    public AnimationCurve curve;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = trackedObject.GetComponent<Rigidbody2D>();
        playerScript = trackedObject.GetComponent<PlayerBehaviourScript>();
    }

    float BezierBlend(float t)
    {
        return t * t * (3.0f - 2.0f * t);
    }

    private void CalculateVelocityX()
    {
        targetX = rb.position.x;
        velocityX = 0;

        if (!isTrackingX && Mathf.Abs(transform.position.x - targetX) > allowedDistanceX)
        {
            isTrackingX = true;
            counterX = 0;
            directionX = rb.linearVelocityX;
            targetX = transform.position.x;
        }

        if (isTrackingX)
        {
            if (counterX < bezierTime)
            {
                counterX += Time.deltaTime;
                velocityX = (targetX - transform.position.x) * curve.Evaluate(Mathf.Clamp01(counterX/bezierTime));
            }
            else
            {
                if (rb.linearVelocityX * directionX <= 0)
                {
                    isTrackingX = false;
                }
                velocityX = targetX - transform.position.x;
            }
        }
    }

    private void CalculateVelocityY()
    {
        targetY = rb.position.y;
        velocityY = 0;

        if (!isTrackingY && (Mathf.Abs(transform.position.y - targetY) > allowedDistanceY || (playerIsAirborne && playerScript.isGrounded)))
        {
            if (playerScript.isGrounded)
            {
                playerIsAirborne = false;
            }
            isTrackingY = true;
            counterY = 0;
            directionY = rb.linearVelocityY;
            targetY = transform.position.y;
        }

        if (isTrackingY)
        {
            if (counterY < bezierTime)
            {
                counterY += Time.deltaTime;
                velocityY = (targetY - transform.position.y) * curve.Evaluate(Mathf.Clamp01(counterY / bezierTime));
            }
            else
            {
                if (rb.linearVelocityY * directionY <= 0)
                {
                    isTrackingY = false;
                }
                velocityY = targetY - transform.position.y;
            }
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!playerScript.isGrounded)
        {
            playerIsAirborne = true;
        }
        CalculateVelocityX();
        CalculateVelocityY();
        transform.position = new Vector3(transform.position.x + velocityX, transform.position.y + velocityY, 0);
        
    }

}

