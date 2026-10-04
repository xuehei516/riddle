using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Health : Entity_Health
{
    public System.Action OnPlayerDeath;

    PlayerInput playerInput;

    private void OnEnable()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    protected override void Die()
    {
        base.Die();
        
        //ÃÌº”ÕÊº“À¿Õˆ¬ﬂº≠
        Debug.Log($"{gameObject.name} has died.");
        OnPlayerDeath?.Invoke();

        playerInput.enabled = false; 
    }
}
