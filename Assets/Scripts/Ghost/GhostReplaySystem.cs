using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

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
	[SerializeField] private float recordDuration = 8f;

	[Header("影子外观")]
	[Tooltip("影子颜色，默认半透明蓝")]
	[SerializeField] private Color ghostColor = new Color(0.6f, 0.8f, 1f, 0.5f);
	[Tooltip("渲染层级偏移，-1 表示压在玩家身后")]
	[SerializeField] private int sortingOffset = -1;
	[Tooltip("影子所在的 Layer。必须是 Ghost 层：Player 层与 Player 层在物理矩阵里是互不碰撞的，留在 Player 层影子永远碰不到玩家")]
	[SerializeField] private string ghostLayerName = "Ghost";

	[Header("影子行为")]
	[Tooltip("影子是否与玩家碰撞。开启后影子会真的挡住玩家（代价：互相推挤可能把回放轨迹挤歪）")]
	[SerializeField] private bool ghostCollidesWithPlayer = true;
	[Tooltip("回放结束后销毁影子（默认留在原地待机）")]
	[SerializeField] private bool destroyGhostAfterReplay = false;

	[Header("调试")]
	[Tooltip("在屏幕左上角显示当前状态与可用按键")]
	[SerializeField] private bool showHud = true;

	private State state = State.Idle;
	private float recordTimer;
	private float replayTimer;
	private GameObject ghost;
	private GUIStyle hudStyle;

	/// <summary>硬编码的进度条路径：角色 → Canvas → Slider（按需求写死，不开 Inspector 接口）</summary>
	private const string ProgressSliderPath = "Canvas/Slider";

	private Slider progressSlider;
	private bool warnedMissingSlider;

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

		UpdateSlider();
	}

	private void FixedUpdate()
	{
		// 回放中：按物理帧累加，与影子推进的节奏一致，滑动条才不会偏
		if (state == State.Replaying)
		{
			replayTimer += Time.fixedDeltaTime;
			return;
		}

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

		if (AudioManager.instance != null)
		{
			AudioManager.instance.Play("录制音效");
		}

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

		replayTimer = 0f; // 回放计时归零，滑动条从「本段录制时长」开始倒转
		SpawnGhost();

		if (ghost != null)
		{
			state = State.Replaying;

			if (AudioManager.instance != null)
			{
				AudioManager.instance.Play("重演音效");
			}
		}
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

		// 换到 Ghost 层。克隆来的层是 Player，而物理矩阵里 Player×Player 是关闭的——
		// 留在 Player 层的话影子永远不可能和玩家发生物理接触，下面的开关也就成了死开关。
		ApplyGhostLayer();

		// 影子是整份克隆玩家的，会把角色身上的 Canvas/Slider 也复制一份。
		// 而 Canvas 是 Screen Space-Overlay，克隆体会和真条完全重叠 ——
		// 看起来就是"原来的滑动头卡住不动，又冒出一个新滑动头往回走"。
		DisableCloneUI();

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

	/// <summary>
	/// 把影子整体换到 ghostLayerName 指定的层（含子物体）。
	/// 找不到该层时保持克隆来的 Player 层并警告——那样影子将无法与玩家发生物理接触。
	/// </summary>
	private void ApplyGhostLayer()
	{
		if (string.IsNullOrEmpty(ghostLayerName)) return;

		int layer = LayerMask.NameToLayer(ghostLayerName);
		if (layer < 0)
		{
			Debug.LogWarning($"[幽灵回放] 工程里没有名为「{ghostLayerName}」的 Layer，影子仍留在 Player 层。" +
			                 "Player×Player 在 2D 物理矩阵里是互不碰撞的，影子将碰不到玩家。");
			return;
		}

		SetLayerRecursively(ghost, layer);
	}

	/// <summary>把整棵子物体树都换到指定层</summary>
	private static void SetLayerRecursively(GameObject target, int layer)
	{
		target.layer = layer;

		foreach (Transform child in target.transform)
		{
			SetLayerRecursively(child.gameObject, layer);
		}
	}

	/// <summary>
	/// 关掉克隆体里的 UI（Canvas / Slider）。
	/// 只翻组件开关，不删对象、不碰任何 Transform，也不去控制滑动头——
	/// 影子从此不带进度条，屏幕上只保留玩家那一条。
	/// </summary>
	private void DisableCloneUI()
	{
		if (ghost == null) return;

		foreach (Canvas canvas in ghost.GetComponentsInChildren<Canvas>(true))
		{
			canvas.enabled = false;
		}

		foreach (Slider slider in ghost.GetComponentsInChildren<Slider>(true))
		{
			slider.enabled = false;
		}
	}

	private Transform FindPlayer()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		return player != null ? player.transform : null;
	}
	#endregion

	#region 滑动条（硬编码，无 Inspector 接口）
	/// <summary>
	/// 取角色身上那个叫 Slider 的进度条：路径写死为 Player/Canvas/Slider。
	/// 找不到时兜底扫一遍角色层级里名为 "Slider" 的对象；再找不到只警告一次。
	/// </summary>
	private Slider ResolveProgressSlider()
	{
		if (progressSlider != null) return progressSlider;

		Transform player = FindPlayer();
		if (player == null) return null;

		Transform target = player.Find(ProgressSliderPath);

		if (target == null)
		{
			foreach (Slider candidate in player.GetComponentsInChildren<Slider>(true))
			{
				if (candidate.name == "Slider")
				{
					target = candidate.transform;
					break;
				}
			}
		}

		if (target != null)
		{
			progressSlider = target.GetComponent<Slider>();
		}

		if (progressSlider == null && !warnedMissingSlider)
		{
			warnedMissingSlider = true;
			Debug.LogWarning($"[幽灵回放] 角色身上找不到 {ProgressSliderPath} 的 Slider，进度条不会更新");
		}

		return progressSlider;
	}

	/// <summary>
	/// 滑动条同步：
	/// 上限恒定 = 最大录制时间；
	/// 待机 = 0，录制中 = 已录制时间（往上涨），就绪 = 本段总时长，
	/// 回放中 = 从「本段总时长」倒着减到 0。
	/// </summary>
	private void UpdateSlider()
	{
		Slider slider = ResolveProgressSlider();
		if (slider == null) return;

		slider.maxValue = recordDuration; // 上限 = 最大录制时间

		float recordedLength = GhostReplayData.Frames.Count * Time.fixedDeltaTime;

		switch (state)
		{
			case State.Idle:
				slider.value = 0f;
				break;

			case State.Recording:
				slider.value = recordTimer; // 已经录制的时间
				break;

			case State.Ready:
				slider.value = recordedLength;
				break;

			case State.Replaying:
				slider.value = Mathf.Max(0f, recordedLength - replayTimer); // 倒转
				break;
		}
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
