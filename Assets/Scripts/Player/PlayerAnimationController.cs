using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerAnimationEvent))]
[DisallowMultipleComponent]
public class PlayerAnimationController : MonoBehaviour
{
	#region 动画参数哈希
	private static readonly int HorizontalSpeedHash =
		Animator.StringToHash("HorizontalSpeed");

	private static readonly int VerticalSpeedHash =
		Animator.StringToHash("VerticalSpeed");

	private static readonly int GroundedHash =
		Animator.StringToHash("Grounded");

	private static readonly int AttackHash =
		Animator.StringToHash("Attack");
	#endregion

	private Animator animator;
	private SpriteRenderer spriteRenderer;

	private PlayerAnimationEvent playerAnimationEvent;

	private void Awake()
	{
		playerAnimationEvent = GetComponent<PlayerAnimationEvent>();

		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		playerAnimationEvent.OnAnimationStateChanged += OnAnimationStateChanged;

		playerAnimationEvent.OnAttack += OnAttack;
	}

	private void OnDisable()
	{
		playerAnimationEvent.OnAnimationStateChanged -= OnAnimationStateChanged;

		playerAnimationEvent.OnAttack -= OnAttack;
	}

	private void OnAnimationStateChanged(PlayerAnimationEvent playerAnimationEvent, PlayerAnimationStateArgs playerAnimationStateArgs)
	{
		animator.SetFloat(HorizontalSpeedHash, playerAnimationStateArgs.horizontalSpeed);
		animator.SetFloat(VerticalSpeedHash, playerAnimationStateArgs.verticalSpeed);
		animator.SetBool(GroundedHash, playerAnimationStateArgs.isGrounded);

		spriteRenderer.flipX = playerAnimationStateArgs.facingLeft;
	}

	private void OnAttack(PlayerAnimationEvent playerAnimationEvent)
	{
		animator.SetTrigger(AttackHash);
	}
}
