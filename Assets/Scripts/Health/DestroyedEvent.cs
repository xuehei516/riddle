using System;
using UnityEngine;

public class DestroyedEvent : MonoBehaviour
{
	/// <summary>
	/// 销毁事件，在对象被销毁时调用
	/// </summary>
	public event Action OnPlayerDeath;

	public void CallPlayerDeathEvent()
	{
		OnPlayerDeath?.Invoke();
	}
}
