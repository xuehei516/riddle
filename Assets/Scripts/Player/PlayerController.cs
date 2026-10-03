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
		// 影子：从录制数据里取输入（不读真实键盘）
		if (replayMode)
		{
			ApplyReplayFrame();
		}

		rigidBody2D.velocity = new Vector2(moveInput * speed, rigidBody2D.velocity.y);

		if (jumpBufferTimer > 0f && coyoteTimer > 0f && jumpTimer <= 0f)
		{
			rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, jumpForce);
			jumpBufferTimer = 0f;
			coyoteTimer = 0f;
			jumpTimer = jumpInterval;
		}

		// 录制：把"这一物理帧的输入"存下来（影子和录制都走同一套移动代码，轨迹才会一致）
		if (GhostReplayData.IsRecording && !replayMode)
		{
			GhostReplayData.Frames.Add(new InputFrame
			{
				move = moveInput,
				jumpDown = jumpDownThisFrame,
				jumpUp = jumpUpThisFrame
			});
		}

		jumpDownThisFrame = false;
		jumpUpThisFrame = false;
	}

	/// <summary>从录制数据里取出当前帧输入并施加（影子专用）</summary>
	private void ApplyReplayFrame()
	{
		if (replayFrames == null || replayIndex >= replayFrames.Count)
		{
			moveInput = 0f;
			ReplayFinished = true;
			return;
		}

		InputFrame frame = replayFrames[replayIndex];
		replayIndex++;

		moveInput = frame.move;

		if (frame.jumpDown) jumpBufferTimer = jumpBufferTime;
		if (frame.jumpUp) rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, rigidBody2D.velocity.y * 0.5f);

		ReplayFinished = replayIndex >= replayFrames.Count;
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
