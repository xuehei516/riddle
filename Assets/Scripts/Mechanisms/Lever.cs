using UnityEngine;

/// <summary>
/// 玩家或回放幽灵靠近并按 E 后，只发出一次信号
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : SignalSource
{
	[SerializeField] private Animator animator;
	[SerializeField] private string pullTrigger = "Pull";

	private bool wasPulled;

	public void TryPull()
	{
		if (wasPulled)
			return;

		wasPulled = true;
		SetSignal(true);
		if (animator != null && !string.IsNullOrEmpty(pullTrigger))
			animator.SetTrigger(pullTrigger);
	}
}
