using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : SignalSource
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
		// 检测进入的物体是否为玩家或幽灵
		PlayerController controller = collision.GetComponentInParent<PlayerController>();
		if (controller != null && (controller.CompareTag("Player") || controller.IsGhost) && occupants.Add(collision))
		{
			if (occupants.Count == 1)
			{
				StartPlateMove(pressedPositionY, pressDuration);
				SetSignal(true);
			}
		}
	}

	/// <summary>
	/// 离开压力板时，移除该物体并检查是否需要回弹
	/// </summary>
	/// <param name="collision"></param>
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (occupants.Remove(collision))
		{
			if (occupants.Count == 0)
			{
				StartPlateMove(originalPositionY, releaseDuration);
				SetSignal(false);
			}
		}
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
