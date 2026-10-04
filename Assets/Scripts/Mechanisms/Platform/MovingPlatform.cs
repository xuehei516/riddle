using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
	[Header("移动端点")]
	[SerializeField] protected Transform startPoint;
	[SerializeField] protected Transform endPoint;

	[Header("移动速度")]
	[SerializeField] protected float speed = 2f;

	protected Rigidbody2D rigidBody2D;
	protected Transform targetPoint;

	public float VelocityX { get; protected set; }
	/// <summary>
	/// 平台是否能移动
	/// </summary>
	protected bool IsMoving => startPoint != null && endPoint != null && speed > 0f;

	protected virtual void Awake()
	{
		rigidBody2D = GetComponent<Rigidbody2D>();
		targetPoint = endPoint;
	}

	protected virtual void FixedUpdate()
	{
		// 如果没有设置端点或者速度为0，则不移动
		if (!IsMoving || rigidBody2D == null)
		{
			VelocityX = 0f;
			return;
		}

		// 计算下一帧的物理位置
		Vector2 newPos = Vector2.MoveTowards(rigidBody2D.position, targetPoint.position, speed * Time.fixedDeltaTime);
		rigidBody2D.MovePosition(newPos);

		VelocityX = (newPos.x - rigidBody2D.position.x) / Time.fixedDeltaTime;

		// 到达端点掉头
		if (Vector2.Distance(rigidBody2D.position, targetPoint.position) < 0.05f)
		{
			if (targetPoint == endPoint)
				targetPoint = startPoint;
			else
				targetPoint = endPoint;
		}
	}
}
