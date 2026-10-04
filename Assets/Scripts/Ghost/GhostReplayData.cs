using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个物理帧（FixedUpdate）里的玩家输入快照。
/// 只存"意图"不存坐标：移动、跳跃和拉杆交互输入。
/// </summary>
public struct InputFrame
{
	public float move;
	public bool jumpDown;
	public bool jumpUp;
	public bool interactDown;
}

/// <summary>
/// 跨场景的录制数据仓库。
/// 用 static 存储，所以 SceneManager.LoadScene 之后数据依然在（不会被场景卸载清掉）。
/// </summary>
public static class GhostReplayData
{
	/// <summary>录到的输入帧，按物理帧顺序排列</summary>
	public static readonly List<InputFrame> Frames = new List<InputFrame>();

	/// <summary>影子出生位置 = 按下 R 开始录制那一刻玩家的位置</summary>
	public static Vector3 SpawnPosition;

	/// <summary>是否正在录制（PlayerController 每物理帧检查这个标记来决定要不要记一帧）</summary>
	public static bool IsRecording;

	/// <summary>手上是否已经有一段录好的数据</summary>
	public static bool HasRecording;

	/// <summary>场景重载后是否需要生成影子并回放</summary>
	public static bool PendingReplay;

	/// <summary>清空所有数据（开始新一轮录制时调用）</summary>
	public static void Reset()
	{
		Frames.Clear();
		IsRecording = false;
		HasRecording = false;
		PendingReplay = false;
	}
}
