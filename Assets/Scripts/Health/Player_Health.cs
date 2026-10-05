using System;
using UnityEngine;

[RequireComponent(typeof(HealthEvent))]
public class Player_Health : MonoBehaviour
{
	[SerializeField, Min(1)]
	[Tooltip("最大生命值")]
	private int maxHealth = 100;

	private Player player;

	public int CurrentHealth { get; private set; }
	public bool IsDead { get; private set; }

	private HealthEvent healthEvent;

	private void Awake()
	{
		player = GetComponent<Player>();
		healthEvent = GetComponent<HealthEvent>();
	}

	private void Start()
	{
		CurrentHealth = maxHealth;

		healthEvent.CallHealthChangedEvent(CurrentHealth, 0);
	}

	/// <summary>
	/// 受到伤害，减少生命值
	/// </summary>
	/// <param name="damageAmount"></param>
	public void TakeDamage(int damageAmount)
	{
		if (IsDead || damageAmount <= 0) 
			return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);

		healthEvent.CallHealthChangedEvent(CurrentHealth, damageAmount);

		// 当前生命值归 0 时，触发死亡事件
		if (CurrentHealth <= 0) 
		{
			IsDead = true;
			player.destroyedEvent.CallPlayerDeathEvent();
		}
	}

	/// <summary>
	/// 恢复生命值
	/// </summary>
	/// <param name="amount"></param>
	public void AddHealth(int amount)
	{
		if (IsDead || amount <= 0)
			return;

		CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
		healthEvent.CallHealthChangedEvent(CurrentHealth, 0);
	}

	/// <summary>
	/// 复活：清掉死亡标记并把血量回满（由复活点调用）
	/// </summary>
	public void Revive()
	{
		IsDead = false;
		CurrentHealth = maxHealth;

		healthEvent.CallHealthChangedEvent(CurrentHealth, 0);
	}


}