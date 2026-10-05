using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 复活点。三件事：
///
///   1) 玩家碰到触发器 → 自动激活本点；同一时间只有一个复活点生效（新的把旧的顶下去）
///   2) 玩家死亡 → 延迟 respawnDelay 秒，复活到「本点位置 + respawnOffset」
///   3) 音效 / 特效 / UI → 接 onActivated、onRespawned，或代码订阅 OnPointActivated
///
/// 实现方式：激活时订阅 Player_Health.OnPlayerDeath，下台时退订，全程没有轮询。
/// 场景配置：空物体 + 本脚本 + 一个勾了 Is Trigger 的 Collider2D（范围画大＝靠近就激活）。
/// </summary>
[AddComponentMenu("机关/复活点 (Respawn Point)")]
[DisallowMultipleComponent]
public class RespawnPoint : MonoBehaviour
{
	[Header("复活点")]
	[Tooltip("复活位置相对本物体的偏移，避免复活后卡进地面")]
	[SerializeField] private Vector2 respawnOffset = new Vector2(0f, 0.5f);

	[Tooltip("死亡后停留多久再复活（秒）")]
	[SerializeField, Min(0f)] private float respawnDelay = 1f;

	[Tooltip("复活时回满血、清除死亡状态、重新打开玩家输入")]
	[SerializeField] private bool restorePlayerOnRespawn = true;

	[Header("音效 / 特效 / UI 接口")]
	[Tooltip("本点被玩家激活时触发")]
	[SerializeField] private UnityEvent onActivated;

	[Tooltip("玩家在本点复活完成时触发")]
	[SerializeField] private UnityEvent onRespawned;

	/// <summary>当前生效的复活点，同一时间只有一个；还没有人踩过时为 null</summary>
	public static RespawnPoint ActivePoint { get; private set; }

	/// <summary>某个点被激活 / 下台时触发（代码里订阅，不用互相拖引用）</summary>
	public static event System.Action<RespawnPoint> OnPointActivated;
	public static event System.Action<RespawnPoint> OnPointDeactivated;

	/// <summary>本点是不是当前生效的复活点</summary>
	public bool IsActivated => ActivePoint == this;

	/// <summary>玩家复活时出现的位置</summary>
	public Vector3 RespawnPosition => transform.position + (Vector3)respawnOffset;

	/// <summary>当前订阅了死亡事件的玩家</summary>
	private Player_Health playerHealth;


	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other == null) return;

		// 碰撞体可能挂在子物体上，所以要往父级找生命组件
		Player_Health player = other.GetComponentInParent<Player_Health>();
		if (player == null) return;

		// 幽灵回放是克隆玩家对象生成的，影子身上也有 Player_Health，别让它抢走复活点
		if (IsGhost(player)) return;

		Activate(player);
	}

	/// <summary>激活本点：把当前生效的那个顶下去，然后订阅玩家死亡事件</summary>
	public void Activate(Player_Health player)
	{
		if (player == null || player.isDead) return;

		// 同一时间只能有一个复活点生效
		if (ActivePoint != null && ActivePoint != this)
		{
			ActivePoint.Deactivate();
		}
		ActivePoint = this;

		BindPlayer(player);

		Debug.Log($"[复活点] 「{name}」已激活，复活位置 {RespawnPosition}。");
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

	/// <summary>收到死亡事件：等 respawnDelay 秒，再把人拉回复活点</summary>
	private void HandlePlayerDeath()
	{
		if (!IsActivated) return;

		Invoke(nameof(RespawnNow), respawnDelay);
	}

	/// <summary>立刻把玩家拉回本点复活（掉落死亡区也可以直接调 ActivePoint.RespawnNow()）</summary>
	public void RespawnNow()
	{
		if (playerHealth == null) return;

		// 死亡时 Player_Health 设了 isDead、关了输入，这里一并还原
		if (restorePlayerOnRespawn)
		{
			playerHealth.Revive();
		}

		MovePlayerToPoint(playerHealth.gameObject);

		Debug.Log($"[复活点] 玩家已在「{name}」复活。");
		if (onRespawned != null) onRespawned.Invoke();
	}

	private void OnDisable() => Release();

	/// <summary>订阅玩家死亡事件（换玩家时自动退订上一个）</summary>
	private void BindPlayer(Player_Health player)
	{
		if (playerHealth == player) return;

		UnbindPlayer();
		playerHealth = player;
		playerHealth.OnPlayerDeath += HandlePlayerDeath;
	}

	/// <summary>退订死亡事件</summary>
	private void UnbindPlayer()
	{
		if (playerHealth != null)
		{
			playerHealth.OnPlayerDeath -= HandlePlayerDeath;
		}
		playerHealth = null;
	}

	/// <summary>退订 + 取消待执行的复活 + 让出「当前复活点」的位置</summary>
	private void Release()
	{
		UnbindPlayer();

		CancelInvoke(nameof(RespawnNow));
		if (ActivePoint == this) ActivePoint = null;
	}


	/// <summary>把玩家挪到复活点：同步刚体位置、清掉下落速度、恢复输入</summary>
	private void MovePlayerToPoint(GameObject target)
	{
		target.transform.position = RespawnPosition;

		Rigidbody2D body = target.GetComponent<Rigidbody2D>();
		if (body != null)
		{
			body.velocity = Vector2.zero;

			// 刚体要用 position 赋值才会立刻同步，否则可能被物理系统拽回死亡前的位置
			body.position = RespawnPosition;
		}

		if (restorePlayerOnRespawn)
		{
			PlayerInput input = target.GetComponent<PlayerInput>();
			if (input != null) input.enabled = true;
		}
	}

	/// <summary>幽灵回放是克隆玩家对象生成的，影子身上也带 Player_Health</summary>
	private static bool IsGhost(Player_Health player)
	{
		PlayerController controller = player.GetComponent<PlayerController>();
		return controller != null && controller.IsGhost;
	}
}
