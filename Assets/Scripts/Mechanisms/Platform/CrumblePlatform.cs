using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CrumblePlatform : MovingPlatform
{
	public enum TriggerMode
	{
		OnStep,     // 踩上后开始碎裂
		OnJump      // 站在平台上向上跳时立即碎裂
	}

	[Header("触发方式")]
	[Tooltip("OnStep：踩上后开始碎裂；OnJump：站在平台上向上跳时立即碎裂")]
	[SerializeField] private TriggerMode triggerMode = TriggerMode.OnStep;

	[Header("时间参数")]
	[Tooltip("踩上后碎裂的时间")]
	[SerializeField] private float crumbleDelay = 0.4f;
	[Tooltip("碎裂后重新生成的时间")]
	[SerializeField] private float respawnTime = 3f;

	[Header("组件引用")]
	[SerializeField] private Collider2D platformCollider;
	[SerializeField] private SpriteRenderer spriteRenderer;

	private bool isCrumbling;

	protected override void FixedUpdate()
	{
		base.FixedUpdate();

		if (isCrumbling)
			VelocityX = 0f;
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (triggerMode == TriggerMode.OnStep && !isCrumbling && IsPlayerOrGhost(collision) &&
			collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
		{
			BeginCrumble();
		}
	}

	private bool IsPlayerOrGhost(Collision2D collision)
	{
		return collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Ghost");
	}

	/// <summary>
	/// 开始碎裂的协程
	/// </summary>
	private void BeginCrumble()
	{
		if (!isCrumbling)
			StartCoroutine(CrumbleRoutine());
	}

	private IEnumerator CrumbleRoutine()
	{
		isCrumbling = true;

		float time = 0f;
		while (time < crumbleDelay)
		{
			time += Time.deltaTime;

			transform.localPosition = transform.localPosition + new Vector3(Random.Range(-0.05f, 0.05f), 0f, 0f);
			
			yield return null;
		}

		if (platformCollider != null)
			platformCollider.enabled = false;
		if (spriteRenderer != null)
			spriteRenderer.enabled = false;

		// 如果设置了重新生成时间，则等待一段时间后重新启用碰撞器和渲染器
		if (respawnTime > 0f)
		{
			yield return new WaitForSeconds(respawnTime);

			if (platformCollider != null)
				platformCollider.enabled = true;
			if (spriteRenderer != null)
				spriteRenderer.enabled = true;
			isCrumbling = false;
		}
	}

	/// <summary>
	/// 玩家跳跃时的通知方法
	/// </summary>
	public void NotifyPlayerJump()
	{
		if (triggerMode != TriggerMode.OnJump || isCrumbling)
			return;

		BeginCrumble();
	}
}
