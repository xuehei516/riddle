using System;
using UnityEngine;
using UnityEngine.InputSystem;

#region 需要的组件
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(HealthEvent))]
[RequireComponent(typeof(Player_Health))]
[RequireComponent(typeof(PlayerAnimationEvent))]
[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(DestroyedEvent))]
[RequireComponent(typeof(Destroyed))]
#endregion

public class Player : MonoBehaviour
{
	#region 组件引用
	[HideInInspector] public PlayerController playerController;
	[HideInInspector] public Player_Health playerHealth;
	[HideInInspector] public PlayerAnimationController playerAnimationController;
	[HideInInspector] public Destroyed destroyed;
	#endregion

	#region 要用的事件
	[HideInInspector] public DestroyedEvent destroyedEvent;
	[HideInInspector] public PlayerAnimationEvent playerAnimationEvent;
	#endregion

	/// <summary>
	/// 玩家是否在光照区域内
	/// </summary>
	[HideInInspector] public bool isInLightZone;
	/// <summary>
	/// 当前玩家所在的光源区域
	/// </summary>
	[HideInInspector] public LightBeamArea currentLightSource;

	private void Awake()
	{
		playerController = GetComponent<PlayerController>();
		playerHealth = GetComponent<Player_Health>();
		playerAnimationController = GetComponent<PlayerAnimationController>();
		destroyed = GetComponent<Destroyed>();

		playerAnimationEvent = GetComponent<PlayerAnimationEvent>();
		destroyedEvent = GetComponent<DestroyedEvent>();
	}

}
