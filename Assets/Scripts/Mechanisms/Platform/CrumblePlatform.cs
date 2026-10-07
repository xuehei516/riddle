using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

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
	[SerializeField] private TilemapRenderer tilemapRenderer;

    private bool isCrumbling;
    /// <summary>正在跑的碎裂协程，复位时要停掉</summary>
    private Coroutine crumbleRoutine;
    /// <summary>碎裂前的抖动偏移，复位时要从位置上减掉</summary>
    private float shakeOffsetX;

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
			crumbleRoutine = StartCoroutine(CrumbleRoutine());
	}

	private IEnumerator CrumbleRoutine()
	{
		isCrumbling = true;

		float time = 0f;
		shakeOffsetX = 0f;
		while (time < crumbleDelay)
		{
			time += Time.deltaTime;

			// 先移除上一帧的抖动，保留平台本身的移动，再施加新的偏移
			Vector3 basePosition = transform.localPosition - new Vector3(shakeOffsetX, 0f, 0f);
			shakeOffsetX = Random.Range(-0.05f, 0.05f);
			transform.localPosition = basePosition + new Vector3(shakeOffsetX, 0f, 0f);
			
			yield return null;
		}

		transform.localPosition -= new Vector3(shakeOffsetX, 0f, 0f);

		if (platformCollider != null)
			platformCollider.enabled = false;
		if (spriteRenderer != null)
			spriteRenderer.enabled = false;

		if (tilemapRenderer != null)
			tilemapRenderer.enabled = false;

        // 如果设置了重新生成时间，则等待一段时间后重新启用碰撞器和渲染器
        if (respawnTime > 0f)
		{
			yield return new WaitForSeconds(respawnTime);

			if (platformCollider != null)
				platformCollider.enabled = true;
			if (spriteRenderer != null)
				spriteRenderer.enabled = true;
			if (tilemapRenderer != null)
				tilemapRenderer.enabled = true;
            isCrumbling = false;
			crumbleRoutine = null;
		}
	}

	/// <summary>
	/// 复位成「完好」状态：停掉碎裂协程、清掉抖动偏移、重新打开碰撞体和渲染器。
	/// 拉杆之类的开关重新启用本平台时会调用——只 SetActive(true) 的话，
	/// 平台会永远卡在「已粉碎」里（协程被失活打断，isCrumbling 没人置回 false）。
	/// </summary>
	public override void ResetPlatform()
	{
		if (crumbleRoutine != null)
		{
			StopCoroutine(crumbleRoutine);
			crumbleRoutine = null;
		}

		isCrumbling = false;

		// 抖动可能停在中途，位置要还原
		transform.localPosition -= new Vector3(shakeOffsetX, 0f, 0f);
		shakeOffsetX = 0f;

		if (platformCollider != null)
			platformCollider.enabled = true;
		if (spriteRenderer != null)
			spriteRenderer.enabled = true;
		if (tilemapRenderer != null)
			tilemapRenderer.enabled = true;
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
