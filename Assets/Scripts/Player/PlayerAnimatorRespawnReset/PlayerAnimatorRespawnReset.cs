using UnityEngine;

/// <summary>
/// 复活后把角色 Animator 从「死亡状态」里拉回来。
///
/// 为什么会卡在死亡姿势：
///   - 死亡时角色被打进死亡状态，随后 Destroyed 把整个角色 SetActive(false)，Animator 随之停止更新，
///     姿势就定在死亡那一帧；
///   - 复活流程（RespawnPoint.RespawnNow → Revive() → SetActive(true)）只重置了位置 / 血量 / 死亡标记，
///     没有动 Animator；
///   - 如果死亡状态在 Animator 里没有「退出过渡」，那参数怎么变状态机也出不来
///     （PlayerAnimationController 每帧照样在设 HorizontalSpeed / Grounded，但它没法把状态机拽出死亡态）。
///
/// 这个脚本订阅 RespawnPoint.OnPlayerRespawned（复活流程里是在 SetActive(true) 之后才抛的，
/// 所以此时 Animator 一定是启用状态），然后：
///   1) Rebind()：让 Animator 回到它的默认状态
///   2) 清掉指定的 Trigger / Bool（例如 "Die" / "IsDead"，在 Inspector 里填名字，只处理确实存在的参数）
///   3) 可选：直接 Play 一个指定状态（例如 "Idle"）
///   4) Update(0f)：立刻求值，避免复活瞬间闪一帧死亡姿势
///
/// 用法：挂在玩家对象上（或者任意开局就启用的物体上，留空 animator 时脚本会自己找玩家的 Animator）。
/// </summary>
[AddComponentMenu("玩家/复活后重置动画 (Player Animator Respawn Reset)")]
[DisallowMultipleComponent]
public class PlayerAnimatorRespawnReset : MonoBehaviour
{
	[Header("要重置的 Animator（留空 = 自动找玩家的 Animator）")]
	[SerializeField] private Animator animator;

	[Header("要清掉的动画参数（按你的 Animator 填，没填就跳过）")]
	[Tooltip("死亡用的 Trigger 名称，例如 Die。复活时会 ResetTrigger")]
	[SerializeField] private string[] resetTriggers = new string[0];

	[Tooltip("死亡用的 Bool 名称，例如 IsDead。复活时会 SetBool(false)")]
	[SerializeField] private string[] clearBools = new string[0];

	[Header("复位方式")]
	[Tooltip("复活后强制切到这个状态名，例如 Idle；留空 = 只做 Rebind（回到 Animator 默认状态）")]
	[SerializeField] private string forceStateName = "";

	[Tooltip("复位后立刻求值一帧，避免闪一下死亡姿势")]
	[SerializeField] private bool updateImmediately = true;

	private void OnEnable()
	{
		RespawnPoint.OnPlayerRespawned += HandlePlayerRespawned;
	}

	private void OnDisable()
	{
		RespawnPoint.OnPlayerRespawned -= HandlePlayerRespawned;
	}

	/// <summary>玩家复活（此时角色已经 SetActive(true)，Animator 是启用的）</summary>
	private void HandlePlayerRespawned(RespawnPoint respawnPoint)
	{
		Animator target = ResolveAnimator();
		if (target == null || !target.isActiveAndEnabled) return;

		// 1) 回到默认状态
		target.Rebind();

		// 2) 清参数。放在 Rebind 之后：Rebind 有可能把参数恢复成默认值，这里再明确清一次最保险
		foreach (string trigger in resetTriggers)
		{
			if (HasParameter(target, trigger, AnimatorControllerParameterType.Trigger))
			{
				target.ResetTrigger(trigger);
			}
		}

		foreach (string flag in clearBools)
		{
			if (HasParameter(target, flag, AnimatorControllerParameterType.Bool))
			{
				target.SetBool(flag, false);
			}
		}

		// 3) 可选：直接切到指定状态（例如 Idle），用哈希调用，状态不存在就什么都不做
		if (!string.IsNullOrEmpty(forceStateName))
		{
			int stateHash = Animator.StringToHash(forceStateName);

			if (target.HasState(0, stateHash))
			{
				target.Play(stateHash, 0, 0f);
			}
		}

		// 4) 立刻求值，别让死亡姿势多显示一帧
		if (updateImmediately)
		{
			target.Update(0f);
		}
	}

	/// <summary>拿到要复位的 Animator：优先用 Inspector 里拖的，否则找玩家的（跳过影子）</summary>
	private Animator ResolveAnimator()
	{
		if (animator != null) return animator;

		foreach (PlayerController controller in FindObjectsOfType<PlayerController>())
		{
			if (controller == null || controller.IsGhost) continue;

			animator = controller.GetComponentInChildren<Animator>(true);
			break;
		}

		return animator;
	}

	/// <summary>这个名字的参数是不是真的存在（存在才去清，免得 Unity 刷警告）</summary>
	private static bool HasParameter(Animator target, string parameterName, AnimatorControllerParameterType type)
	{
		if (target == null || string.IsNullOrEmpty(parameterName)) return false;

		foreach (AnimatorControllerParameter parameter in target.parameters)
		{
			if (parameter.name == parameterName && parameter.type == type) return true;
		}

		return false;
	}
}
