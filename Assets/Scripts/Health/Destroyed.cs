using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DestroyedEvent))]
[DisallowMultipleComponent]
public class Destroyed : MonoBehaviour
{
	private DestroyedEvent destroyedEvent;

	private Player player;

	private void Awake()
	{
		destroyedEvent = GetComponent<DestroyedEvent>();
		player = GetComponent<Player>();
	}

	private void OnEnable()
	{
		destroyedEvent.OnPlayerDeath += DestroyedEvent_OnDestroyed;
	}

	private void OnDisable()
	{
		destroyedEvent.OnPlayerDeath -= DestroyedEvent_OnDestroyed;
	}

	private void DestroyedEvent_OnDestroyed(DestroyedEvent destroyedEvent, DestroyedEventArgs destroyedEventArgs)
	{
		// 如果玩家在光照区域内死亡，则在玩家位置生成灵魂
		if (destroyedEventArgs.isInLightZone)
		{
			player.currentLightSource.SpawnSoulAtPosition(player.transform.position);
		}

		gameObject.SetActive(false);
	}
}
