using System;
using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour
{
	public event Action<PlayerAnimationEvent, PlayerAnimationStateArgs> OnAnimationStateChanged;

	public event Action<PlayerAnimationEvent> OnAttack;

	public void CallAnimationStateChanged(float horizontalSpeed, float verticalSpeed, bool isGrounded, bool facingLeft)
	{
		OnAnimationStateChanged?.Invoke(this, new PlayerAnimationStateArgs()
		{
			horizontalSpeed = horizontalSpeed,
			verticalSpeed = verticalSpeed,
			isGrounded = isGrounded,
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
	public float horizontalSpeed;
	public float verticalSpeed;
	public bool isGrounded;
	public bool facingLeft;
}