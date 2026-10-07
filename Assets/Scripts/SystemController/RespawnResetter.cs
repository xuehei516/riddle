using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 玩家复活后，把目标对象（可多个）全部重设回**游戏刚开始时的启用状态**。
///
///   - 开局（Awake）记下每个目标的初始启用状态，之后每次复活都以这份为准
///   - 复活怎么判断：每帧看一眼玩家的 Player_Health.IsDead，
///     检测到「死 → 活」那一下就是刚复活，重置一次。
///     （也可以把某个复活点上的 onRespawned 拖到本脚本的 ResetToInitialState() 上，两种方式并存没问题）
///     例：某对象初始是启用的，玩家死的时候它是关着的 → 复活后会被重新启用
///
/// 用法：挂在一个开局就启用的物体上（**别挂在玩家对象下面**——玩家死亡时 Destroyed 会把它一起关掉），
/// 然后把要重置的对象拖进 targets。
///
/// 注意这里只重置「启用状态」（外加平台自己的粉碎 / 消失状态）。门的开关位置、平台走到哪儿了
/// 这类属于它们自己内部逻辑的状态不去碰：直接改 Transform 会让状态和视觉对不上，需要的话
/// 应该走它们自己的 SetSignal 信号接口。
/// </summary>
public class RespawnResetter : MonoBehaviour
{
	[Header("复活时要重置回初始状态的目标")]
	[Tooltip("要重置的对象；可以留空，也可以拖多个。初始启用状态在开局记录")]
	[SerializeField] private GameObject[] targets;

	[Tooltip("目标里如果挂过易碎 / 限时平台，顺手把「已粉碎 / 已消失」的内部状态也复位")]
	[SerializeField] private bool resetPlatforms = true;

	[Header("音效 / 特效接口（可选）")]
	[Tooltip("重置完成后触发")]
	[SerializeField] private UnityEvent onReset;

	/// <summary>目标的初始启用状态</summary>
	private struct TargetState
	{
		public GameObject target;
		public bool initialActive;
	}

	private readonly List<TargetState> initialStates = new List<TargetState>();

	/// <summary>盯着的玩家</summary>
	private Player_Health player;
	/// <summary>上一帧玩家是不是死的（用来抓「死 → 活」那一下）</summary>
	private bool wasDead;

	private void Awake()
	{
		CaptureInitialStates();
		ResolvePlayer();
	}

	/// <summary>
	/// 每帧检测「死 → 活」的翻转：翻转了就说明刚复活，重置一次
	/// </summary>
	private void Update()
	{
		ResolvePlayer();
		if (player == null) return;

		bool dead = player.IsDead;
		if (dead == wasDead) return;

		wasDead = dead;
		if (!dead) ResetToInitialState();   // 从死变活 = 刚复活
	}

	/// <summary>把目标全部重设回初始状态（复活时自动调，也可以自己接在别的事件上）</summary>
	public void ResetToInitialState()
	{
		foreach (TargetState state in initialStates)
		{
			if (state.target == null) continue;

			state.target.SetActive(state.initialActive);

			if (state.initialActive && resetPlatforms) ResetPlatforms(state.target);
		}

		if (onReset != null) onReset.Invoke();
	}

	/// <summary>记下每个目标的初始启用状态，只记一次</summary>
	private void CaptureInitialStates()
	{
		if (initialStates.Count > 0 || targets == null) return;

		foreach (GameObject target in targets)
		{
			if (target == null) continue;

			initialStates.Add(new TargetState { target = target, initialActive = target.activeSelf });
		}
	}

	/// <summary>找场景里的玩家，跳过影子（影子是克隆玩家生成的，身上也带 Player_Health）</summary>
	private void ResolvePlayer()
	{
		if (player != null) return;

		foreach (Player_Health candidate in FindObjectsOfType<Player_Health>())
		{
			if (candidate == null) continue;

			PlayerController controller = candidate.GetComponent<PlayerController>();
			if (controller != null && controller.IsGhost) continue;

			player = candidate;
			return;
		}
	}

	/// <summary>让目标（含子物体）上的平台复位成完好状态；没挂平台就什么都不做</summary>
	private static void ResetPlatforms(GameObject target)
	{
		foreach (MovingPlatform platform in target.GetComponentsInChildren<MovingPlatform>(true))
		{
			platform.ResetPlatform();
		}
	}
}
