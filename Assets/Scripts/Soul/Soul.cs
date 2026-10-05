using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SoulPlatform : MonoBehaviour
{
	[Header("灵魂参数")]
	[Tooltip("向上匀速运动的速度")]
	[SerializeField] private float riseSpeed = 2f;

	[Tooltip("存在的最长时间（秒）")]
	[SerializeField] private float lifeTime = 10f;

	[Header("边界保护")]
	[Tooltip("天花板所在的层")]
	[SerializeField] private LayerMask ceilingLayer;

	[Tooltip("从玩家头顶中心向上探测天花板的距离")]
	[SerializeField] private float ceilingCheckDistance = 0.2f;

	[Header("消散视觉特效（可选）")]
    [Tooltip("消散粒子预制体")]
	[SerializeField] private GameObject dissolveVfxPrefab;

	private Rigidbody2D rigidBody2D;
	private Collider2D soulCollider;

	private void Awake()
	{
		rigidBody2D = GetComponent<Rigidbody2D>();
		soulCollider = GetComponent<Collider2D>();
	}

	private void Start()
	{
		rigidBody2D.velocity = new Vector2(0, riseSpeed);

		Invoke(nameof(Disappear), lifeTime);
	}

	private void FixedUpdate()
	{
		rigidBody2D.velocity = new Vector2(0, riseSpeed);
	}

	// 当角色踩在灵魂上被顶着走时，每物理帧检测玩家头顶是否触碰天花板
	private void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			// 确保玩家是在灵魂的上方
			if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
			{
				Collider2D playerCollider = collision.collider;

				// 从玩家头顶中心，向上发射一条极短的射线检测天花板
				Vector2 rayOrigin = new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.max.y);
				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, ceilingCheckDistance, ceilingLayer);

				// 如果玩家头顶顶到了天花板
				if (hit.collider != null)
				{
					Disappear();
				}
			}
		}
	}

	// 灵魂消散逻辑
	private void Disappear()
	{
		if (dissolveVfxPrefab != null)
		{
			Instantiate(dissolveVfxPrefab, transform.position, Quaternion.identity);
		}

		Destroy(gameObject);
	}
}