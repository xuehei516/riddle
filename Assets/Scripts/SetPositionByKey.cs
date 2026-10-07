using UnityEngine;
using UnityEngine.InputSystem;

public class SetPositionByKey : MonoBehaviour
{
	[Header("要移动的对象")]
	[SerializeField] private Transform targetTransform;

	[Header("目标位置")]
	[SerializeField] private Vector2 targetPosition = Vector2.zero;

	[Header("触发键")]
	[SerializeField] private Key triggerKey = Key.F3;

	private void Reset()
	{
		if (targetTransform == null) targetTransform = transform;
	}

	private void Update()
	{
		// 需要确保新 Input System 可用
		if (Keyboard.current == null)
			return;

		// 按键按下触发一次
		if (Keyboard.current[triggerKey].wasPressedThisFrame)
		{
			SetPosition();
		}
	}

	private void SetPosition()
	{
		if (targetTransform == null) 
			return;

		// 保持原 z（适用于 2D）
		var z = targetTransform.position.z;
		targetTransform.position = new Vector3(targetPosition.x, targetPosition.y, z);
	}
}