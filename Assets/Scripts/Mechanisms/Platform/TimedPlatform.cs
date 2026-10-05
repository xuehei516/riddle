using System.Collections;
using UnityEngine;

/// <summary>
/// 限时平台
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TimedPlatform : MovingPlatform
{
	public enum ActivationMode
	{
		OnStep,     // 踩上后开始计时
		Periodic    // 平台会周期性地出现和消失
	}

	[Header("限时设置")]
	[SerializeField] private ActivationMode activationMode = ActivationMode.OnStep;
	[Tooltip("平台保持可见的时间")]
	[SerializeField] private float activeTime = 4f;
	[Tooltip("消失后重新出现的时间；设为 0 则不重生")]
	[SerializeField] private float respawnTime = 2f;
	[Tooltip("倒计时期间的平台抖动幅度")]
	[SerializeField] private float shakeAmount = 0.03f;

	[Header("组件引用")]
	[SerializeField] private Collider2D platformCollider;
	[SerializeField] private SpriteRenderer spriteRenderer;

	private bool isRunning;
	private void Start()
	{
		if (activationMode == ActivationMode.Periodic)
			BeginTimer();
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();

		if (isRunning)
			VelocityX = 0f;
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (activationMode == ActivationMode.OnStep && !isRunning && (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Ghost")) &&
			collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
		{
			BeginTimer();
		}
	}

	private void BeginTimer()
	{
		if (!isRunning)
			StartCoroutine(TimerRoutine());
	}

	private IEnumerator TimerRoutine()
	{
		isRunning = true;

		float time = 0f;
		while (time < activeTime)
		{
			time += Time.deltaTime;

			transform.localPosition = transform.localPosition + new Vector3(Random.Range(-shakeAmount, shakeAmount), 0f, 0f);
			
			yield return null;
		}

		if (platformCollider != null)
			platformCollider.enabled = false;
		if (spriteRenderer != null)
			spriteRenderer.enabled = false;

		if (respawnTime > 0f)
		{
			yield return new WaitForSeconds(respawnTime);

			if (platformCollider != null)
				platformCollider.enabled = true;
			if (spriteRenderer != null)
				spriteRenderer.enabled = true;

			isRunning = false;

			if (activationMode == ActivationMode.Periodic)
			{
				yield return new WaitForSeconds(activeTime);
				BeginTimer();
			}
		}
	}
}
