using UnityEngine;

/// <summary>
/// 玩家或回放幽灵靠近并按 E 后，只发出一次信号
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : SignalSource
{
	private SpriteRenderer spriteRenderer;

	[SerializeField] private Sprite opened;
	[SerializeField] private Sprite closed;

	/// <summary>
	/// 是否已经被拉过了，拉过后不会再发出信号
	/// </summary>
	private bool wasPulled = false;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public void TryPull()
	{
		wasPulled = !wasPulled;
		SetSignal(wasPulled);

		if (wasPulled)
		{
			spriteRenderer.sprite = opened;
		}
		else
		{
			spriteRenderer.sprite = closed;
		}
	}
}
