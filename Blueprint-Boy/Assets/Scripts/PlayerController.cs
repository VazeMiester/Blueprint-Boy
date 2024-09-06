using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public float speed = 5.0f;       // Horizontal movement speed
    public float jumpForce = 10.0f;  // Jump force
    private Rigidbody2D rb2D;        // Reference to the Rigidbody2D component
    private bool isGrounded = true;  // Check if the player is grounded

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Handle horizontal movement manually
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        rb2D.velocity = new Vector2(moveHorizontal * speed, rb2D.velocity.y); // Maintain vertical velocity

        // Handle jumping using Rigidbody2D physics
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false; // Set grounded to false after jumping
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
}
