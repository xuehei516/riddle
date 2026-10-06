using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 拉杆的复制版：行为和 Lever 完全一致（玩家或回放幽灵按 E 交互 → 开 / 关状态来回切，
/// 换 opened / closed 图，并把信号转发给基类关联的门和移动平台），
/// 额外多一个功能：用拉杆翻转一组目标 GameObject 的启用状态。
///
///   拉杆打开 → 数组里每个对象翻转一次：本来启用的关掉、本来没启用的启用
///   拉杆关闭 → 全部还原成拉杆打开之前的状态
///
/// 目标可以留空、可以拖多个；如果目标是平台（易碎 / 限时），被杀掉再重新启用时会顺带复位，
/// 避免平台卡在「已粉碎 / 已消失」的状态里出不来。
///
/// 注意必须继承 Lever：PlayerController 交互时是按 Lever 类型找目标的
/// （GetComponentInParent&lt;Lever&gt;()），做成兄弟类的话按 E 根本找不到这个拉杆。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LeverBeta : Lever
{
	/// <summary>目标在拉杆打开之前的状态，拉杆关闭时按这份名单还原</summary>
	private struct TargetState
	{
		public GameObject target;
		public bool wasActive;
	}

	[Header("拉杆要翻转的目标")]
	[Tooltip("拉杆打开时逐个翻转启用状态（本来启用的关掉、本来没启用的启用），拉杆关闭时全部还原；可以留空，也可以拖多个")]
	[SerializeField] private GameObject[] targetObjects;

	private readonly List<TargetState> changedTargets = new List<TargetState>();

	/// <summary>
	/// 切换开关状态：先走原版逻辑（换图 + SetSignal 广播），再翻转 / 还原目标物体
	/// </summary>
	public override void TryPull()
	{
		base.TryPull();

		if (IsActive) FlipTargets();
		else RestoreTargets();
	}

	/// <summary>
	/// 翻转每个目标：本来启用的关掉、本来没启用的启用，并记下翻转前的状态
	/// </summary>
	private void FlipTargets()
	{
		if (targetObjects == null) return;

		foreach (GameObject target in targetObjects)
		{
			if (target == null) continue;

			bool wasActive = target.activeSelf;

			changedTargets.Add(new TargetState { target = target, wasActive = wasActive });
			target.SetActive(!wasActive);

			// 刚被启用：如果是平台，让它从「已粉碎 / 已消失」里复位
			if (!wasActive) ResetPlatforms(target);
		}
	}

	/// <summary>
	/// 还原成拉杆打开之前的样子
	/// </summary>
	private void RestoreTargets()
	{
		foreach (TargetState state in changedTargets)
		{
			if (state.target == null) continue;

			state.target.SetActive(state.wasActive);

			if (state.wasActive) ResetPlatforms(state.target);
		}

		changedTargets.Clear();
	}

	/// <summary>
	/// 让目标（含子物体）上的平台复位成完好状态；没挂平台就什么都不做
	/// </summary>
	private static void ResetPlatforms(GameObject target)
	{
		foreach (MovingPlatform platform in target.GetComponentsInChildren<MovingPlatform>(true))
		{
			platform.ResetPlatform();
		}
	}
}
