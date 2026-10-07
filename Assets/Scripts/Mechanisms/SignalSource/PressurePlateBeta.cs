using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 压力板的复制版：行为与原版一致（玩家或影子踩上去 → 下陷 + SetSignal(true)，
/// 离开 → 回弹 + SetSignal(false)，信号照旧转发给基类里关联的门 / 移动平台），
/// 额外多一个功能：被踩下时启用一个目标 GameObject。
/// </summary>
public class PressurePlateBeta : SignalSource
{
	[Header("踏板视觉反馈")]
	[Tooltip("踩下时压力板下陷的深度")]
	[SerializeField] private float pressDownDistance = 0.08f;

	[Header("下陷的子物体")]
	[Tooltip("使用子物体，避免碰撞体变化")]
	[SerializeField] private Transform pressDownSpriteTransform;
	[Tooltip("下陷速度")]
	[SerializeField] private float pressDuration = 0.08f;
	[Tooltip("回弹速度")]
	[SerializeField] private float releaseDuration = 0.12f;

	[Header("被踩时启用的目标")]
	[Tooltip("压力板被踩下时 SetActive(true) 的对象；可以留空不填")]
	[SerializeField] private GameObject targetObject;

	/// <summary>
	/// 初始位置的 Y 坐标
	/// </summary>
	private float originalPositionY;
	/// <summary>
	/// 下陷位置的 Y 坐标
	/// </summary>
	private float pressedPositionY;
	/// <summary>
	/// 当前压力板上的物体数量
	/// </summary>
	private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();

	private Coroutine moveCoroutine;

	private void Awake()
	{
		if (pressDownSpriteTransform == null) return;
		originalPositionY = pressDownSpriteTransform.localPosition.y;
		pressedPositionY = originalPositionY - pressDownDistance;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		// 对象被别的机关关掉时，物理回调可能是延迟送达的：直接忽略，别去启动协程 / 改状态
		if (!isActiveAndEnabled) return;

		// 检测进入的物体是否为玩家或幽灵（影子是克隆玩家生成的，身上也带 PlayerController）
		if (collision.GetComponentInParent<PlayerController>() == null) return;
		if (!occupants.Add(collision)) return;

		if (occupants.Count == 1)
		{
			StartPlateMove(pressedPositionY, pressDuration);
			SetSignal(true);
			ActivateTarget();
		}
	}

	/// <summary>
	/// 离开压力板时，移除该物体并检查是否需要回弹
	/// </summary>
	/// <param name="collision"></param>
	private void OnTriggerExit2D(Collider2D collision)
	{
		// 同上：板子已经失活时什么都不做（否则会去还原目标物体、启动协程）
		if (!isActiveAndEnabled) return;

		if (occupants.Remove(collision))
		{
			if (occupants.Count == 0)
			{
				StartPlateMove(originalPositionY, releaseDuration);
				SetSignal(false);
			}
		}
	}

	/// <summary>
	/// 启用目标物体（没配就什么都不做）
	/// </summary>
	private void ActivateTarget()
	{
		if (targetObject == null) return;

		targetObject.SetActive(true);
	}

	protected override void OnDisable()
	{
		if (moveCoroutine != null) StopCoroutine(moveCoroutine);
		moveCoroutine = null;
		occupants.Clear();
		if (pressDownSpriteTransform != null)
		{
			Vector3 position = pressDownSpriteTransform.localPosition;
			position.y = originalPositionY;
			pressDownSpriteTransform.localPosition = position;
		}
		base.OnDisable();
	}

	/// <summary>
	/// 开启压力板的移动协程
	/// </summary>
	/// <param name="targetPositionY"></param>
	/// <param name="duration"></param>
	private void StartPlateMove(float targetPositionY, float duration)
	{
		if (pressDownSpriteTransform == null) return;
		if (!isActiveAndEnabled) return;   // 没启用的对象不能 StartCoroutine
		if (moveCoroutine != null)
		{
			StopCoroutine(moveCoroutine);
		}

		moveCoroutine = StartCoroutine(AnimateMoveRoutine(targetPositionY, duration));
	}

	private IEnumerator AnimateMoveRoutine(float targetY, float duration)
	{
		float startY = pressDownSpriteTransform.localPosition.y;
		float time = 0f;

		while (time < duration)
		{
			time += Time.deltaTime;
			float t = time / duration;
			float currentY = Mathf.SmoothStep(startY, targetY, t);

			pressDownSpriteTransform.localPosition = new Vector3(pressDownSpriteTransform.localPosition.x, currentY, pressDownSpriteTransform.localPosition.z);

			yield return null;
		}

		pressDownSpriteTransform.localPosition = new Vector3(pressDownSpriteTransform.localPosition.x, targetY, pressDownSpriteTransform.localPosition.z);

		moveCoroutine = null;
	}
}
