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

	[Header("信号设置")]
	[SerializeField] private bool requireSignal;

	private readonly HashSet<SignalSource> activeSources = new HashSet<SignalSource>();

	protected Rigidbody2D rigidBody2D;
	protected Transform targetPoint;

	public float VelocityX { get; protected set; }

	/// <summary>
	/// 平台是否能移动
	/// </summary>
	protected bool IsMoving => startPoint != null && endPoint != null && speed > 0f && (!requireSignal || activeSources.Count > 0);

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

	/// <summary>
	/// 设置信号源的激活状态
	/// </summary>
	/// <param name="source">信号源</param>
	/// <param name="active">是否激活</param>
	public void SetSignal(SignalSource source, bool active)
	{
		if (source == null) 
			return;

		if (active)
			activeSources.Add(source);
		else
			activeSources.Remove(source);
	}

	/// <summary>
	/// 把平台复位成「完好的初始状态」。
	/// 自己管着可见性 / 碰撞体开关的子类（易碎平台、限时平台）要 override：
	/// 外面的开关（比如拉杆）重新启用平台时会调它，否则平台可能一直卡在「已粉碎 / 已消失」的状态里。
	/// </summary>
	public virtual void ResetPlatform()
	{
	}
}
