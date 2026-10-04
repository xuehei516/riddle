using UnityEngine;

/// <summary>
/// 机关的信号源。每个信号源在门内只占一个输入
/// </summary>
public abstract class SignalSource : MonoBehaviour
{
	[Header("关联的机关门")]
	[Tooltip("信号源对应的机关门")]
	[SerializeField] private Door targetDoor;
	[Tooltip("如果有多个机关门需要同时响应这个信号源，可以在这里添加额外的门")]
	[SerializeField] private Door[] additionalDoors;

	/// <summary>
	/// 当前信号状态
	/// </summary>
	public bool IsActive { get; private set; }

	protected void SetSignal(bool active)
	{
		if (IsActive == active) 
			return;

		IsActive = active;
		if (targetDoor != null) 
			targetDoor.SetSignal(this, active);

		if (additionalDoors == null) 
			return;
		foreach (Door door in additionalDoors)
		{
			if (door != null && door != targetDoor) 
				door.SetSignal(this, active);
		}
	}

	protected virtual void OnDisable()
	{
		SetSignal(false);
	}
}
