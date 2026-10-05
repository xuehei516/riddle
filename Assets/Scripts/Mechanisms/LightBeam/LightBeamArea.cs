using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LightBeamArea : MonoBehaviour
{
	[Header("光束参数")]
	[Tooltip("光束的最大照射距离")]
	[SerializeField] private float maxDistance = 20f;

	[Tooltip("光束的宽度")]
	[SerializeField] private float beamWidth = 1.5f;

	[Tooltip("会被阻挡的障碍物层（通常是 Ground / Wall 层）")]
	[SerializeField] private LayerMask obstacleLayer;

	[Header("灵魂预制体")]
	[SerializeField] private GameObject soulPrefab;

	[Header("光束视觉效果")]
	[SerializeField] private SpriteRenderer beamSpriteRenderer;

	private BoxCollider2D beamCollider;
	
	private void Awake()
	{
		beamCollider = GetComponent<BoxCollider2D>();
	}

	private void Update()
	{
		UpdateLightBeam();
	}

	// 动态发射射线，计算光束受阻长度
	private void UpdateLightBeam()
	{
		Vector2 origin = transform.position;
		Vector2 direction = transform.right; // 默认朝向物体的 X 轴正方向，旋转物体即可改变光照方向

		// 向前发射一个宽度为 beamWidth 的盒子射线
		RaycastHit2D hit = Physics2D.BoxCast(origin, new Vector2(0.1f, beamWidth), transform.eulerAngles.z, direction, maxDistance, obstacleLayer);

		float currentDistance = maxDistance;
		if (hit.collider != null)
		{
			// 被障碍物挡住，取实际距离
			currentDistance = hit.distance;
		}

		// 动态调节触发器碰撞体大小与中心点
		beamCollider.size = new Vector2(currentDistance, beamWidth);
		beamCollider.offset = new Vector2(currentDistance * 0.5f, 0f);

		// 动态调节视觉贴图大小
		if (beamSpriteRenderer != null)
		{
			// 确保 Sprite 的 Draw Mode 设为 Tiled 或 Sliced
			beamSpriteRenderer.size = new Vector2(currentDistance, beamWidth);
			beamSpriteRenderer.transform.localPosition = new Vector2(currentDistance * 0.5f, 0f);
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		// 处于光区域的影子直接被销毁
		if (collision.CompareTag("Ghost"))
		{
			Debug.Log("【光机制】影子误入光区，被彻底消融！");
			Destroy(collision.gameObject);
		}

		// 记录玩家进入了光区
		if (collision.CompareTag("Player"))
		{
			if (collision.TryGetComponent<Player>(out var player))
			{
				player.isInLightZone = true;
				player.currentLightSource = this; // 绑定当前光源
			}
		}
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		// 玩家离开光区
		if (collision.CompareTag("Player"))
		{
			if (collision.TryGetComponent<Player>(out var player))
			{
				player.isInLightZone = false;
				player.currentLightSource = null;
			}
		}
	}

	// 当玩家在光中死亡时，由玩家死亡脚本调用生成“灵魂”
	public void SpawnSoulAtPosition(Vector2 deathPos)
	{
		if (soulPrefab != null)
		{
			Instantiate(soulPrefab, deathPos, Quaternion.identity);
			Debug.Log("【光机制】玩家在光区死亡，生成上升灵魂！");
		}
	}
}