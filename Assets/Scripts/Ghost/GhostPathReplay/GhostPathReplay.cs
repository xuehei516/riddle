using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 影子的「逐帧位置复现」（不改动任何现有脚本）。
///
/// 为什么需要它：原来的回放只录输入（InputFrame：左右 / 按下跳 / 松开跳 / 交互），
/// 跳跃和下落是靠物理**重算**的。所以一旦为了「不受物理影响」把重力 / 碰撞摘掉，
/// 影子就不会跳了——弧线无从算起。改成录制**每一物理帧的位置**、回放时照抄，
/// 跳跃、下落、抛物线就都和录制时完全一致，同时和场景物理彻底解耦。
///
/// 录制：GhostReplayData.IsRecording 为真时（也就是按 R 开始录的那段时间），
///       每个物理帧记下玩家（不带 IsGhost 标记的那个 PlayerController）的位置和 localScale。
///       新一轮录制开始时清空旧轨迹；数据是 static，跨场景保留。
///
/// 复现：场景里出现影子（PlayerController.IsGhost）后，每帧把它的位置摆到对应帧；
///       位置在 LateUpdate 里写，所以物理怎么算都不影响最终位置。轨迹播完就停在最后一帧。
///
/// 建议和 GhostPhysicsIgnore 一起用：那个负责穿墙 / 和玩家保持实体碰撞 / 质量极大，
/// 这个负责让影子严格走在录下来的轨迹上。
/// </summary>
[AddComponentMenu("影子/影子轨迹复现 (Ghost Path Replay)")]
[DisallowMultipleComponent]
public class GhostPathReplay : MonoBehaviour
{
	/// <summary>录下来的一帧</summary>
	private struct PathFrame
	{
		public Vector3 position;
		public Vector3 localScale;
	}

	[Header("录制")]
	[Tooltip("跟着 GhostReplayData.IsRecording 自动录制玩家位置")]
	[SerializeField] private bool autoRecord = true;

	[Tooltip("最多录多少帧。物理帧 50/秒时，5000 帧约 100 秒")]
	[SerializeField, Min(10)] private int maxFrames = 5000;

	[Header("复现")]
	[Tooltip("连朝向（localScale）一起复现")]
	[SerializeField] private bool applyScale = true;

	/// <summary>整条轨迹（static：跨场景保留，和 GhostReplayData 一样的思路）</summary>
	private static readonly List<PathFrame> path = new List<PathFrame>();
	/// <summary>上一帧是不是在录制（用来抓「新一轮录制开始」这个瞬间）</summary>
	private static bool wasRecording;

	/// <summary>玩家（录制对象）</summary>
	private PlayerController playerController;
	/// <summary>影子（复现对象）</summary>
	private PlayerController ghostController;
	/// <summary>影子当前播到第几帧</summary>
	private int playbackIndex;

	#region 录制
	private void FixedUpdate()
	{
		if (autoRecord) Record();

		if (ghostController == null)
		{
			// 影子已经播完上一段、被销毁了 → 清掉，方便下一次回放
			playbackIndex = 0;
			FindGhost();
			return;   // 刚找到影子这一帧先不推进，让 LateUpdate 先把第 0 帧摆上
		}

		// 必须按物理帧推进：LateUpdate 是渲染帧，帧率通常高于物理帧率，在那里推进会跑快
		if (path.Count > 0 && playbackIndex < path.Count - 1) playbackIndex++;
	}

	/// <summary>
	/// 录制：只在 GhostReplayData.IsRecording 为真时记帧，每个物理帧记一次
	/// </summary>
	private void Record()
	{
		bool recording = GhostReplayData.IsRecording;

		// 新一轮录制开始：清掉上一段轨迹
		if (recording && !wasRecording) path.Clear();

		wasRecording = recording;

		if (!recording || path.Count >= maxFrames) return;

		PlayerController player = GetPlayer();
		if (player == null) return;

		Rigidbody2D body = player.GetComponent<Rigidbody2D>();
		Vector3 position = body != null ? (Vector3)body.position : player.transform.position;

		path.Add(new PathFrame
		{
			position = position,
			localScale = player.transform.localScale
		});
	}

	/// <summary>拿场景里的玩家（不带 IsGhost 标记的那个）</summary>
	private PlayerController GetPlayer()
	{
		if (playerController != null) return playerController;

		foreach (PlayerController controller in FindObjectsOfType<PlayerController>())
		{
			if (controller == null || controller.IsGhost) continue;

			playerController = controller;
			break;
		}

		return playerController;
	}
	#endregion

	#region 复现
	/// <summary>找出场景里的影子（运行时克隆出来的，只能定期找）</summary>
	private void FindGhost()
	{
		foreach (PlayerController controller in FindObjectsOfType<PlayerController>())
		{
			if (controller == null || !controller.IsGhost) continue;

			ghostController = controller;
			playbackIndex = 0;
			return;
		}
	}

	/// <summary>
	/// 把影子摆到当前帧的位置。放在 LateUpdate：物理步进之后再写，位置就是最终结果
	/// </summary>
	private void LateUpdate()
	{
		if (ghostController == null || path.Count == 0) return;

		int index = Mathf.Clamp(playbackIndex, 0, path.Count - 1);
		PathFrame frame = path[index];

		Rigidbody2D body = ghostController.GetComponent<Rigidbody2D>();
		if (body != null)
		{
			body.position = frame.position;
		}

		ghostController.transform.position = frame.position;

		if (applyScale)
		{
			ghostController.transform.localScale = frame.localScale;
		}
	}
	#endregion
}
