using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 影子回放结束后自动销毁影子（专属脚本，不改动任何现有脚本）。
///
/// 判断依据：影子身上的 PlayerController.ReplayFinished（录制数据播完时它会被置为 true）。
/// 所以不用去改 GhostReplaySystem / PlayerController，也不用管影子是谁生成的。
///
/// 用法：挂在场景里任意一个开局就启用的物体上。场景有它 = 回放一播完就销毁影子。
/// 想加点效果（消散动画 / 音效）可以接 onFinished，或者用 delayAfterFinished 留一点时间。
/// </summary>
[AddComponentMenu("影子/影子回放结束自动销毁 (Ghost Auto Destroy)")]
[DisallowMultipleComponent]
public class GhostAutoDestroy : MonoBehaviour
{
	[Header("销毁时机")]
	[Tooltip("影子的回放播完之后再等这么久才销毁（秒）。0 = 立刻销毁")]
	[SerializeField, Min(0f)] private float delayAfterFinished = 0f;

	[Header("扫描")]
	[Tooltip("还没找到影子时，每隔多久找一次")]
	[SerializeField, Min(0.05f)] private float scanInterval = 0.2f;

	[Header("音效 / 特效接口（可选）")]
	[Tooltip("回放播完（销毁之前）触发，可以接消散动画 / 音效")]
	[SerializeField] private UnityEvent onFinished;

	/// <summary>当前盯着的影子</summary>
	private PlayerController ghost;
	/// <summary>计划销毁的时刻（未安排时为 -1）</summary>
	private float destroyTime = -1f;
	private float nextScanTime;

	private void Update()
	{
		// 还没影子：定期找（影子是运行时克隆出来的）
		if (ghost == null)
		{
			ResetState();

			if (Time.unscaledTime < nextScanTime) return;

			nextScanTime = Time.unscaledTime + scanInterval;
			FindGhost();
			return;
		}

		// 刚发现回放播完：安排销毁时刻，并抛一次事件
		if (destroyTime < 0f)
		{
			if (!ghost.ReplayFinished) return;

			destroyTime = Time.unscaledTime + delayAfterFinished;

			if (onFinished != null) onFinished.Invoke();
		}

		if (Time.unscaledTime < destroyTime) return;

		GameObject target = ghost.gameObject;
		ResetState();

		if (target != null) Destroy(target);
	}

	/// <summary>找出场景里的影子</summary>
	private void FindGhost()
	{
		foreach (PlayerController controller in FindObjectsOfType<PlayerController>())
		{
			if (controller == null || !controller.IsGhost) continue;

			ghost = controller;
			destroyTime = -1f;
			return;
		}
	}

	/// <summary>清掉当前状态（影子已经被销毁、或者换了新的影子）</summary>
	private void ResetState()
	{
		ghost = null;
		destroyTime = -1f;
	}
}
