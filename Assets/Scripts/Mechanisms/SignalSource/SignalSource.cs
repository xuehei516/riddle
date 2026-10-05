using UnityEngine;

/// <summary>
/// 机关信号源的抽象基类，负责保存信号状态，并把状态变化转发给关联的机关门和移动平台
/// </summary>
public abstract class SignalSource : MonoBehaviour
{
	[Header("关联的机关门")]
	[Tooltip("信号源对应的机关门")]
	[SerializeField] private Door targetDoor;
	[Tooltip("如果有多个机关门需要同时响应这个信号源，可以在这里添加额外的门")]
	[SerializeField] private Door[] additionalDoors;

	[Header("关联的移动平台")]
	[SerializeField] private MovingPlatform targetPlatform;
	[SerializeField] private MovingPlatform[] additionalPlatforms;

	/// <summary>
	/// 当前信号状态
	/// </summary>
	public bool IsActive { get; private set; }

	protected void SetSignal(bool active)
	{
		// 若新状态与 IsActive 相同，直接返回
		if (IsActive == active) 
			return;

		IsActive = active;
		if (targetDoor != null) 
			targetDoor.SetSignal(this, active);

		if (targetPlatform != null)
			targetPlatform.SetSignal(this, active);

		if (additionalPlatforms != null)
		{
			foreach (MovingPlatform platform in additionalPlatforms)
			{
				if (platform != null && platform != targetPlatform)
					platform.SetSignal(this, active);
			}
		}

		if (additionalDoors != null) 
		{
			foreach (Door door in additionalDoors)
			{
				if (door != null && door != targetDoor) 
					door.SetSignal(this, active);
			}
		}
	}

	protected virtual void OnDisable()
	{
		SetSignal(false);
	}
}
