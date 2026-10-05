using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D), typeof(PlatformEffector2D))]
public class TwoWayPlatform : MonoBehaviour
{
	private const float DropInputBuffer = 0.15f;
	private float dropInputExpiresAt = float.NegativeInfinity;
	private float fallThroughDuration = 1f;
	private Collider2D platformCollider;

	private void Awake()
	{
		platformCollider = GetComponent<Collider2D>();
	}

	private void Update()
	{
		if (Keyboard.current == null) 
			return;

		if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
		{
			dropInputExpiresAt = Time.time + DropInputBuffer;
		}
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		if (!collision.gameObject.CompareTag("Player")) return;
		if (Time.time > dropInputExpiresAt) return;

		bool playerIsAbove = false;
		for (int i = 0; i < collision.contactCount; i++)
		{
			if (collision.GetContact(i).normal.y < -0.5f)
			{
				playerIsAbove = true;
				break;
			}
		}

		if (!playerIsAbove) return;

		// 消耗这次按键，避免重复触发
		dropInputExpiresAt = float.NegativeInfinity;
		StartCoroutine(DropThroughRoutine(collision.collider));
	}

	private IEnumerator DropThroughRoutine(Collider2D playerCollider)
	{
		Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
		yield return new WaitForSeconds(fallThroughDuration);
		Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
	}
}