using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public float speed = 5.0f;               // Horizontal movement speed
    public float jumpForce = 20.0f;          // Jump force
    public float rotationSpeed = 1000f;      // Rotation speed
    private Rigidbody2D rb2D;                // Reference to the Rigidbody2D component
    private bool isGrounded = true;          // Check if the player is grounded
    public Transform spawnPoint;             // Reference spawn location
    private Quaternion targetRotation;       // The target roation for the player
    private Quaternion rotationToAdd;        // The rotation that needs to be added to the target angle when the player rotates
    public string gravityDirection = "down"; // The current direction of gravity
    public float speedLimit = 5f;            // The velocity of the player where force stops being added when the movement key is pressed
    public float currentScale = 1;           // A value representing the current scale of the game
    public CinemachineVirtualCamera virtualCamera;

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();

        targetRotation = transform.rotation; // set intial target rotation to the current rotation
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

        if ((horizontalLocalVelocity <= speedLimit && moveHorizontal == 1) || (horizontalLocalVelocity >= -speedLimit && moveHorizontal == -1))  // only add force if the player is going less than the speedlimit
        {
            rb2D.AddForce(localRight);
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
            RotateLeft();
        }

        if (Input.GetKeyDown(KeyCode.X)) // Check for X key press
        {
            RotateRight();
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // always rotate towards the target rotation

        UpdateGravityBasedOnRotation();

        // control map scale
        if (Input.GetKeyDown (KeyCode.Q) && currentScale != 1)
        {
            ScaleDown();
        }

        if (Input.GetKeyDown(KeyCode.E) && currentScale != 7)
        {
            ScaleUp();
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

    public void RotateLeft() // Method to rotate 90 degrees left
    {
        rotationToAdd = Quaternion.Euler(0, 0, -90);
        targetRotation = targetRotation * rotationToAdd;
    }

    public void RotateRight() // Method to rotate 90 degrees right
    {
        rotationToAdd = Quaternion.Euler(0, 0, 90);
        targetRotation = targetRotation * rotationToAdd;
    }

    public void ScaleDown()
    {
        transform.localScale = new Vector2(transform.localScale.x / 2, transform.localScale.y / 2);
        currentScale = currentScale - 1;
        virtualCamera.m_Lens.OrthographicSize = virtualCamera.m_Lens.OrthographicSize / 2;
    }

    public void ScaleUp()
    {
        transform.localScale = new Vector2(transform.localScale.x * 2, transform.localScale.y * 2);
        currentScale = currentScale + 1;
        virtualCamera.m_Lens.OrthographicSize = virtualCamera.m_Lens.OrthographicSize * 2;
    }
}
