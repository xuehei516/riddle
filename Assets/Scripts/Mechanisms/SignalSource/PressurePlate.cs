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

	[Header("关联的旋转机关")]
	[Tooltip("踩住时绕自身中点旋转的物体（留空则不做旋转）")]
	[SerializeField] private Transform rotatingTarget;
	[Tooltip("旋转角速度，单位：度/秒")]
	[SerializeField, Min(0f)] private float rotateSpeed = 90f;
	[Tooltip("踩住时最多逆时针转过的角度（度）。松开时会沿原路顺时针转回 0")]
	[SerializeField, Min(0f)] private float maxRotateAngle = 180f;

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

	/// <summary>旋转机关的初始姿态，松开时回到这里</summary>
	private Quaternion rotatingInitialRotation;
	/// <summary>旋转机关的初始本地坐标（绕中点旋转会补偿位移，收尾时要还回去）</summary>
	private Vector3 rotatingInitialLocalPosition;
	/// <summary>用来算"自身中点"的渲染器</summary>
	private Renderer rotatingRenderer;
	/// <summary>当前相对初始姿态转过的角度（逆时针为正，度）</summary>
	private float currentRotateAngle;

	private void Awake()
	{
		if (pressDownSpriteTransform != null)
		{
			originalPositionY = pressDownSpriteTransform.localPosition.y;
			pressedPositionY = originalPositionY - pressDownDistance;
		}

		// 记录旋转机关的初始姿态。注意不能写成提前 return，
		// 否则没配下陷子物体时这段就永远不执行了
		if (rotatingTarget != null)
		{
			rotatingInitialRotation = rotatingTarget.localRotation;
			rotatingInitialLocalPosition = rotatingTarget.localPosition;
			rotatingRenderer = rotatingTarget.GetComponentInChildren<Renderer>();
		}
	}

	private void Update()
	{
		UpdateRotatingTarget();
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

		// 旋转机关直接还回初始姿态
		currentRotateAngle = 0f;
		if (rotatingTarget != null)
		{
			rotatingTarget.localRotation = rotatingInitialRotation;
			rotatingTarget.localPosition = rotatingInitialLocalPosition;
		}

		base.OnDisable();
	}

	/// <summary>
	/// 踩住 → 逆时针转到 maxRotateAngle；松开 → 顺时针转回 0。
	/// 目标是角度值，用 MoveTowards 逼近，所以速度恒定且不会过冲。
	/// </summary>
	private void UpdateRotatingTarget()
	{
		if (rotatingTarget == null) return;

		float targetAngle = IsActive ? maxRotateAngle : 0f;
		if (Mathf.Approximately(currentRotateAngle, targetAngle)) return;

		currentRotateAngle = Mathf.MoveTowards(currentRotateAngle, targetAngle, rotateSpeed * Time.deltaTime);
		ApplyRotateAngle(currentRotateAngle);
	}

	/// <summary>
	/// 把物体摆到「初始姿态 + angle 度（逆时针为正）」。
	/// 关键点：先记住旋转前视觉中点的世界坐标，转完再把中点推回原位 —— 
	/// 这样才是绕**自身中点**转，而不是绕 Transform 轴心转（Sprite 轴心不在正中时会有明显公转位移）。
	/// </summary>
	private void ApplyRotateAngle(float angle)
	{
		Vector3 centerBefore = GetRotatingCenter();

		rotatingTarget.localRotation = rotatingInitialRotation * Quaternion.Euler(0f, 0f, angle);

		Vector3 centerAfter = GetRotatingCenter();
		rotatingTarget.position += centerBefore - centerAfter;
	}

	/// <summary>取旋转物体的视觉中点；没有渲染器时退回轴心位置</summary>
	private Vector3 GetRotatingCenter()
	{
		return rotatingRenderer != null ? rotatingRenderer.bounds.center : rotatingTarget.position;
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
