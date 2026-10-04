using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [SerializeField]protected int damageAmount = 10;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player_Health playerHealth = collision.GetComponent<Player_Health>();
        if (playerHealth != null)
        {
            Debug.Log("Player health component found. Dealing damage: " + damageAmount);
            playerHealth.TakeDamage(damageAmount);
        }
    }
}
