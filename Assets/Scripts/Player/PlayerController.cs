using System;
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

	#region 幽灵回放
	[Header("幽灵回放")]
	[Tooltip("是否为影子（回放模式），运行时由 GhostReplaySystem 自动设置")]
	[SerializeField] private bool isGhost = false;

	/// <summary>录到的输入帧（仅影子使用）</summary>
	private List<InputFrame> replayFrames;
	/// <summary>影子当前播到第几帧</summary>
	private int replayIndex;
	/// <summary>是否处于回放模式：忽略真实输入，改用录制数据驱动</summary>
	private bool replayMode;
	/// <summary>本物理帧内"按下跳跃"的意图（用于录制）</summary>
	private bool jumpDownThisFrame;
	/// <summary>本物理帧内"松开跳跃"的意图（用于录制）</summary>
	private bool jumpUpThisFrame;
	private bool interactDownThisFrame;

	[Header("机关交互")]
	[Tooltip("按 E 时搜索附近拉杆的半径")]
	[SerializeField, Min(0f)] private float interactRadius = 1.2f;

	/// <summary>影子是否已经把录制数据播完</summary>
	public bool ReplayFinished { get; private set; }
	/// <summary>当前对象是不是影子</summary>
	public bool IsGhost => isGhost;

	/// <summary>把这份录制数据交给本对象回放（由 GhostReplaySystem 在生成影子时调用）</summary>
	public void SetReplay(List<InputFrame> frames)
	{
		isGhost = true;
		replayMode = true;
		replayFrames = frames;
		replayIndex = 0;
		ReplayFinished = false;

		// 清空一切残留状态，保证影子和录制那一刻从同一个初始状态出发
		moveInput = 0f;
		jumpDownThisFrame = false;
		jumpUpThisFrame = false;
		interactDownThisFrame = false;
		jumpBufferTimer = 0f;
		coyoteTimer = 0f;
		jumpTimer = 0f;

		if (rigidBody2D != null)
		{
			rigidBody2D.velocity = Vector2.zero;
		}
	}
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

	private bool isGrounded;

	private Rigidbody2D rigidBody2D;
	private float moveInput;
	private bool facingLeft;

	private float platformVelocityX;

	private MovingPlatform currentPlatform;

	private Player player;

	private void Awake()
	{
		rigidBody2D = GetComponent<Rigidbody2D>();
		player = GetComponent<Player>();
	}

	private void Update()
	{
		isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

		UpdateTimer();

		UpdateFacingDirection();
		PublishAnimationState();

		if (interactDownThisFrame)
			TryPullNearbyLever();
	}

	private void FixedUpdate()
	{
		if (replayMode)
		{
			ApplyReplayFrame();
		}

		// 如果踩在移动平台上，水平速度要叠加平台的速度
		rigidBody2D.velocity = new Vector2(moveInput * speed + platformVelocityX, rigidBody2D.velocity.y);

		// 如果在土狼时间内按下了跳跃键，并且跳跃间隔已过，则执行跳跃
		if (jumpBufferTimer > 0f && coyoteTimer > 0f && jumpTimer <= 0f)
		{
			// 如果当前踩在可崩塌平台上，通知平台玩家跳跃了
			if (currentPlatform is CrumblePlatform crumblePlatform)
			{
				crumblePlatform.NotifyPlayerJump();
			}

			rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, jumpForce);

			jumpBufferTimer = 0f;
			coyoteTimer = 0f;
			jumpTimer = jumpInterval;
		}

		if (GhostReplayData.IsRecording && !replayMode)
		{
			GhostReplayData.Frames.Add(new InputFrame
			{
				move = moveInput,
				jumpDown = jumpDownThisFrame,
				jumpUp = jumpUpThisFrame,
				interactDown = interactDownThisFrame
			});
		}

		jumpDownThisFrame = false;
		jumpUpThisFrame = false;
		interactDownThisFrame = false;
	}

	/// <summary>从录制数据里取出当前帧输入并施加（影子专用）</summary>
	private void ApplyReplayFrame()
	{
		if (replayFrames == null || replayIndex >= replayFrames.Count)
		{
			moveInput = 0f;
			interactDownThisFrame = false;
			ReplayFinished = true;
			return;
		}

		InputFrame frame = replayFrames[replayIndex];
		replayIndex++;

		moveInput = frame.move;
		interactDownThisFrame = frame.interactDown;

		if (frame.jumpDown) jumpBufferTimer = jumpBufferTime;
		if (frame.jumpUp) rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, rigidBody2D.velocity.y * 0.5f);

		ReplayFinished = replayIndex >= replayFrames.Count;
	}

	/// <summary>
	/// 与附近的拉杆交互（按 E 时触发）
	/// </summary>
	private void TryPullNearbyLever()
	{
		Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, interactRadius);
		Lever closest = null;
		float closestDistance = float.PositiveInfinity;

		foreach (Collider2D candidate in nearby)
		{
			Lever lever = candidate.GetComponentInParent<Lever>();
			if (lever == null || !lever.isActiveAndEnabled) 
				continue;
			float distance = (lever.transform.position - transform.position).sqrMagnitude;
			if (distance >= closestDistance) continue;
			closest = lever;
			closestDistance = distance;
		}
		if (closest != null) 
			closest.TryPull();
	}

	/// <summary>
	/// 更新角色朝向（根据水平输入判断朝左还是朝右）
	/// </summary>
	private void UpdateFacingDirection()
	{
		if (moveInput > 0.01f)
		{
			facingLeft = false;
		}
		else if (moveInput < -0.01f)
		{
			facingLeft = true;
		}
	}

	/// <summary>
	/// 发布动画状态变化事件，通知 PlayerAnimationController 更新动画参数
	/// </summary>
	private void PublishAnimationState()
	{
		float horizontalSpeed = Mathf.Abs(rigidBody2D.velocity.x - platformVelocityX);

		float verticalSpeed = rigidBody2D.velocity.y;

		// 落地后忽略物理碰撞产生的微小抖动
		if (isGrounded || Mathf.Abs(verticalSpeed) < 0.05f)
		{
			verticalSpeed = 0f;
		}

		player.playerAnimationEvent.CallAnimationStateChanged(horizontalSpeed, verticalSpeed, isGrounded, facingLeft);
	}

	public void OnMove(InputAction.CallbackContext ctx)
	{
		if (replayMode) return; // 影子不接受输入
		moveInput = ctx.ReadValue<float>();
	}

	public void OnJump(InputAction.CallbackContext ctx)
	{
		if (replayMode) return; // 影子不接受输入

		if (ctx.started)
		{
			jumpBufferTimer = jumpBufferTime;
			jumpDownThisFrame = true;
		}

		if (ctx.canceled)
		{
			rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, rigidBody2D.velocity.y * 0.5f);
			jumpUpThisFrame = true;
		}
	}

	public void OnInteract(InputAction.CallbackContext ctx)
	{
		if (replayMode) 
			return; // 影子不接受输入

		if (ctx.started)
		{
			interactDownThisFrame = true;
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

	private void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.gameObject.TryGetComponent<MovingPlatform>(out var platform))
		{
			// 判断接触面朝上（确保角色是踩在平台顶部，而不是顶到底部或侧面）
			if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
			{
				currentPlatform = platform;
				platformVelocityX = platform.VelocityX;
				return;
			}
		}
		platformVelocityX = 0f;
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.gameObject.GetComponent<MovingPlatform>() == currentPlatform)
		{
			currentPlatform = null;
			platformVelocityX = 0f;
		}
	}
}
