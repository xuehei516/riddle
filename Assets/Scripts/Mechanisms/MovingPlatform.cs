using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
	[Header("移动端点")]
	[SerializeField] private Transform startPoint;
	[SerializeField] private Transform endPoint;

	[Header("移动速度")]
	[SerializeField] private float speed = 2f;

	private Rigidbody2D rigidBody2D;
	private Transform targetPoint;

	public float VelocityX { get; private set; }

	private void Awake()
	{
		targetPoint = endPoint;
		rigidBody2D = GetComponent<Rigidbody2D>();
	}

	private void FixedUpdate()
	{
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
