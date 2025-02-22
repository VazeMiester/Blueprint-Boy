using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public float speed;               // Horizontal movement speed
    public float jumpForce;          // Jump force
    public float rotationSpeed;      // Rotation speed
    private Rigidbody2D rb2D;                // Reference to the Rigidbody2D component
    private bool isGrounded = true;          // Check if the player is grounded
    public Transform spawnPoint;             // Reference spawn location
    private Quaternion targetRotation;       // The target roation for the player
    private Quaternion rotationToAdd;        // The rotation that needs to be added to the target angle when the player rotates
    public string gravityDirection = "down"; // The current direction of gravity
    public float speedLimit { get; private set; }            // The velocity of the player where force stops being added when the movement key is pressed
    public float sprintingSpeedLimit;
    public float scaleValue = 1;             // A value representing the current scale of the game
    private float currentZoom;               // the current camera zoom
    private float targetZoom;                // the new zoom after scaling
    private Vector2 currentScale;            // the current scale of the player
    private Vector2 targetScale;             // the new scale of the player after scaling
    public float scaleSpeed;          // the amount of time it takes for the player to scale in seconds
    public bool stillScaling = false;        // a bool indicating if scaling is in progress or not
    public CinemachineVirtualCamera virtualCamera;
    private bool isSprinting = false;
    public const int leftRotationIncrement = -90;
    public const int rightRotationIncrement = 90;
    public const float scaleUpIncrement = 2f;
    public const float scaleDownIncrement = 0.5f;
    public float launchForce;
    Vector2[] scaleLaunchVectors;
    Collider2D[] scaleLaunchColliders;
    bool[] isScaleLaunchVectorActive;
    private float numberOfActiveScaleLaunchVectors;

    public enum playerMoveState
    {
        limbo,
        onGround,
        onGroundSprinting,
        inAir,
        inAirSprinting,
    }

    private playerMoveState _currentMoveState;
    public playerMoveState currentMoveState
    {
        get => _currentMoveState;
        set
        {
            _currentMoveState = value;
            setSpeedLimit(_currentMoveState);
        }
    }

    private void setSpeedLimit(playerMoveState state)
    {
        switch (state)
        {
            case playerMoveState.onGround:
                speedLimit = 5;
                break;
            case playerMoveState.onGroundSprinting:
                speedLimit = 10;
                break;
            case playerMoveState.inAir:
                speedLimit = 2;
                break;
            case playerMoveState.inAirSprinting:
                speedLimit = 4;
                break;
        }
    }

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();

        targetRotation = transform.rotation; // set intial target rotation to the current rotation

        scaleLaunchColliders = GetComponentsInChildren<Collider2D>();
        scaleLaunchVectors = new Vector2[scaleLaunchColliders.Length];
        isScaleLaunchVectorActive = new bool[scaleLaunchColliders.Length];
    }

    void Update()
    {
        // control horizontal movement
        float moveHorizontal = Input.GetAxisRaw("Horizontal");         //get the button input for horizontal movement
        Vector2 localRight = transform.right * moveHorizontal * speed; //normalize the vector for local orientation and muliply by speed

        
        // move using velocity
        //if (gravityDirection == "down" || gravityDirection == "up")    //move horizontally based on the players orientation
        //{
        //    rb2D.velocity = new Vector2(localRight.x, rb2D.velocity.y);
        //}
        //else if (gravityDirection == "right" || gravityDirection == "left")
        //{
        //    rb2D.velocity = new Vector2(rb2D.velocity.x, localRight.y); 
        //}

        //move using force
        Vector2 localVelocity = transform.InverseTransformDirection(rb2D.velocity);
        float horizontalLocalVelocity = localVelocity.x;

        if ((horizontalLocalVelocity <= speedLimit && moveHorizontal == 1) || (horizontalLocalVelocity >= -speedLimit && moveHorizontal == -1))
        {
            rb2D.AddForce(localRight);
        }

        currentMoveState = (isGrounded, isSprinting) switch
        {
            (true, true) => playerMoveState.onGroundSprinting,
            (true, false) => playerMoveState.onGround,
            (false, true) => playerMoveState.inAirSprinting,
            (false, false) => playerMoveState.inAir
        };

        // when you press F sprinting is set to true
        if (Input.GetKey(KeyCode.F))
       {
            isSprinting = true;
       }
       else
       {
            isSprinting = false;
       }

        // control jumping
        Vector2 localUp = transform.up;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb2D.AddForce(localUp * jumpForce, ForceMode2D.Impulse);
            isGrounded = false; // Set grounded to false after jumping
        }


        // control map rotation
        if (Input.GetKeyDown(KeyCode.Z)) // Check for Z key press
        {
            Rotate(leftRotationIncrement);
        }

        if (Input.GetKeyDown(KeyCode.X)) // Check for X key press
        {
            Rotate(rightRotationIncrement);
        }

        if (transform.rotation != targetRotation)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // always rotate towards the target rotation
        }

        // control map scale
        if (Input.GetKeyDown (KeyCode.Q) && scaleValue != 1 && stillScaling == false)
        {
            Scale(scaleDownIncrement);
        }

        if (Input.GetKeyDown(KeyCode.E) && scaleValue != 7 && stillScaling == false)
        {
            Scale(scaleUpIncrement);
        }
    }

    void UpdateGravityBasedOnRotation()
    {
        float angle = targetRotation.eulerAngles.z; // Get the angle of the target rotation

        if (Mathf.Approximately(angle, 0f)) // Set gravity direction based on the target rotation angle
        {
            Physics2D.gravity = new Vector2(0, -9.81f);  // Gravity down
            gravityDirection = "down";
        }
        else if (Mathf.Approximately(angle, 90f))
        {
            Physics2D.gravity = new Vector2(9.81f, 0);   // Gravity right
            gravityDirection = "right";
        }
        else if (Mathf.Approximately(angle, 180f))
        {
            Physics2D.gravity = new Vector2(0, 9.81f);   // Gravity up
            gravityDirection = "up";
        }
        else if (Mathf.Approximately(angle, 270f))
        {
            Physics2D.gravity = new Vector2(-9.81f, 0);  // Gravity left
            gravityDirection = "left";
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the player is colliding with the ground
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true; // Reset grounded status when touching the ground
        }
    }

    public void PlayerDied() // method to call whenever the player dies
    {
        transform.position = spawnPoint.position; // teleport player to spawn
    }

    /// <summary>
    /// Rotates the player
    /// </summary>
    /// <param name="angle"></param>
    public void Rotate(float angle) // Method to rotate 90 degrees
    {
        rotationToAdd = Quaternion.Euler(0, 0, angle);
        targetRotation = targetRotation * rotationToAdd;
        UpdateGravityBasedOnRotation();
    }

    /// <summary>
    /// scales the player
    /// </summary>
    /// <param name="scaleInteger"></param>
    public void Scale(float scaleInteger)
    {
        currentScale = new Vector2(transform.localScale.x, transform.localScale.y); // set current scale
        targetScale = currentScale * scaleInteger;                                  // set target scale
        currentZoom = virtualCamera.m_Lens.OrthographicSize;                        // set current zoom
        targetZoom = virtualCamera.m_Lens.OrthographicSize * scaleInteger;          // set target zoom
        StartCoroutine(ScaleAndZoomOverTime());
        rb2D.velocity = rb2D.velocity * scaleInteger;
        speedLimit = speedLimit * scaleInteger;
        if (scaleInteger == 2) //change scale value based on scale up or down
        {
            scaleValue++; 
        }
        else
        {
            scaleValue--;
        }
    }
    
    private IEnumerator ScaleAndZoomOverTime()  // use coroutine so you can use a while loop to scale over time
    {
        float elapsedTime = 0f; // set up for the while loop duration
        stillScaling = true; // set to true so that pressing the button while scaling is still happenning does not cause a bug where the player can have incorrect sizes

        while (elapsedTime < scaleSpeed) // set a while loop so that it scales over multiple frames
        {
            float t = elapsedTime / scaleSpeed; // set the time part of the lerp so that it goes from 0 to 1 in the same amount of time as the while loop goes 
            transform.localScale = Vector2.Lerp(currentScale, targetScale, t); // lerp the player scale
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(currentZoom, targetZoom, t); // lerp the camera zoom
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // set the final values to the target values to avoid rounding errors
        transform.localScale = targetScale;
        virtualCamera.m_Lens.OrthographicSize = targetZoom;
        stillScaling = false; // set still scaling to false so that the player can scale again
        if (targetZoom > currentZoom)
        {
            addScaleLaunchForce();
        }
    }

    private void addScaleLaunchForce()
    {
        Debug.Log("Entered the addScaleLaunchForce");
        for (int i = 0; i < scaleLaunchColliders.Length; i++)
        {
            Debug.Log("loop");
            Collider2D sLCol = scaleLaunchColliders[i];
            scaleLaunchVectors[i] = (rb2D.transform.position - scaleLaunchColliders[i].transform.position).normalized;

            if (sLCol.IsTouchingLayers(LayerMask.GetMask("Ground")))
            {
                isScaleLaunchVectorActive[i] = true;
                Debug.Log("If was true for" + sLCol.name);
            }
            else
            {
                isScaleLaunchVectorActive[i] = false;
                Debug.Log("If was false for" + sLCol.name);
            }
        }
        numberOfActiveScaleLaunchVectors = isScaleLaunchVectorActive.Count(b => b);
        if (numberOfActiveScaleLaunchVectors > 2)
        {
            for (int i = 0; i < isScaleLaunchVectorActive.Length; i++)
            {
                if (isScaleLaunchVectorActive[i])
                {
                    rb2D.AddForce(scaleLaunchVectors[i] * launchForce, ForceMode2D.Impulse);
                    Debug.Log("Force is" + scaleLaunchVectors[i] * launchForce);
                }
            }
        }
    }
}
