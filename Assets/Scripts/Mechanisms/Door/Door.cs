using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
	public enum DoorMode 
	{
		Mechanical,			// 机关门
		Timed,				// 限时机关门
		Sustained,			// 持续信号机关门
		MultipleSignals		// 双(多)信号机关门
	}

	[Header("信号规则")]
	[SerializeField] private DoorMode mode = DoorMode.Sustained;

	[Tooltip("限时门收到信号后保持开启的秒数")]
	[SerializeField, Min(0f)] private float openSeconds = 3f;

	[Tooltip("多信号门需要几个不同的信号源")]
	[SerializeField, Min(2)] private int requiredSignals = 2;

	[Tooltip("勾选后累计收到足够信号就永久开启；取消勾选则要求同时激活")]
	[SerializeField] private bool latchMultipleSignals = true;

	[Header("开门设置")]
	[Tooltip("门升起/移动的相对偏移量")]
	[SerializeField] private Vector2 openOffset = new Vector2(0, 3f);

	[Tooltip("开门/关门时间")]
	[SerializeField] private float duration = 3f;

	/// <summary>
	/// 关门位置
	/// </summary>
	private Vector2 closedPosition;
	/// <summary>
	/// 开门位置
	/// </summary>
	private Vector2 openPosition;

	/// <summary>
	/// 活跃的信号源集合
	/// </summary>
	private readonly HashSet<SignalSource> activeSources = new HashSet<SignalSource>();
	/// <summary>
	/// 已收到信号的信号源集合
	/// </summary>
	private readonly HashSet<SignalSource> receivedSources = new HashSet<SignalSource>();
	private bool isOpen;
	private float closeAtTime = -1f;

	private Coroutine doorRoutine;

	private void Awake()
	{
		closedPosition = transform.localPosition;
		openPosition = closedPosition + openOffset;
	}

	private void Update()
	{
		if (mode == DoorMode.Timed && closeAtTime >= 0f && Time.time >= closeAtTime)
		{
			closeAtTime = -1f;
			SetDoorOpen(false);
		}
	}

	/// <summary>
	/// 设置信号源的状态，门会根据当前模式和信号源的状态来决定是否开门，active为true表示信号源激活，false表示信号源失效
	/// </summary>
	public void SetSignal(SignalSource source, bool active)
	{
		if (source == null) 
			return;

		bool changed = active ? activeSources.Add(source) : activeSources.Remove(source);

		if (!changed) 
			return;
		if (active) 
			receivedSources.Add(source);

		switch (mode)
		{
			case DoorMode.Mechanical:
				if (active)
					SetDoorOpen(true);
				break;
			case DoorMode.Timed:
				if (active)
				{
					SetDoorOpen(true);
					closeAtTime = Time.time + openSeconds + duration;
				}
				break;
			case DoorMode.Sustained:
				SetDoorOpen(activeSources.Count > 0);
				break;
			case DoorMode.MultipleSignals:
				if (latchMultipleSignals)
				{
					if (receivedSources.Count >= requiredSignals) SetDoorOpen(true);
				}
				else SetDoorOpen(activeSources.Count >= requiredSignals);
				break;
		}
	}

	/// <summary>
	/// 设置门的开关状态，如果当前状态与目标状态相同则不做任何操作
	/// </summary>
	/// <param name="open"></param>
	public void SetDoorOpen(bool open)
	{
		if (isOpen == open) 
			return;

		isOpen = open;
		if (doorRoutine != null)
			StopCoroutine(doorRoutine);

		doorRoutine = StartCoroutine(AnimateDoorRoutine(open));
	}

	private IEnumerator AnimateDoorRoutine(bool open)
	{
		Vector3 start = transform.localPosition;
		Vector3 target = open ? openPosition : closedPosition;

		float time = 0f;

		while (time < duration)
		{
			time += Time.deltaTime;
			float t = time / duration;

			transform.localPosition = Vector3.Lerp(start, target, t);

			yield return null;
		}

		transform.localPosition = target;
		doorRoutine = null;
	}
}
