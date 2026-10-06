using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
	public static ScreenFader Instance { get; private set; }

	[Header("UI 组件")]
	[SerializeField] private CanvasGroup canvasGroup;

	[Header("默认时长")]
	[SerializeField] private float defaultFadeDuration = 0.5f;

	/// <summary>
	/// 当前正在进行的淡入淡出协程
	/// </summary>
	private Coroutine fadeRoutine;
	private Coroutine pendingFadeIn;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;

		if (Instance == this)
			Instance = null;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (pendingFadeIn != null)
			StopCoroutine(pendingFadeIn);

		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
			fadeRoutine = null;
		}

		canvasGroup.alpha = 1f;
		canvasGroup.blocksRaycasts = true;
		pendingFadeIn = StartCoroutine(FadeInAfterFirstFrame());
	}

	/// <summary>
	/// 淡入屏幕（从黑到透明）
	/// </summary>
	/// <param name="duration"></param>
	/// <returns></returns>
	public Coroutine FadeIn(float duration = -1f)
	{
		float fadeDuration = duration > 0f ? duration : defaultFadeDuration;
		StartFade(canvasGroup.alpha, 0f, fadeDuration);
		return fadeRoutine;
	}

	/// <summary>
	/// 淡出屏幕（从透明到黑）
	/// </summary>
	/// <param name="duration"></param>
	/// <returns></returns>
	public Coroutine FadeOut(float duration = -1f)
	{
		float fadeDuration = duration > 0f ? duration : defaultFadeDuration;
		StartFade(canvasGroup.alpha, 1f, fadeDuration);
		return fadeRoutine;
	}

	private void StartFade(float startAlpha, float targetAlpha, float duration)
	{
		if (pendingFadeIn != null)
		{
			StopCoroutine(pendingFadeIn);
			pendingFadeIn = null;
		}

		if (fadeRoutine != null)
			StopCoroutine(fadeRoutine);

		fadeRoutine = StartCoroutine(FadeRoutine(startAlpha, targetAlpha, duration));
	}

	private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration)
	{
		float time = 0f;
		canvasGroup.alpha = startAlpha;
		canvasGroup.blocksRaycasts = targetAlpha > 0.5f;

		while (time < duration)
		{
			time += Time.unscaledDeltaTime;
			float progress = Mathf.Clamp01(time / duration);
			if (targetAlpha < startAlpha)
				progress = 1f - (1f - progress) * (1f - progress);
			canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
			yield return null;
		}

		canvasGroup.alpha = targetAlpha;
		canvasGroup.blocksRaycasts = targetAlpha > 0.5f;
		fadeRoutine = null;
	}

	private IEnumerator FadeInAfterFirstFrame()
	{
		// 场景加载事件早于新场景的第一帧渲染
		yield return new WaitForEndOfFrame();
		pendingFadeIn = null;
		FadeIn();
	}
}
