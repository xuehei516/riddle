using System;
using UnityEngine;

public class DestroyedEvent : MonoBehaviour
{
	/// <summary>
	/// 销毁事件，在对象被销毁时调用
	/// </summary>
	public event Action<DestroyedEvent, DestroyedEventArgs> OnPlayerDeath;

	public void CallPlayerDeathEvent(bool isInLightZone)
	{
		OnPlayerDeath?.Invoke(this, new DestroyedEventArgs()
		{
			isInLightZone = isInLightZone
		});

	}
}

public class DestroyedEventArgs : EventArgs
{
	public bool isInLightZone;
}


