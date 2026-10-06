using UnityEngine;

/// <summary>
/// 玩家或回放幽灵靠近并按 E 后，只发出一次信号
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : SignalSource
{
	[SerializeField] private Animator animator;

	/// <summary>
	/// 是否已经被拉过了，拉过后不会再发出信号
	/// </summary>
	private bool wasPulled = false;

	public void TryPull()
	{
		wasPulled = !wasPulled;
		SetSignal(wasPulled);

		print("拉杆被拉动，发出信号");
		// if (animator != null && !string.IsNullOrEmpty(pullTrigger))
		//	 animator.SetTrigger(pullTrigger);
	}
}
