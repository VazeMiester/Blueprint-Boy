using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathCondition : MonoBehaviour
{
    public PlayerController playercontroller; //reference playercontroller script

    private void OnTriggerEnter2D(Collider2D player) // when something enters the area
    {
        if (player.CompareTag("Player")) // Check if it is the player
        {
            playercontroller.PlayerDied(); // Call the kill function in the player controller
        }
    }
}
