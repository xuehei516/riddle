using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	#region 玩家操作设置
	[Header("玩家操作设置")]

	[Tooltip("移动速度")]
	[SerializeField] private float speed = 5f;
	[Tooltip("跳跃受力")]
	[SerializeField] private float jumpForce = 10f;
	[Tooltip("两次跳跃之间的最小间隔")]
	[SerializeField] private float jumpInterval = 0.2f;

	[Header("操作手感优化")]
	[Tooltip("土狼时间")]
	[SerializeField] private float coyoteTime = 0.12f;
	[Tooltip("跳跃预输入时间")]
	[SerializeField] private float jumpBufferTime = 0.12f;

	[Header("地面检测")]
	[Tooltip("地面检测点，放在角色脚底")]
	[SerializeField] private Transform groundCheck;
	[Tooltip("地面检测范围")]
	[SerializeField] private float groundCheckRadius = 0.1f;
	[Tooltip("哪些层属于地面")]
	[SerializeField] private LayerMask groundLayer;
	#endregion

	#region 计时器
	/// <summary>
	/// 跳跃计时器
	/// </summary>
	private float jumpTimer = 0f;
	/// <summary>
	/// 土狼时间计时器
	/// </summary>
	private float coyoteTimer = 0f;
	/// <summary>
	/// 跳跃预输入计时器
	/// </summary>
	private float jumpBufferTimer = 0f;
	#endregion


	private Rigidbody2D rigidBody2D;
	private float moveInput;
	private bool isGrounded;

	private void Awake()
	{
		rigidBody2D = GetComponent<Rigidbody2D>();
	}

	private void Update()
	{
		isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

		UpdateTimer();
	}

	private void FixedUpdate()
	{
		rigidBody2D.velocity = new Vector2(moveInput * speed, rigidBody2D.velocity.y);

		if (jumpBufferTimer > 0f && coyoteTimer > 0f && jumpTimer <= 0f)
		{
			rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, jumpForce);
			jumpBufferTimer = 0f;
			coyoteTimer = 0f;
			jumpTimer = jumpInterval;
		}
	}

	public void OnMove(InputAction.CallbackContext ctx)
	{
		moveInput = ctx.ReadValue<float>();
	}

	public void OnJump(InputAction.CallbackContext ctx)
	{
		if (ctx.started)
		{
			jumpBufferTimer = jumpBufferTime;
		}

		if (ctx.canceled)
		{
			rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, rigidBody2D.velocity.y * 0.5f);
		}
	}

	private void UpdateTimer()
	{
		if (jumpTimer > 0)
		{
			jumpTimer -= Time.deltaTime;
		}

		if (isGrounded)
		{
			coyoteTimer = coyoteTime;
		}
		else
		{
			coyoteTimer -= Time.deltaTime;
		}

		if (jumpBufferTimer > 0)
		{
			jumpBufferTimer -= Time.deltaTime;
		}
	}
}
