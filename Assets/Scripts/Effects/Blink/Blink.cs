using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 让一组 GameObject 闪烁。
///
/// 只开关目标身上（含子物体）所有 Renderer 的 enabled，**不动对象本身的启用状态**，
/// 所以闪烁期间目标的脚本、碰撞体、动画都照常运行——适合做「即将消失 / 危险」的提示。
///
/// 用法：
///   - 把要闪的对象拖进 targets（可以留空，也可以多个）
///   - playOnStart 勾上就自动开始；也可以在 Inspector 事件或代码里调 StartBlink / StopBlink / ToggleBlink
/// 停止闪烁（或组件被禁用）时，会把每个 Renderer 还原成闪烁前的状态。
/// </summary>
[AddComponentMenu("效果/闪烁 (Blink)")]
[DisallowMultipleComponent]
public class Blink : MonoBehaviour
{
	[Header("闪烁目标")]
	[Tooltip("要闪烁的对象，子物体上的 Renderer 一起管；可以留空，也可以拖多个")]
	[SerializeField] private GameObject[] targets;

	[Header("节奏")]
	[Tooltip("亮 / 灭各持续多少秒")]
	[SerializeField, Min(0.01f)] private float interval = 0.15f;

	[Tooltip("闪几次（一次 = 灭 + 亮）；0 表示一直闪")]
	[SerializeField, Min(0)] private int blinkCount = 0;

	[Tooltip("进入游戏就自动开始闪")]
	[SerializeField] private bool playOnStart = true;

	/// <summary>当前是不是正在闪</summary>
	public bool IsBlinking => isBlinking;

	/// <summary>闪烁用到的 Renderer，以及它们闪烁前的启用状态</summary>
	private readonly List<Renderer> renderers = new List<Renderer>();
	private readonly List<bool> originalEnabled = new List<bool>();

	private Coroutine blinkRoutine;
	private bool isBlinking;

	private void Start()
	{
		if (playOnStart) StartBlink();
	}

	private void OnDisable() => StopBlink();

	/// <summary>开始闪烁（已经在闪时重复调用没有副作用）</summary>
	public void StartBlink()
	{
		if (isBlinking) return;

		CacheRenderers();
		if (renderers.Count == 0) return;

		blinkRoutine = StartCoroutine(BlinkRoutine());
	}

	/// <summary>停止闪烁，并把所有 Renderer 还原成闪烁前的状态</summary>
	public void StopBlink()
	{
		if (!isBlinking) return;

		if (blinkRoutine != null)
		{
			StopCoroutine(blinkRoutine);
			blinkRoutine = null;
		}

		isBlinking = false;
		RestoreRenderers();
	}

	/// <summary>正在闪就停、没在闪就开始（方便直接接在按钮 / 拉杆 / 压力板事件上）</summary>
	public void ToggleBlink()
	{
		if (isBlinking) StopBlink();
		else StartBlink();
	}

	private IEnumerator BlinkRoutine()
	{
		isBlinking = true;

		// 一次闪烁 = 灭 + 亮，所以切换次数是次数的两倍；0 表示无限
		int toggles = blinkCount > 0 ? blinkCount * 2 : int.MaxValue;

		for (int i = 0; i < toggles; i++)
		{
			SetRenderersEnabled(i % 2 != 0);   // 先灭后亮，闪完最后停在「亮」上
			yield return new WaitForSeconds(interval);
		}

		blinkRoutine = null;
		isBlinking = false;
		RestoreRenderers();
	}

	/// <summary>把所有目标（含子物体）的 Renderer 和它们当前的启用状态记下来，只记一次</summary>
	private void CacheRenderers()
	{
		if (renderers.Count > 0 || targets == null) return;

		foreach (GameObject target in targets)
		{
			if (target == null) continue;

			foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
			{
				renderers.Add(renderer);
				originalEnabled.Add(renderer.enabled);
			}
		}
	}

	private void SetRenderersEnabled(bool visible)
	{
		foreach (Renderer renderer in renderers)
		{
			if (renderer != null) renderer.enabled = visible;
		}
	}

	private void RestoreRenderers()
	{
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null) renderers[i].enabled = originalEnabled[i];
		}
	}
}
