using System;
using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour
{
	public event Action<PlayerAnimationEvent, PlayerAnimationStateArgs> OnAnimationStateChanged;

	public event Action<PlayerAnimationEvent> OnAttack;

	public void CallAnimationStateChanged(float speed, bool isFalling, bool facingLeft)
	{
		OnAnimationStateChanged?.Invoke(this, new PlayerAnimationStateArgs()
		{
			speed = speed,
			isFalling = isFalling,
			facingLeft = facingLeft
		});
	}

	public void CallAttack()
	{
		OnAttack?.Invoke(this);
	}
}

public class PlayerAnimationStateArgs : EventArgs
{
	public float speed;
	public bool isFalling;
	public bool facingLeft;
}