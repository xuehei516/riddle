using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_Health : MonoBehaviour
{
    [SerializeField] private float defaultHP = 100;
    [SerializeField] public bool isDead;

    [SerializeField] protected float currentHP;

    /// <summary>当前血量</summary>
    public float CurrentHP => currentHP;
    /// <summary>血量上限</summary>
    public float MaxHP => defaultHP;

    private void OnEnable()
    {
        currentHP = defaultHP;
    }

    /// <summary>
    /// 初始化血量。写成 protected 是有原因的：
    /// 子类（如 Player_Health）只写了自己的 OnEnable 时，基类这个私有 OnEnable 不会被 Unity 调用，
    /// 于是 currentHP 一直是 0，被打一下就死。放在 Awake 里子类没有覆盖就能吃到。
    /// </summary>
    protected virtual void Awake()
    {
        currentHP = defaultHP;
    }

    /// <summary>回满血并清除死亡状态（复活点复活时调用）</summary>
    public virtual void Revive()
    {
        isDead = false;
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
