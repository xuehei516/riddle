using System.Collections;
using UnityEngine;

public class CutsceneBars : MonoBehaviour
{
	public static CutsceneBars Instance { get; private set; }

	[Header("UI 引用")]
	[SerializeField] private RectTransform topBar;
	[SerializeField] private RectTransform bottomBar;

	[Header("黑边参数")]
	[Tooltip("黑边向内缩进的最大高度")]
	[SerializeField] private float targetHeight = 120f;

	[Tooltip("黑边切入/退出的平滑时长（秒）")]
	[SerializeField] private float transitionDuration = 0.5f;

	private Coroutine barCoroutine;

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);

		// 游戏初始时确保高度为 0（隐藏）
		SetHeight(0f);
	}

	/// <summary>
	/// 显示黑边（过场开始时调用）
	/// </summary>
	/// <param name="duration"></param>
	public void Show(float duration = -1f)
	{
		float d = duration > 0 ? duration : transitionDuration;
		StartAnimate(targetHeight, d);
	}

	/// <summary>
	/// 隐藏黑边（过场结束时调用）
	/// </summary>
	/// <param name="duration"></param>
	public void Hide(float duration = -1f)
	{
		float d = duration > 0 ? duration : transitionDuration;
		StartAnimate(0f, d);
	}

	private void StartAnimate(float targetH, float duration)
	{
		if (barCoroutine != null) 
			StopCoroutine(barCoroutine);
		barCoroutine = StartCoroutine(AnimateBarsRoutine(targetH, duration));
	}

	// 过渡协程
	private IEnumerator AnimateBarsRoutine(float targetH, float duration)
	{
		float currentH = topBar.sizeDelta.y;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);

			float newH = Mathf.SmoothStep(currentH, targetH, t);
			SetHeight(newH);

			yield return null;
		}

		SetHeight(targetH);
		barCoroutine = null;
	}

	private void SetHeight(float h)
	{
		topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, h);
		bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, h);
	}
}