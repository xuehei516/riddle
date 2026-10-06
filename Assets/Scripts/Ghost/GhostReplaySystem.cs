using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// 时间回溯（幽灵回放）主控制器。
///
/// 按键分工（F 管录制，R 管回放，互不重叠）：
///   F → 录制开关：按一下开始录制，再按一下停止录制
///   R → 回放最近一段录制（在当前场景就地生成影子重放，不重载场景）
///
/// 完整流程：
///   1) 待机按 F             → 记住玩家当前位置作为影子出生点，开始录制输入
///   2) 再按 F（或录满 X 秒）→ 录制结束，进入「就绪」
///   3) 按 R                → 在当前场景生成半透明影子，影子重放同一段输入
///
/// 注意：回放不重载场景，所以关卡状态（拉杆、平台、敌人、已吃的道具）保持当前值不变。
///
/// 使用方式：不需要手动摆对象——进游戏时脚本会自动创建一个 GhostManager。
/// 想改录制秒数、影子颜色、按键等参数，就在场景里手动建个空物体挂上本脚本（自动创建会跳过）。
/// </summary>
public class GhostReplaySystem : MonoBehaviour
{
	public enum State { Idle, Recording, Ready, Replaying }

	[Header("按键")]
	[Tooltip("录制开关：按一下开始录制，再按一下停止录制")]
	[SerializeField] private Key recordKey = Key.F;
	[Tooltip("回放：重放最近一段录制（不重载场景）")]
	[SerializeField] private Key replayKey = Key.R;

	[Header("录制设置")]
	[Tooltip("单次录制最长时长（秒），录满自动停止")]
	[SerializeField] private float recordDuration = 5f;

	[Header("影子外观")]
	[Tooltip("影子颜色，默认半透明蓝")]
	[SerializeField] private Color ghostColor = new Color(0.6f, 0.8f, 1f, 0.5f);
	[Tooltip("渲染层级偏移，-1 表示压在玩家身后")]
	[SerializeField] private int sortingOffset = -1;

	[Header("影子行为")]
	[Tooltip("影子是否与玩家碰撞（默认关闭，防止互相推挤导致轨迹漂移）")]
	[SerializeField] private bool ghostCollidesWithPlayer = false;
	[Tooltip("回放结束后销毁影子（默认留在原地待机）")]
	[SerializeField] private bool destroyGhostAfterReplay = false;

	[Header("调试")]
	[Tooltip("在屏幕左上角显示当前状态与可用按键")]
	[SerializeField] private bool showHud = true;

	private State state = State.Idle;
	private float recordTimer;
	private GameObject ghost;
	private GUIStyle hudStyle;

	/// <summary>当前状态，方便在 Inspector 或调试时查看</summary>
	public State CurrentState => state;

	/// <summary>当前场景里的影子（没有则为 null）</summary>
	public GameObject CurrentGhost => ghost;

	#region 生命周期
	private void Awake()
	{
		// 管理器跨场景常驻：换场景后 F/R 依然可用，录制数据也不会丢
		DontDestroyOnLoad(gameObject);
	}

	/// <summary>场景里没手动挂本脚本时，自动创建一个，保证零配置可用</summary>
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Bootstrap()
	{
		if (FindObjectOfType<GhostReplaySystem>() != null) return;
		new GameObject("GhostManager (auto)").AddComponent<GhostReplaySystem>();
	}
	#endregion

	#region 主循环
	private void Update()
	{
		Keyboard keyboard = Keyboard.current;

		bool recordPressed = WasPressedThisFrame(keyboard, recordKey);
		bool replayPressed = WasPressedThisFrame(keyboard, replayKey);

		switch (state)
		{
			case State.Idle:
				// F：开始录制；R：手上还有上一段数据时可以直接再放一次
				if (recordPressed) StartRecording();
				else if (replayPressed) StartReplay();
				break;

			case State.Recording:
				// F：停止录制。录制中按 R 不响应，避免半截数据被回放
				if (recordPressed) FinishRecording();
				else if (replayPressed) Debug.LogWarning($"[幽灵回放] 正在录制中，先按 {recordKey} 结束录制才能回放");
				break;

			case State.Ready:
				// F：覆盖旧数据重录；R：回放
				if (recordPressed) StartRecording();
				else if (replayPressed) StartReplay();
				break;

			case State.Replaying:
				// F：就地重录（不重载场景）；R：再放一次；播完自动回待机
				if (recordPressed) StartRecording();
				else if (replayPressed) StartReplay();
				else CheckReplayFinished();
				break;
		}
	}

	private void FixedUpdate()
	{
		if (state != State.Recording) return;

		recordTimer += Time.fixedDeltaTime;

		if (recordTimer >= recordDuration)
		{
			Debug.Log($"[幽灵回放] 已录满 {recordDuration} 秒，自动停止");
			FinishRecording();
		}
	}

	/// <summary>某个按键这一帧是否刚被按下（没接键盘时返回 false）</summary>
	private static bool WasPressedThisFrame(Keyboard keyboard, Key key)
	{
		if (keyboard == null) return false;

		KeyControl control = keyboard[key];
		return control != null && control.wasPressedThisFrame;
	}
	#endregion

	#region 录制流程
	/// <summary>F①：开始录制（会顺带清掉上一轮留下的影子）</summary>
	private void StartRecording()
	{
		Transform player = FindPlayer();
		if (player == null)
		{
			Debug.LogError("[幽灵回放] 场景里找不到 Tag 为 Player 的对象");
			return;
		}

		// 开始新一轮录制，先把上一轮的影子清掉，避免场景里越叠越多
		ClearGhost();

		GhostReplayData.Reset();
		GhostReplayData.IsRecording = true;
		GhostReplayData.SpawnPosition = player.position; // 影子出生点 = 按下 F 时的玩家位置

		recordTimer = 0f;
		state = State.Recording;

		Debug.Log($"[幽灵回放] 开始录制，最长 {recordDuration} 秒；再按 {recordKey} 结束");
	}

	/// <summary>F②：结束录制并锁定数据</summary>
	private void FinishRecording()
	{
		GhostReplayData.IsRecording = false;
		GhostReplayData.HasRecording = GhostReplayData.Frames.Count > 0;

		if (!GhostReplayData.HasRecording)
		{
			state = State.Idle;
			Debug.LogWarning($"[幽灵回放] 没录到任何帧（{recordKey} 按太快了），请重新录制");
			return;
		}

		state = State.Ready;
		float seconds = GhostReplayData.Frames.Count * Time.fixedDeltaTime;
		Debug.Log($"[幽灵回放] 录制完成：{GhostReplayData.Frames.Count} 帧（约 {seconds:F1} 秒）。按 {replayKey} 就地回放");
	}
	#endregion

	#region 回放流程
	/// <summary>R：在当前场景就地重放上一段录制（不重载场景）</summary>
	private void StartReplay()
	{
		if (!GhostReplayData.HasRecording || GhostReplayData.Frames.Count == 0)
		{
			Debug.LogWarning($"[幽灵回放] 还没有可回放的录制，先按 {recordKey} 录一段");
			return;
		}

		// 不重载场景 → 必须手动清掉上一轮的影子，否则每按一次 R 就多一个
		ClearGhost();

		SpawnGhost();

		if (ghost != null) state = State.Replaying;
	}

	/// <summary>影子把录制数据播完后，自动回到待机</summary>
	private void CheckReplayFinished()
	{
		if (ghost == null)
		{
			state = State.Idle;
			return;
		}

		PlayerController ghostController = ghost.GetComponent<PlayerController>();
		if (ghostController == null || !ghostController.ReplayFinished) return;

		if (destroyGhostAfterReplay)
		{
			ClearGhost();
		}

		state = State.Idle;
		Debug.Log("[幽灵回放] 回放结束，回到待机");
	}
	#endregion

	#region 生成影子
	private void SpawnGhost()
	{
		Transform player = FindPlayer();
		if (player == null)
		{
			Debug.LogError("[幽灵回放] 场景里找不到 Tag 为 Player 的对象，无法生成影子");
			return;
		}

		if (GhostReplayData.Frames.Count == 0)
		{
			Debug.LogWarning("[幽灵回放] 没有录制数据，无法生成影子");
			return;
		}

		// 直接克隆玩家对象：Rigidbody2D / 碰撞体 / groundCheck / 各项参数全都现成，不用重配
		ghost = Instantiate(player.gameObject, GhostReplayData.SpawnPosition, player.rotation);
		ghost.name = "Ghost";
		ghost.tag = "Untagged"; // 防止之后 FindWithTag("Player") 找到影子而不是玩家

		// 影子不读真实键盘输入
		PlayerInput playerInput = ghost.GetComponent<PlayerInput>();
		if (playerInput != null) playerInput.enabled = false;

		// 半透明 + 压到玩家身后
		SpriteRenderer sprite = ghost.GetComponent<SpriteRenderer>();
		if (sprite != null)
		{
			sprite.color = ghostColor;
			sprite.sortingOrder += sortingOffset;
		}

		// 影子与玩家互不碰撞，避免互相推挤
		if (!ghostCollidesWithPlayer)
		{
			Collider2D playerCollider = player.GetComponent<Collider2D>();
			Collider2D ghostCollider = ghost.GetComponent<Collider2D>();
			if (playerCollider != null && ghostCollider != null)
			{
				Physics2D.IgnoreCollision(playerCollider, ghostCollider, true);
			}
		}

		// 交给同一个控制器走"回放分支"：从静止、无输入开始，保证和录制时的起始状态一致。
		// 传一份拷贝：这样之后按 F 重录（会清空 Frames）不会让正在跑的影子突然卡住。
		PlayerController controller = ghost.GetComponent<PlayerController>();
		if (controller != null)
		{
			controller.SetReplay(new List<InputFrame>(GhostReplayData.Frames));
		}

		Debug.Log($"[幽灵回放] 影子已生成于 {GhostReplayData.SpawnPosition}，开始回放 {GhostReplayData.Frames.Count} 帧");
	}

	/// <summary>销毁当前影子并清空引用</summary>
	private void ClearGhost()
	{
		if (ghost != null) Destroy(ghost);
		ghost = null;
	}

	private Transform FindPlayer()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		return player != null ? player.transform : null;
	}
	#endregion

	#region 屏幕提示
	private void OnGUI()
	{
		if (!showHud) return;

		if (hudStyle == null)
		{
			hudStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 16,
				fontStyle = FontStyle.Bold
			};
			hudStyle.normal.textColor = Color.white;
		}

		GUI.Label(new Rect(16f, 16f, 640f, 26f), BuildHint(), hudStyle);
	}

	private string BuildHint()
	{
		switch (state)
		{
			case State.Idle:
				return $"[幽灵回放] 待机　按 {recordKey} 开始录制";
			case State.Recording:
				return $"[幽灵回放] 录制中 {recordTimer:F1}s / {recordDuration}s　按 {recordKey} 停止";
			case State.Ready:
				return $"[幽灵回放] 已就绪　按 {replayKey} 回放　按 {recordKey} 重录";
			case State.Replaying:
				return $"[幽灵回放] 回放中　按 {recordKey} 重录　按 {replayKey} 再放一次";
			default:
				return string.Empty;
		}
	}
	#endregion
}
