using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 条件开关：检测 A 组对象是否**全部启用**，一旦「全部启用」这件事成立，
/// 就把 B 组对象的启用状态**反转**一次（本来启用的关掉、本来没启用的启用）。
///
///   A 组：每帧检查，只在这个条件**从不成立变成成立**的那一下触发（不会每帧反复翻 B）
///   B 组：触发时逐个反转；可以留空，也可以拖多个
///
/// 两个默认行为，都写在字段上了：
///   - 进关卡时如果 A 已经全部启用，不会立刻反转 B（要等 A 真的发生「变成全部启用」这个变化）
///   - 条件不再成立时，默认不动 B；勾上 restoreWhenLost 就还原成反转之前的样子
/// </summary>
public class AllActiveInverter : MonoBehaviour
{
	[Header("检测：这些对象是否全部启用")]
	[Tooltip("全部启用时才触发；可以留空，也可以拖多个")]
	[SerializeField] private GameObject[] conditions;

	[Header("触发：反转这些对象")]
	[Tooltip("条件成立时逐个反转启用状态（本来启用的关掉、本来没启用的启用）；可以留空，也可以拖多个")]
	[SerializeField] private GameObject[] targetObjects;

	[Tooltip("条件不再成立时，把目标还原成反转之前的样子")]
	[SerializeField] private bool restoreWhenLost = false;

	[Header("音效 / 特效接口（可选）")]
	[Tooltip("条件变成「全部启用」并且反转了目标时触发")]
	[SerializeField] private UnityEvent onTriggered;

	/// <summary>
	/// 目标在反转之前的状态，只有需要还原（restoreWhenLost）时才记录
	/// </summary>
	private struct TargetState
	{
		public GameObject target;
		public bool wasActive;
	}

	private readonly List<TargetState> changedTargets = new List<TargetState>();

	/// <summary>最后一次检查时，A 组是不是全部启用</summary>
	public bool AreConditionsMet { get; private set; }

	private void OnEnable()
	{
		// 开局（或重新启用）先对一次表，免得刚进关卡就白翻一次 B
		AreConditionsMet = CheckConditions();
	}

	private void Update()
	{
		bool met = CheckConditions();
		if (met == AreConditionsMet) return;

		AreConditionsMet = met;

		if (met)
		{
			InvertTargets();

			if (onTriggered != null) onTriggered.Invoke();
		}
		else if (restoreWhenLost)
		{
			RestoreTargets();
		}
	}

	/// <summary>
	/// A 组对象是不是都在场景里启用着：对象自己的开关关了、或者父物体关了，都算未启用
	/// </summary>
	private bool CheckConditions()
	{
		if (conditions == null) return true;

		foreach (GameObject condition in conditions)
		{
			if (condition == null || !condition.activeInHierarchy) return false;
		}

		return true;
	}

	/// <summary>
	/// 逐个反转目标，需要还原的话先记下反转前的状态
	/// </summary>
	private void InvertTargets()
	{
		if (targetObjects == null) return;

		foreach (GameObject target in targetObjects)
		{
			if (target == null) continue;

			bool wasActive = target.activeSelf;
			if (restoreWhenLost) changedTargets.Add(new TargetState { target = target, wasActive = wasActive });

			target.SetActive(!wasActive);

			// 刚被启用：如果是平台，让它从「已粉碎 / 已消失」里复位
			if (!wasActive) ResetPlatforms(target);
		}
	}

	/// <summary>
	/// 还原成反转之前的样子（只有勾了 restoreWhenLost 时名单里才有内容）
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
