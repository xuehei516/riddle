using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

#region 需要的组件
[RequireComponent(typeof(SpriteRenderer))]
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
	[HideInInspector] public SpriteRenderer spriteRenderer;
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
		#region 获取组件引用
		playerController = GetComponent<PlayerController>();
		playerHealth = GetComponent<Player_Health>();
		playerAnimationController = GetComponent<PlayerAnimationController>();
		destroyed = GetComponent<Destroyed>();
		spriteRenderer = GetComponent<SpriteRenderer>();

		playerAnimationEvent = GetComponent<PlayerAnimationEvent>();
		destroyedEvent = GetComponent<DestroyedEvent>();
		#endregion

		DontDestroyOnLoad(gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");

		if (spawnPoint != null)
		{
			transform.position = spawnPoint.transform.position;

			Rigidbody2D body = GetComponent<Rigidbody2D>();
			if (body != null)
			{
				body.position = spawnPoint.transform.position;
				body.velocity = Vector2.zero;
			}
		}

		PlayerInput input = GetComponent<PlayerInput>();
		if (input != null)
			input.ActivateInput();
	}

}
