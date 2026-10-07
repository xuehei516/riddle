using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 影子回放的「物理屏蔽」（场景级开关：场景里挂了本组件，影子就按下面的规则处理物理）。
///
/// 目标行为：
///   - 影子**和玩家保持实体碰撞**：玩家能站在影子上、被影子挡住
///     （这一条优先于 GhostReplaySystem 里的 ghostCollidesWithPlayer 开关）
///   - 影子**穿过其它一切实体几何**：墙、地板、门、平台……也就是可以穿墙
///   - 影子**照旧能触发压力板 / 触发区**（Trigger 一律不动），也照旧能按 E 拉拉杆
///   - 影子**质量极大**：玩家的推挤对它几乎没影响，水平路线不会被顶偏、也不会被撞得转起来
///   - 竖直方向默认**不下落**（verticalMode = HoldHeight）：影子保持在回放把它带到的高度
///
/// 实现方式（不改任何现有脚本）：定期扫描，用 PlayerController.IsGhost 认出影子，
/// 改它身上现成的 Rigidbody2D / Collider2D，并用 Physics2D.IgnoreCollision 逐对忽略
/// 「除玩家以外的一切实体碰撞体」——不动 Layer 碰撞矩阵，也不影响别的对象之间的碰撞。
/// </summary>
[AddComponentMenu("影子/影子物理屏蔽 (Ghost Physics Ignore)")]
[DisallowMultipleComponent]
public class GhostPhysicsIgnore : MonoBehaviour
{
	/// <summary>影子竖直方向怎么处理</summary>
	public enum VerticalMode
	{
		/// <summary>默认：不下落也不被顶飞，竖直速度每帧清零，影子保持在回放把它带到的高度</summary>
		HoldHeight,

		/// <summary>由本脚本补一份录制时的重力 + 向下探地：复刻下落 / 跳跃弧线，但仍不与场景几何碰撞</summary>
		SimulateGravity,

		/// <summary>不管竖直方向，完全交给物理（注意：重力关掉后回放里的跳跃会一直上升）</summary>
		Free
	}

	[Header("物理屏蔽")]
	[Tooltip("关掉影子的 Unity 重力（HoldHeight / Free 模式下必须开着；SimulateGravity 模式由脚本自己补重力）")]
	[SerializeField] private bool ignoreGravity = true;

	[Tooltip("忽略影子和「除玩家以外的一切实体碰撞体」的碰撞：可以穿墙。Trigger 不动，所以压力板 / 触发区照旧")]
	[SerializeField] private bool passThroughOthers = true;

	[Tooltip("保证影子和玩家的实体碰撞（玩家可以站在影子上）。会覆盖 GhostReplaySystem 里的 ghostCollidesWithPlayer 开关")]
	[SerializeField] private bool collideWithPlayer = true;

	[Header("质量（让玩家的推挤对影子影响微乎其微）")]
	[Tooltip("改影子的质量。质量远大于玩家时，玩家的碰撞几乎推不动它")]
	[SerializeField] private bool overrideMass = true;

	[Tooltip("影子要用的质量。玩家质量一般是 1 左右，这里给个几百到几万，推挤的影响就基本看不出来了")]
	[SerializeField, Min(0.0001f)] private float mass = 10000f;

	[Tooltip("锁死影子的旋转：被玩家撞到时不会转起来（影子自己不需要旋转）")]
	[SerializeField] private bool lockRotation = true;

	[Header("竖直方向")]
	[Tooltip("HoldHeight（默认）：不下落、不被顶飞，保持在回放把它带到的高度；SimulateGravity：脚本补重力 + 探地，复刻录制弧线；Free：完全交给物理")]
	[SerializeField] private VerticalMode verticalMode = VerticalMode.HoldHeight;

	[Tooltip("HoldHeight 用：刚生成时被重力拽下去的高度差小于这个值，才把它修正回出生高度（避免误修正）")]
	[SerializeField, Min(0f)] private float heightFixTolerance = 0.75f;

	[Tooltip("SimulateGravity 用：哪些层算「地面」")]
	[SerializeField] private LayerMask groundLayers = ~0;

	[Tooltip("SimulateGravity 用：从影子碰撞体底部往下探测的距离")]
	[SerializeField, Min(0.01f)] private float groundCheckDistance = 0.12f;

	[Header("扫描")]
	[Tooltip("已经找到影子后，每隔多久扫一次（新出现的碰撞体也会在这一步补上忽略）")]
	[SerializeField, Min(0.05f)] private float scanInterval = 0.25f;

	[Tooltip("还没找到影子时的查找间隔：影子随时可能被生成出来，所以查得勤一点")]
	[SerializeField, Min(0.01f)] private float probeInterval = 0.05f;

	[Tooltip("每隔多久把「忽略碰撞 / 质量 / 锁旋转」重挂一遍：碰撞体被禁用再启用时 Unity 会清掉 IgnoreCollision 的状态")]
	[SerializeField, Min(0.1f)] private float refreshInterval = 0.5f;

	/// <summary>一个已经在处理的影子</summary>
	private class GhostRig
	{
		public PlayerController controller;
		/// <summary>主刚体（根上的那个，竖直逻辑用它）</summary>
		public Rigidbody2D body;
		/// <summary>全部刚体（含子物体）</summary>
		public Rigidbody2D[] bodies;
		/// <summary>每个刚体原本的重力倍率（SimulateGravity 模式要按同样的数值补重力）</summary>
		public float[] gravityScales;
		public Collider2D[] colliders;
		/// <summary>已经忽略过的世界碰撞体，避免每次扫描重复调用</summary>
		public readonly HashSet<Collider2D> handled = new HashSet<Collider2D>();
	}

	private readonly List<GhostRig> ghosts = new List<GhostRig>();
	private readonly RaycastHit2D[] groundHits = new RaycastHit2D[1];

	private float nextScanTime;
	private float nextRefreshTime;

	private void Update()
	{
		if (Time.unscaledTime < nextScanTime) return;

		// 还没找到影子时查得勤一点，免得影子刚生成出来先自己掉一段
		nextScanTime = Time.unscaledTime + (ghosts.Count == 0 ? probeInterval : scanInterval);

		// 定期让忽略清单作废、整体重挂一遍（Unity 在碰撞体禁用 / 重新启用后会清掉忽略状态）
		if (Time.unscaledTime >= nextRefreshTime)
		{
			nextRefreshTime = Time.unscaledTime + refreshInterval;

			foreach (GhostRig ghost in ghosts)
			{
				ghost.handled.Clear();
				ApplyBodySettings(ghost);
			}
		}

		Scan();
	}

	private void FixedUpdate()
	{
		if (verticalMode == VerticalMode.Free || ghosts.Count == 0) return;

		float deltaTime = Time.fixedDeltaTime;

		foreach (GhostRig ghost in ghosts)
		{
			if (ghost.bodies == null) continue;

			if (verticalMode == VerticalMode.HoldHeight)
			{
				// 竖直方向完全不参与物理：不下落，也不会被回放里的跳跃 / 外部碰撞带飞
				for (int i = 0; i < ghost.bodies.Length; i++)
				{
					Rigidbody2D body = ghost.bodies[i];
					if (body == null) continue;

					body.velocity = new Vector2(body.velocity.x, 0f);
				}

				continue;
			}

			// SimulateGravity：脚本自己补一份录制时的重力，再向下探地把下落收住（效果像踩在实体地板上）
			for (int i = 0; i < ghost.bodies.Length; i++)
			{
				Rigidbody2D body = ghost.bodies[i];
				if (body == null || !ignoreGravity) continue;

				body.velocity += Physics2D.gravity * ghost.gravityScales[i] * deltaTime;
			}

			if (ghost.body != null && ghost.body.velocity.y <= 0f && IsOnGround(ghost))
			{
				ghost.body.velocity = new Vector2(ghost.body.velocity.x, 0f);
			}
		}
	}

	/// <summary>
	/// 扫描一次：认出玩家和所有影子，给影子补上「和玩家碰撞、穿其它几何」的设置
	/// </summary>
	private void Scan()
	{
		// 清掉已经不存在的影子
		for (int i = ghosts.Count - 1; i >= 0; i--)
		{
			if (ghosts[i].controller == null) ghosts.RemoveAt(i);
		}

		PlayerController player = null;
		PlayerController[] controllers = FindObjectsOfType<PlayerController>();

		foreach (PlayerController controller in controllers)
		{
			if (controller == null) continue;

			if (controller.IsGhost) Track(controller);
			else player = controller;   // 不带 IsGhost 标记的那个就是玩家
		}

		if (ghosts.Count == 0) return;

		// 影子 ↔ 玩家：保持实体碰撞
		if (collideWithPlayer && player != null)
		{
			foreach (GhostRig ghost in ghosts)
			{
				KeepPlayerCollision(ghost, player);
			}
		}

		// 影子 ↔ 其它一切实体几何：忽略碰撞（Trigger 不动）
		if (passThroughOthers)
		{
			Collider2D[] colliders = FindObjectsOfType<Collider2D>(true);

			foreach (GhostRig ghost in ghosts)
			{
				IgnoreWorldColliders(ghost, colliders, player);
			}
		}
	}

	/// <summary>
	/// 第一次见到这个影子：记下它原本的重力倍率、挂上刚体设置，并把「还没被接管之前」掉下去的高度修回来
	/// </summary>
	private void Track(PlayerController controller)
	{
		if (Find(controller) != null) return;

		Rigidbody2D[] bodies = controller.GetComponentsInChildren<Rigidbody2D>(true);
		float[] gravityScales = new float[bodies.Length];

		for (int i = 0; i < bodies.Length; i++)
		{
			gravityScales[i] = bodies[i] != null ? bodies[i].gravityScale : 1f;
		}

		GhostRig ghost = new GhostRig
		{
			controller = controller,
			body = bodies.Length > 0 ? bodies[0] : null,
			bodies = bodies,
			gravityScales = gravityScales,
			colliders = controller.GetComponentsInChildren<Collider2D>(true)
		};

		// 主刚体优先用根上的那个（假 null 不能用 ?? 判断，所以显式判空）
		Rigidbody2D rootBody = controller.GetComponent<Rigidbody2D>();
		if (rootBody != null)
		{
			ghost.body = rootBody;
		}

		ApplyBodySettings(ghost);
		FixHeightAfterSpawn(ghost);

		ghosts.Add(ghost);
	}

	/// <summary>
	/// 影子的刚体设置：关重力、改质量、锁旋转。
	/// 质量远大于玩家时，玩家的推挤对影子几乎不起作用（水平速度每帧由回放重写，位置也不会被顶偏）；
	/// 反过来影子对玩家依旧是实实在在的实体（能挡住 / 能站上去）。
	/// </summary>
	private void ApplyBodySettings(GhostRig ghost)
	{
		if (ghost.bodies == null) return;

		foreach (Rigidbody2D body in ghost.bodies)
		{
			if (body == null) continue;

			if (ignoreGravity)
			{
				body.gravityScale = 0f;
			}

			if (overrideMass)
			{
				body.useAutoMass = false;   // 开着的话 mass 会被碰撞体密度算出来，设了也没用
				body.mass = mass;
			}

			if (lockRotation)
			{
				body.freezeRotation = true;
				body.angularVelocity = 0f;
			}
		}
	}

	/// <summary>
	/// HoldHeight 模式：把影子刚生成、还没被本脚本接管时被重力拽下去的那一点高度修回来。
	/// 只动 Y、不动 X，所以不会抹掉这段时间里已经回放出来的水平位移
	/// </summary>
	private void FixHeightAfterSpawn(GhostRig ghost)
	{
		if (verticalMode != VerticalMode.HoldHeight || ghost.body == null) return;

		float spawnY = GhostReplayData.SpawnPosition.y;
		float offsetY = Mathf.Abs(ghost.body.position.y - spawnY);

		if (offsetY <= 0.0001f || offsetY > heightFixTolerance) return;

		ghost.body.position = new Vector2(ghost.body.position.x, spawnY);
		ghost.body.velocity = new Vector2(ghost.body.velocity.x, 0f);
	}

	/// <summary>
	/// 确保影子 ↔ 玩家之间是实体碰撞（GhostReplaySystem 里那个开关如果关掉了，这里会把它打开）
	/// </summary>
	private void KeepPlayerCollision(GhostRig ghost, PlayerController player)
	{
		Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>(true);

		foreach (Collider2D ghostCollider in ghost.colliders)
		{
			if (ghostCollider == null) continue;

			foreach (Collider2D playerCollider in playerColliders)
			{
				if (playerCollider == null) continue;

				Physics2D.IgnoreCollision(ghostCollider, playerCollider, false);
			}
		}
	}

	/// <summary>
	/// 把影子和「除玩家以外的一切实体碰撞体」的碰撞关掉；Trigger 保持原样
	/// </summary>
	private void IgnoreWorldColliders(GhostRig ghost, Collider2D[] worldColliders, PlayerController player)
	{
		foreach (Collider2D other in worldColliders)
		{
			if (other == null) continue;
			if (other.isTrigger) continue;                              // 触发器不动：压力板 / 触发区照旧
			if (ghost.handled.Contains(other)) continue;                 // 这个已经处理过
			if (IsPartOf(other, ghost.controller)) continue;             // 影子自己
			if (player != null && IsPartOf(other, player)) continue;     // 玩家单独处理，保持碰撞

			foreach (Collider2D ghostCollider in ghost.colliders)
			{
				if (ghostCollider == null) continue;

				Physics2D.IgnoreCollision(ghostCollider, other, true);
			}

			ghost.handled.Add(other);
		}
	}

	/// <summary>
	/// SimulateGravity 用：从影子所有碰撞体的最低点往下打一条射线，看下面有没有「地面」。
	/// 只认实体碰撞体（忽略 Trigger），并排除影子自己所在的层，免得打到自己
	/// </summary>
	private bool IsOnGround(GhostRig ghost)
	{
		if (ghost.controller == null || ghost.colliders == null || ghost.colliders.Length == 0) return false;

		float bottom = float.MaxValue;
		float centerX = ghost.controller.transform.position.x;

		foreach (Collider2D collider in ghost.colliders)
		{
			if (collider == null || !collider.enabled) continue;

			Bounds bounds = collider.bounds;
			if (bounds.min.y < bottom)
			{
				bottom = bounds.min.y;
				centerX = bounds.center.x;
			}
		}

		if (bottom == float.MaxValue) return false;

		int ghostLayer = ghost.controller.gameObject.layer;

		ContactFilter2D filter = new ContactFilter2D();
		filter.useTriggers = false;
		filter.SetLayerMask(groundLayers & ~(1 << ghostLayer));

		// 起点抬一点点，确保落在影子自己的碰撞体内部
		Vector2 origin = new Vector2(centerX, bottom + 0.02f);

		return Physics2D.Raycast(origin, Vector2.down, filter, groundHits, groundCheckDistance) > 0;
	}

	/// <summary>这个碰撞体是不是属于某个对象（含它的子物体）</summary>
	private static bool IsPartOf(Collider2D collider, Component owner)
	{
		return collider != null && owner != null && collider.transform.IsChildOf(owner.transform);
	}

	/// <summary>找出已经在处理的那个影子的记录</summary>
	private GhostRig Find(PlayerController controller)
	{
		foreach (GhostRig ghost in ghosts)
		{
			if (ghost.controller == controller) return ghost;
		}

		return null;
	}
}
