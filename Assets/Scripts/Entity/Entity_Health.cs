using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_Health : MonoBehaviour
{
    private float defaultHP = 100;
    [SerializeField] public bool isDead;

    [SerializeField] protected float currentHP;

    private void OnEnable()
    {
        currentHP = defaultHP;
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        ReduceHP(damage);
    }

    protected void ReduceHP(float damage)
    {
        Debug.Log("" + gameObject.name + " took " + damage + " damage.");

        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        //Debug.Log($"{gameObject.name} has died.");
    }
}
