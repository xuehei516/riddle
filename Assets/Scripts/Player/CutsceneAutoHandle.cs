using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneAutoHandle : MonoBehaviour
{
	[Header("自动行走设置")]
	[Tooltip("行走速度")]
	[SerializeField] private float walkSpeed = 4f;

	[Tooltip("未设置目标点时的自动行走时长")]
	[SerializeField] private float autoWalkDuration = 3f;

	[Tooltip("过场行走目标点")]
	[SerializeField] private Transform targetPoint;

	[Tooltip("到井边跳跃的目标点")]
	[SerializeField] private Transform jumpTargetPoint;

	[Tooltip("到达跳跃目标点后的向上起跳速度，设为 0 就是直接走出边缘下落")]
	[SerializeField] private float jumpUpSpeed = 2f;

	[Tooltip("起跳阶段的水平速度")]
	[SerializeField] private float jumpForwardSpeed = 4f;

	[Tooltip("到达井口后保持静止的时间")]
	[SerializeField] private float jumpFallDuration = 2f;

	[Header("跳跃 QTE")]
	[Tooltip("到达 jumpTargetPoint 后是否等待玩家按空格")]
	[SerializeField] private bool requireJumpInput = true;

	[Tooltip("等待空格时显示的提示物体")]
	[SerializeField] private GameObject jumpPrompt;

	private float stoppingDistance = 0.05f;

	private Rigidbody2D rb;
	private PlayerInput playerInput;
	private PlayerController playerController;
	private Player player;
	private bool waitingForJumpInput;

	public bool isAutoHandling = false;
	public bool IsWaitingForJumpInput => waitingForJumpInput;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		playerInput = GetComponent<PlayerInput>();
		playerController = GetComponent<PlayerController>();

		player = GetComponent<Player>();
		SetJumpPromptVisible(false);
	}

	private void Start()
	{
		StartAutoWalk();
	}

	public void StartAutoWalk()
	{
		StartCoroutine(AutoHandleRoutine());
	}

	/// <summary>
	/// 过场动画协程
	/// </summary>
	/// <returns></returns>
	private IEnumerator AutoHandleRoutine()
	{
		isAutoHandling = true;

		if (CutsceneBars.Instance != null)
		{
			CutsceneBars.Instance.Show();
		}

		// 禁用玩家手动按键输入
		if (playerInput != null)
			playerInput.DeactivateInput();

		float timer = 0f;
		while (ShouldKeepWalking(timer))
		{
			timer += Time.deltaTime;
			float direction = 1f;
			float currentSpeed = walkSpeed;
			if (targetPoint != null)
			{
				float deltaX = targetPoint.position.x - transform.position.x;
				if (Mathf.Abs(deltaX) <= stoppingDistance)
					break;

				direction = Mathf.Sign(deltaX);
				// 最后一小段减速，避免高速移动越过目标点后反复来回。
				float maxDistanceThisStep = walkSpeed * Time.fixedDeltaTime;
				if (Mathf.Abs(deltaX) < maxDistanceThisStep)
					currentSpeed = Mathf.Abs(deltaX) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
			}

			// 交给 PlayerController 写入物理速度，避免 FixedUpdate 把速度覆盖回 0
			if (playerController != null)
				playerController.BeginCutsceneMovement(direction * currentSpeed);
			else
				rb.velocity = new Vector2(direction * currentSpeed, rb.velocity.y);

			yield return null;
		}

		#region 到达目标点后，做一个小动作，表示到达
		if (playerController != null)
			playerController.EndCutsceneMovement();
		else
			rb.velocity = new Vector2(0, rb.velocity.y);
		yield return new WaitForSeconds(1f);

		if (playerController != null)
			playerController.SetFacingDirection(true);
		else if (player != null && player.spriteRenderer != null)
			player.spriteRenderer.flipX = true;

		yield return new WaitForSeconds(2f);

		if (playerController != null)
			playerController.SetFacingDirection(false);
		else if (player != null && player.spriteRenderer != null)
			player.spriteRenderer.flipX = false;

		yield return new WaitForSeconds(1f);
		#endregion

		// 转身动作结束后，继续走到跳跃目标点。
		if (jumpTargetPoint != null)
		{
			yield return MoveToPoint(jumpTargetPoint);

			// 到达边缘后先停住，等待玩家确认。原来的自动跳跃逻辑保留在确认之后。
			if (requireJumpInput)
				yield return WaitForJumpInput();

			// 确认后向前起跳，沿用原有的物理跳跃和下落动画。
			float launchSpeed = CalculateJumpSpeed(jumpTargetPoint);
			// 起跳方向以角色当前朝向为准：flipX=true 表示向左，false 表示向右。
			float jumpDirection = player != null && player.spriteRenderer != null && player.spriteRenderer.flipX
				? -1f
				: 1f;
			if (Mathf.Abs(jumpForwardSpeed) < 0.01f)
				Debug.LogWarning("过场跳跃的 Jump Forward Speed 为 0，角色不会产生水平位移。");
			if (playerController != null)
				playerController.BeginCutsceneJump(jumpDirection * jumpForwardSpeed, launchSpeed);
			else
				rb.velocity = new Vector2(jumpDirection * jumpForwardSpeed, launchSpeed);

			// 等待真实物理运动到达最高点，再把水平和垂直速度锁为 0
			yield return WaitForJumpDescent();

			yield return new WaitForSeconds(jumpFallDuration);
		}


		// 结束动画，把控制权还给玩家
		if (playerController != null)
			playerController.EndCutsceneMovement();
		else
			rb.velocity = new Vector2(0, rb.velocity.y);

		isAutoHandling = false;
		if (playerInput != null)
			playerInput.ActivateInput();

		GameTimer.Instance.StartTimer();
		if (CutsceneBars.Instance != null)
		{
			CutsceneBars.Instance.Hide();
		}
	}

	private IEnumerator WaitForJumpInput()
	{
		waitingForJumpInput = true;
		SetJumpPromptVisible(true);

		// 确保角色停在边缘，不让 PlayerController 的固定更新继续推动角色。
		if (playerController != null)
			playerController.SetCutsceneHorizontalSpeed(0f);
		else if (rb != null)
			rb.velocity = new Vector2(0f, rb.velocity.y);

		// 提示出现前按住的空格不算确认，需要松开后重新按下。
		yield return null;
		while (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
			yield return null;

		while (!WasJumpConfirmPressed())
			yield return null;

		waitingForJumpInput = false;
		SetJumpPromptVisible(false);
	}

	private bool WasJumpConfirmPressed()
	{
		// 项目已经使用 Input System；这里直接读取空格，避免等待期间重新启用整套移动输入。
		return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
	}

	private void SetJumpPromptVisible(bool visible)
	{
		if (jumpPrompt != null)
			jumpPrompt.SetActive(visible);
	}

	/// <summary>
	/// 判断是否继续行走
	/// </summary>
	/// <param name="timer"></param>
	/// <returns></returns>
	private bool ShouldKeepWalking(float timer)
	{
		if (targetPoint != null)
			return Mathf.Abs(targetPoint.position.x - transform.position.x) > stoppingDistance;

		return timer < autoWalkDuration;
	}

	private IEnumerator MoveToPoint(Transform destination)
	{
		while (destination != null && Mathf.Abs(destination.position.x - transform.position.x) > stoppingDistance)
		{
			float deltaX = destination.position.x - transform.position.x;
			float direction = Mathf.Sign(deltaX);
			float currentSpeed = walkSpeed;

			if (playerController != null)
				playerController.SetFacingDirection(direction < 0f);
			else if (player != null && player.spriteRenderer != null)
				player.spriteRenderer.flipX = direction < 0f;
			float maxDistanceThisStep = walkSpeed * Time.fixedDeltaTime;
			if (Mathf.Abs(deltaX) < maxDistanceThisStep)
				currentSpeed = Mathf.Abs(deltaX) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);

			if (playerController != null)
				playerController.BeginCutsceneMovement(direction * currentSpeed);
			else
				rb.velocity = new Vector2(direction * currentSpeed, rb.velocity.y);

			yield return null;
		}
	}

	private IEnumerator WaitForJumpDescent()
	{
		// 等一帧物理更新，确保起跳速度已经写入 Rigidbody2D。
		yield return new WaitForFixedUpdate();

		// 只根据竖直速度判断最高点，避免目标点与起跳点同高时立即清零水平速度。
		while (rb.velocity.y > 0f)
			yield return new WaitForFixedUpdate();

		if (playerController != null)
			playerController.SetCutsceneHorizontalSpeed(0f);
		else
			rb.velocity = new Vector2(0f, rb.velocity.y);
	}

	private float CalculateJumpSpeed(Transform destination)
	{
		float height = Mathf.Max(0f, destination.position.y - transform.position.y);
		float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
		if (height <= 0f || gravity <= 0f)
			return Mathf.Abs(jumpUpSpeed);

		// v² = 2gh，保证至少能到达井口；Inspector 值可用于额外提高跳跃高度。
		float minimumSpeed = Mathf.Sqrt(2f * gravity * height);
		return Mathf.Max(Mathf.Abs(jumpUpSpeed), minimumSpeed);
	}
}
