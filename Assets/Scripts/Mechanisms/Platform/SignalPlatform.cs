using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 信号驱动平台：默认停在 startPoint，只有收到信号（拉杆 / 压力板等 SignalSource）时才去 endPoint，
/// 信号消失就回 startPoint。
///
/// 和 MovingPlatform 的区别：基类是「在两点之间来回跑」，这个是由信号决定「该去哪」，
/// 不需要勾 requireSignal——有没有信号就是它的行为本身。
///
/// 用法：把 MovingPlatform 换成这个组件（Inspector 里的 startPoint / endPoint / speed
/// 要重新拖一次，换组件不会迁移字段），信号侧照旧用拉杆 / 压力板的「关联的移动平台」字段指过来。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SignalPlatform : MovingPlatform
{
	/// <summary>自己记一份「谁给我发了信号」：基类里那份是 private，子类拿不到</summary>
	private readonly HashSet<SignalSource> activeSources = new HashSet<SignalSource>();

	/// <summary>当前是否有信号</summary>
	public bool HasSignal => activeSources.Count > 0;

	protected override void Awake()
	{
		base.Awake();

		targetPoint = startPoint;   // 默认停在起点，等信号
	}

	protected override void FixedUpdate()
	{
		if (rigidBody2D == null || startPoint == null || endPoint == null || speed <= 0f)
		{
			VelocityX = 0f;
			return;
		}

		// 有信号去终点，没信号回起点
		targetPoint = HasSignal ? endPoint : startPoint;

		Vector2 newPos = Vector2.MoveTowards(rigidBody2D.position, targetPoint.position, speed * Time.fixedDeltaTime);
		rigidBody2D.MovePosition(newPos);

		VelocityX = (newPos.x - rigidBody2D.position.x) / Time.fixedDeltaTime;
	}

	/// <summary>
	/// 信号源通过基类字段调用过来：先走基类（通用的信号记录），再记自己这一份
	/// </summary>
	public override void SetSignal(SignalSource source, bool active)
	{
		base.SetSignal(source, active);

		if (source == null) return;

		if (active)
			activeSources.Add(source);
		else
			activeSources.Remove(source);
	}
}
