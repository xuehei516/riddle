using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 复活点：玩家靠近自动激活（同一时间只有一个），死亡后复活到「本点位置 + respawnOffset」。
/// 全程订阅玩家的 DestroyedEvent.OnPlayerDeath，没有轮询；音效 / 特效 / UI 接 onActivated、onRespawned。
/// 场景配置：空物体 + 本脚本 + 勾了 Is Trigger 的 Collider2D（范围画大＝靠近就激活）。
/// </summary>
[AddComponentMenu("机关/复活点 (Respawn Point)")]
[DisallowMultipleComponent]
public class RespawnPoint : MonoBehaviour
{
	[SerializeField] private Vector2 respawnOffset = new Vector2(0f, 0.5f); // 复活位置偏移
	[SerializeField, Min(0f)] private float respawnDelay = 1f;              // 死亡后多久复活（秒）
	[SerializeField] private bool restorePlayerOnRespawn = true;            // 复活时回满血、清掉死亡标记
	[SerializeField] private Sprite activatedSprite, inactiveSprite;        // 激活时 / 未激活时的图（可只配一张）
	[SerializeField] private UnityEvent onActivated, onRespawned;           // 激活时 / 复活完成时（音效 / 特效 / UI）

	public static RespawnPoint ActivePoint { get; private set; }                           // 当前生效的复活点
	public static event System.Action<RespawnPoint> OnPointActivated, OnPointDeactivated; // 代码订阅用
	public bool IsActivated => ActivePoint == this;
	public Vector3 RespawnPosition => transform.position + (Vector3)respawnOffset;
	private Player_Health playerHealth;      // 当前订阅了死亡事件的玩家
	private SpriteRenderer spriteRenderer;   // 换激活 / 未激活图用
	private void OnDisable() => Release();

	// 玩家靠近 → 自动激活；影子是克隆玩家生成的，同样带 Player_Health，别让它抢复活点
	private void OnTriggerEnter2D(Collider2D other)
	{
		Player_Health player = other != null ? other.GetComponentInParent<Player_Health>() : null;
		PlayerController controller = player != null ? player.GetComponent<PlayerController>() : null;
		if (player == null || (controller != null && controller.IsGhost)) return;
		Activate(player);
	}

	/// <summary>激活本点：顶掉当前生效的那个，并订阅玩家的死亡事件</summary>
	public void Activate(Player_Health player)
	{
		if (player == null || player.IsDead) return;
		if (ActivePoint != null && ActivePoint != this) ActivePoint.Deactivate();
		ActivePoint = this;
		if (playerHealth != player)
		{
			if (playerHealth != null) playerHealth.GetComponent<DestroyedEvent>().OnPlayerDeath -= HandlePlayerDeath;
			playerHealth = player;
			player.GetComponent<DestroyedEvent>().OnPlayerDeath += HandlePlayerDeath;
		}
		RefreshSprite();
		OnPointActivated?.Invoke(this);
		if (onActivated != null) onActivated.Invoke();
	}

	/// <summary>本点下台（被别的点顶替，或手动调用）</summary>
	public void Deactivate()
	{
		if (ActivePoint != this) return;
		Release();
		OnPointDeactivated?.Invoke(this);
	}

	/// <summary>退订死亡事件 + 取消待执行的复活 + 让出「当前复活点」+ 换回未激活的图</summary>
	private void Release()
	{
		if (playerHealth != null) playerHealth.GetComponent<DestroyedEvent>().OnPlayerDeath -= HandlePlayerDeath;
		playerHealth = null;
		CancelInvoke(nameof(RespawnNow));
		if (ActivePoint == this) ActivePoint = null;
		RefreshSprite();
	}

	// 玩家死亡 → 等 respawnDelay 秒再复活
	private void HandlePlayerDeath(DestroyedEvent destroyedEvent, DestroyedEventArgs destroyedEventArgs)
	{
		if (IsActivated) Invoke(nameof(RespawnNow), respawnDelay);
	}

	/// <summary>按当前激活状态换图（哪张没配就跳过，不会把贴图清空）</summary>
	private void RefreshSprite()
	{
		if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		Sprite sprite = IsActivated ? activatedSprite : inactiveSprite;
		if (spriteRenderer != null && sprite != null) spriteRenderer.sprite = sprite;
	}

	/// <summary>立刻复活到本点（掉落死亡区也可以直接调 ActivePoint.RespawnNow()）</summary>
	public void RespawnNow()
	{
		CancelInvoke(nameof(RespawnNow));
		if (playerHealth == null) return;
		GameObject player = playerHealth.gameObject;
		player.SetActive(true);                             // 死亡时 Destroyed 把玩家整个 SetActive(false) 了
		if (restorePlayerOnRespawn) playerHealth.Revive();   // 清掉死亡标记 + 回满血
		player.transform.position = RespawnPosition;
		Rigidbody2D body = player.GetComponent<Rigidbody2D>();
		if (body != null) { body.velocity = Vector2.zero; body.position = RespawnPosition; } // 刚体要用 position 才立刻同步
		if (onRespawned != null) onRespawned.Invoke();
	}
}
