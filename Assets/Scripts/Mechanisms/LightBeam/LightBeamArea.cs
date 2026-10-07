using UnityEngine;
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class LightBeamArea : MonoBehaviour
{
	[Header("光束参数")]
	[Tooltip("光束射程")]
	[SerializeField] private float maxDistance = 20f;
	[Tooltip("光束宽度")]
	[SerializeField] private float beamWidth = 2f;

	[Tooltip("射线采样密集度")]
	[Range(5, 50)]
	[SerializeField] private int rayCount = 25;

	[Tooltip("会被阻挡的障碍物层")]
	[SerializeField] private LayerMask obstacleLayer;

	[Header("灵魂预制体")]
	[SerializeField] private GameObject soulPrefab;

	private Mesh mesh;
	private MeshFilter meshFilter;
	private PolygonCollider2D polyCollider;
	private MeshRenderer meshRenderer;

	// 缓存数组
	/// <summary>
	/// 保存 Mesh 顶点位置
	/// </summary>
	private Vector3[] vertices;
	/// <summary>
	/// 保存每个顶点的 UV 坐标
	/// </summary>
	private Vector2[] uvs;
	/// <summary>
	/// 构成 Mesh 的三角形索引数组
	/// </summary>
	private int[] triangles;
	/// <summary>
	/// 保存 PolygonCollider2D 的轮廓点
	/// </summary>
	private Vector2[] colliderPath;

	private void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		polyCollider = GetComponent<PolygonCollider2D>();
		meshRenderer = GetComponent<MeshRenderer>();

		meshRenderer.sortingOrder = 5;
		mesh = new Mesh();
		mesh.name = "DynamicLightBeamMesh";
		meshFilter.mesh = mesh;

		// 初始化网格和顶点缓存
		InitBuffers();
	}

	/// <summary>
	/// 根据 rayCount 预先创建 Mesh 和 PolygonCollider2D 每帧要用的数据数组，并提前生成三角形索引
	/// </summary>
	private void InitBuffers()
	{
		// 每个射线有两个顶点，一个在光源处，一个在射线终点处，顶点总数为 rayCount * 2
		int vertCount = rayCount * 2;
		vertices = new Vector3[vertCount];
		uvs = new Vector2[vertCount];
		triangles = new int[(rayCount - 1) * 6];
		colliderPath = new Vector2[vertCount];

		// 预生成三角形拓扑结构，每两个相邻的射线形成两个三角形，组成一个四边形
		int t = 0;
		for (int i = 0; i < rayCount - 1; i++)
		{
			int topL = i * 2;
			int botL = topL + 1;
			int topR = topL + 2;
			int botR = topL + 3;

			triangles[t++] = topL;
			triangles[t++] = topR;
			triangles[t++] = botL;

			triangles[t++] = topR;
			triangles[t++] = botR;
			triangles[t++] = botL;
		}
	}

	private void Update()
	{
		GenerateLightMesh();

		// 网格刷新完再判影子，用的是本帧最新的光束形状
		DestroyGhostInsideBeam();
	}

	/// <summary>
	/// 每帧调用，沿光束宽度方向发射多条射线，检测障碍物并生成动态网格和碰撞框
	/// </summary>
	private void GenerateLightMesh()
	{
		Vector2 origin = transform.position;
		Vector2 shootDir = transform.right; // 向着本地 x 轴正方向发射
		Vector2 widthDir = transform.up;    // 垂直于发射方向的宽度轴

		for (int i = 0; i < rayCount; i++)
		{
			// 计算当前射线在光束宽度上的偏移百分比，范围从 0 到 1
			float percent = (float)i / (rayCount - 1);
			// 从起点向两边沿宽度偏移
			float offset = (percent - 0.5f) * beamWidth;
			Vector2 rayStart = origin + widthDir * offset;

			// 单独向前方发射射线检测障碍物
			RaycastHit2D hit = Physics2D.Raycast(rayStart, shootDir, maxDistance, obstacleLayer);
			Vector2 rayEnd = hit.collider != null ? hit.point : rayStart + shootDir * maxDistance;

			// 转换到局部坐标系供 Mesh 和 PolygonCollider 使用
			int topIdx = i * 2;
			int botIdx = topIdx + 1;

			vertices[topIdx] = transform.InverseTransformPoint(rayStart);
			vertices[botIdx] = transform.InverseTransformPoint(rayEnd);

			// UV 坐标映射
			float hitLength = hit.collider != null ? hit.distance : maxDistance;
			uvs[topIdx] = new Vector2(0f, percent);
			uvs[botIdx] = new Vector2(hitLength / maxDistance, percent);

			// 记录碰撞框多边形边界
			colliderPath[i] = vertices[topIdx];
			colliderPath[colliderPath.Length - 1 - i] = vertices[botIdx];
		}

		// 刷新渲染网格
		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();

		// 动态刷新多边形触发器碰撞框
		polyCollider.SetPath(0, colliderPath);
	}

	/// <summary>
	/// 判断光束接触到的物体类型，如果是影子则销毁，如果是玩家则设置玩家在光照区域内的状态
	/// </summary>
	/// <param name="collision"></param>
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (IsGhost(collision))
		{
			// 影子本体挂在哪一层子物体上不一定，统一销毁带 PlayerController 的那个
			DestroyGhost(collision);
			return;
		}

		if (collision.CompareTag("Player"))
		{
			if (collision.TryGetComponent<Player>(out var player))
			{
				player.currentLightSource = this;
				player.isInLightZone = true;
			}
		}
	}

	/// <summary>
	/// 玩家离开光束区域时，更新玩家在光照区域内的状态
	/// </summary>
	/// <param name="collision"></param>
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.CompareTag("Player"))
		{
			if (collision.TryGetComponent<Player>(out var player))
			{
				if (player.currentLightSource == this)
				{
					player.currentLightSource = null;
				}
				player.isInLightZone = false;
			}
		}
	}
	
	/// <summary>
	/// 这个碰撞体是不是幽灵影子。
	/// 不只认 "Ghost" 标签：影子出生时被设成了 Untagged（防止 FindWithTag("Player") 抓错对象），
	/// 所以直接认 PlayerController.IsGhost 更可靠。
	/// </summary>
	private static bool IsGhost(Collider2D collision)
	{
		if (collision == null) return false;
		if (collision.CompareTag("Ghost")) return true;

		PlayerController controller = collision.GetComponentInParent<PlayerController>();
		return controller != null && controller.IsGhost;
	}

	/// <summary>销毁这个碰撞体所属的影子（优先销毁带 PlayerController 的那个根物体）</summary>
	private static void DestroyGhost(Collider2D collision)
	{
		if (collision == null) return;

		PlayerController controller = collision.GetComponentInParent<PlayerController>();
		Destroy(controller != null ? controller.gameObject : collision.gameObject);
	}

	/// <summary>
	/// 影子进入光束就销毁。
	/// 不依赖 OnTriggerEnter2D：层矩阵里 LightBeam 与 Ghost 的交叉格可能是关的，
	/// 那样触发器根本不会响；这里用纯几何判定，绕开层矩阵与 Tag。
	/// </summary>
	private void DestroyGhostInsideBeam()
	{
		GameObject ghost = GhostReplaySystem.ActiveGhost;
		if (ghost == null) return;

		Collider2D ghostCollider = ghost.GetComponentInChildren<Collider2D>();
		if (ghostCollider == null || !ghostCollider.enabled) return;

		// 两个判定取并集：实体相交（准）或视觉中点在光束多边形内（Distance 对触发器不适用时兜底）
		bool touched = polyCollider.Distance(ghostCollider).isOverlapped
		               || polyCollider.OverlapPoint(ghostCollider.bounds.center);

		if (touched) Destroy(ghost);
	}

	/// <summary>
	/// 在指定位置生成灵魂预制体
	/// </summary>
	/// <param name="deathPos">灵魂生成位置</param>
	public void SpawnSoulAtPosition(Vector2 deathPos)
	{
		if (soulPrefab != null)
		{
			Instantiate(soulPrefab, deathPos, Quaternion.identity);
		}
	}
}