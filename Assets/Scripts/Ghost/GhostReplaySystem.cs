using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 时间回溯（幽灵回放）主控制器。
///
/// 流程：
///   1) 按 R      → 记住玩家当前位置作为影子出生点，开始录制输入
///   2) X 秒内自由操作（或提前再按 R 结束录制）
///   3) 按 R      → 重载当前场景
///   4) 重载完成后 → 在「按 R 时的位置」生成半透明影子，影子重放同一段输入
///
/// 使用方式：不需要手动摆对象——进游戏时脚本会自动创建一个 GhostManager。
/// 想改 X 秒、影子颜色等参数，就在场景里手动建个空物体挂上本脚本（自动创建会跳过）。
/// </summary>
public class GhostReplaySystem : MonoBehaviour
{
	public enum State { Idle, Recording, Ready, Replaying }

	[Header("录制设置")]
	[Tooltip("单次录制最长时长（秒）= 你的 X")]
	[SerializeField] private float recordDuration = 5f;
	[Tooltip("勾选后可在录满 X 秒前按 R 提前结束录制")]
	[SerializeField] private bool allowEarlyStop = true;

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

	private State state = State.Idle;
	private float recordTimer;
	private GameObject ghost;
	private bool isLoadingScene;

	/// <summary>当前状态，方便在 Inspector 或调试时查看</summary>
	public State CurrentState => state;

	#region 生命周期
	private void Awake()
	{
		// 管理器必须活过场景重载，否则重载后就没人去生成影子了
		DontDestroyOnLoad(gameObject);
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	/// <summary>场景里没手动挂本脚本时，自动创建一个，保证零配置可用</summary>
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Bootstrap()
	{
		if (FindObjectOfType<GhostReplaySystem>() != null) return;
		new GameObject("GhostManager (auto)").AddComponent<GhostReplaySystem>();
	}

	/// <summary>新场景加载完成：有待回放数据就在录制起点生成影子</summary>
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		isLoadingScene = false;

		if (!GhostReplayData.PendingReplay) return;

		GhostReplayData.PendingReplay = false;
		recordTimer = 0f;
		SpawnGhost();
		state = State.Replaying;
	}
	#endregion

	#region 主循环
	private void Update()
	{
		if (isLoadingScene) return;

		bool rPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

		switch (state)
		{
			case State.Idle:
				// R①：开始录制
				if (rPressed) StartRecording();
				break;

			case State.Recording:
				// 录满 X 秒由 FixedUpdate 自动收尾，这里负责"提前结束"
				if (rPressed && allowEarlyStop) FinishRecording();
				break;

			case State.Ready:
				// R②：重载场景 + 生成影子
				if (rPressed) ReloadSceneAndReplay();
				break;

			case State.Replaying:
				// 再按 R：清掉旧数据重新录一轮（影子要等下次重载才会被替换）
				if (rPressed) StartRecording();
				break;
		}

		if (state == State.Replaying && ghost != null && destroyGhostAfterReplay)
		{
			PlayerController ghostController = ghost.GetComponent<PlayerController>();
			if (ghostController != null && ghostController.ReplayFinished) Destroy(ghost);
		}
	}

	private void FixedUpdate()
	{
		if (state != State.Recording) return;

		recordTimer += Time.fixedDeltaTime;

		if (recordTimer >= recordDuration)
		{
			Debug.Log($"[幽灵回放] 已录满 {recordDuration} 秒");
			FinishRecording();
		}
	}
	#endregion

	#region 录制流程
	/// <summary>R①：开始录制</summary>
	private void StartRecording()
	{
		Transform player = FindPlayer();
		if (player == null)
		{
			Debug.LogError("[幽灵回放] 场景里找不到 Tag 为 Player 的对象");
			return;
		}

		GhostReplayData.Reset();
		GhostReplayData.IsRecording = true;
		GhostReplayData.SpawnPosition = player.position; // 影子出生点 = 按下 R 时的玩家位置

		recordTimer = 0f;
		state = State.Recording;

		Debug.Log($"[幽灵回放] 开始录制，最长 {recordDuration} 秒");
	}

	/// <summary>结束录制并锁定数据（此时还没重载场景）</summary>
	private void FinishRecording()
	{
		GhostReplayData.IsRecording = false;
		GhostReplayData.HasRecording = GhostReplayData.Frames.Count > 0;

		if (!GhostReplayData.HasRecording)
		{
			state = State.Idle;
			Debug.LogWarning("[幽灵回放] 没录到任何帧，请重新按 R 录制");
			return;
		}

		state = State.Ready;
		float seconds = GhostReplayData.Frames.Count * Time.fixedDeltaTime;
		Debug.Log($"[幽灵回放] 录制完成：{GhostReplayData.Frames.Count} 帧（约 {seconds:F1} 秒）。再按一次 R 重载场景并生成影子");
	}

	/// <summary>R②：重载当前场景，重载完成后生成影子</summary>
	private void ReloadSceneAndReplay()
	{
		if (!GhostReplayData.HasRecording)
		{
			state = State.Idle;
			return;
		}

		GhostReplayData.PendingReplay = true;
		isLoadingScene = true;

		Scene active = SceneManager.GetActiveScene();
		if (active.buildIndex >= 0)
		{
			SceneManager.LoadScene(active.buildIndex);
		}
		else
		{
			// 场景没加进 Build Settings 时兜底（按名字加载）
			SceneManager.LoadScene(active.name);
		}

		Debug.Log("[幽灵回放] 重载场景中…");
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
		// 传一份拷贝：这样之后你按 R 重录（会清空 Frames）不会让正在跑的影子突然卡住。
		PlayerController controller = ghost.GetComponent<PlayerController>();
		if (controller != null)
		{
			controller.SetReplay(new List<InputFrame>(GhostReplayData.Frames));
		}

		Debug.Log($"[幽灵回放] 影子已生成于 {GhostReplayData.SpawnPosition}，开始回放 {GhostReplayData.Frames.Count} 帧");
	}

	private Transform FindPlayer()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		return player != null ? player.transform : null;
	}
	#endregion
}
