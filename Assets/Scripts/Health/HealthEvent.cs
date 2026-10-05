using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HealthEvent : MonoBehaviour
{
	/// <summary>
	/// 血量已变化事件，在血量变化时调用
	/// </summary>
	public event Action<HealthEvent, HealthEventArgs> OnHealthChanged;

	/// <summary>
	/// 调用此方法来触发血量已变化事件，传入当前血量和伤害值
	/// </summary>
	/// <param name="healthPercent"></param>
	/// <param name="healthAmount"></param>
	/// <param name="damageAmount"></param>
	public void CallHealthChangedEvent(int healthAmount, int damageAmount)
	{
		OnHealthChanged?.Invoke(this, new HealthEventArgs()
		{
			healthAmount = healthAmount,
			damageAmount = damageAmount
		});
	}
}

public class HealthEventArgs : EventArgs
{
	/// <summary>
	/// 当前血量
	/// </summary>
	public int healthAmount;
	/// <summary>
	/// 伤害值
	/// </summary>
	public int damageAmount;
}
