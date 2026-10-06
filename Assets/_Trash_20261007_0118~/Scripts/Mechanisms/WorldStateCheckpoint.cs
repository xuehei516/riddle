// 归档（01:18 之后新增的脚本，已移出工程）。
// 这是最后一版实现：只记「拉杆自己的开关状态」，回滚时让拉杆切回去，
// 下游的门 / 平台 / 目标物体由它们自己的信号逻辑跟着走。
// 用的是 Lever 已有的公开接口（读状态 IsActive、改状态 TryPull），不需要改 Lever。

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 拉杆状态的「存档 / 回滚」。
///
/// 关键点：只记**拉杆自己的开关状态**，不碰它下游的门 / 移动平台 / 目标物体。
/// 回滚时让拉杆切回存档时的状态，它会按原有逻辑重新广播信号（SignalSource.SetSignal），
/// 下游的门、移动平台、LeverBeta 的目标物体自然跟着回到该有的状态。
///
/// 规则：
///   - 开局先记一份（= 出生时的拉杆状态）；
///   - 玩家走到复活点（复活点被激活）→ 存档：当前拉杆状态成为新基准；
///   - 玩家死亡复活 → 回滚到上次存档的拉杆状态。
/// </summary>
[AddComponentMenu("机关/拉杆状态存档 (World State Checkpoint)")]
[DisallowMultipleComponent]
public class WorldStateCheckpoint : MonoBehaviour
{
	[Header("要记录 / 回滚的拉杆")]
	[Tooltip("拖进需要记住开关状态的拉杆；会影响关卡状态的拉杆都建议拖进来")]
	[SerializeField] private Lever[] trackedLevers;

	[Header("音效 / 特效接口（可选）")]
	[Tooltip("玩家到复活点存档时触发")]
	[SerializeField] private UnityEvent onSaved;

	[Tooltip("死亡复活、回滚到上次存档时触发")]
	[SerializeField] private UnityEvent onRolledBack;

	/// <summary>一根拉杆被记下来的状态</summary>
	private struct LeverState
	{
		public Lever lever;
		public bool open;
	}

	private readonly List<LeverState> savedStates = new List<LeverState>();

	private void Awake()
	{
		// 开局先记一份：没去过任何复活点就死的话，回滚到这里（不播音效）
		CaptureStates();
	}

	private void OnEnable()
	{
		RespawnPoint.OnPointActivated += HandlePointActivated;
		RespawnPoint.OnPlayerRespawned += HandlePlayerRespawned;
	}

	private void OnDisable()
	{
		RespawnPoint.OnPointActivated -= HandlePointActivated;
		RespawnPoint.OnPlayerRespawned -= HandlePlayerRespawned;
	}

	/// <summary>玩家踩到复活点 → 存档</summary>
	private void HandlePointActivated(RespawnPoint point) => Save();

	/// <summary>玩家复活 → 回到上次存档的拉杆状态</summary>
	private void HandlePlayerRespawned(RespawnPoint point) => Rollback();

	/// <summary>存档：把当前拉杆状态记成新的基准</summary>
	public void Save()
	{
		CaptureStates();

		if (onSaved != null) onSaved.Invoke();
	}

	/// <summary>回滚：把每根拉杆恢复成上次存档时的开关状态（下游的门 / 平台会自动跟着走）</summary>
	public void Rollback()
	{
		foreach (LeverState state in savedStates)
		{
			if (state.lever == null) continue;

			RestoreLever(state.lever, state.open);
		}

		if (onRolledBack != null) onRolledBack.Invoke();
	}

	/// <summary>记下每根拉杆当前的开关状态，覆盖掉旧的那份</summary>
	private void CaptureStates()
	{
		savedStates.Clear();

		if (trackedLevers == null) return;

		foreach (Lever lever in trackedLevers)
		{
			if (lever == null) continue;

			savedStates.Add(new LeverState { lever = lever, open = lever.IsActive });
		}
	}

	/// <summary>
	/// 把一根拉杆设回指定状态。Lever 只提供「切换」接口（TryPull），
	/// 所以做法是：当前状态和存档不一致就切；极少数情况（拉杆被其它机关禁用过，
	/// 内部开关和信号不同步）切一次还不到位，最多再切一次兜底。
	/// </summary>
	private static void RestoreLever(Lever lever, bool open)
	{
		for (int i = 0; i < 2 && lever.IsActive != open; i++)
		{
			lever.TryPull();
		}
	}
}
