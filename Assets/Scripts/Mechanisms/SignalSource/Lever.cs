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

	protected virtual void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	/// <summary>
	/// 切换开关状态：拉一次开、再拉一次关（子类可以 override 追加自己的效果）
	/// </summary>
	public virtual void TryPull()
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
