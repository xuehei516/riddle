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
            Debug.Log($"成功获取玩家生命值组件. 造成伤害: {damageAmount}" );
            playerHealth.TakeDamage(damageAmount);
        }
    }
}
