using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Finish : MonoBehaviour
{
	[SerializeField] private Image image;

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.TryGetComponent<PlayerInput>(out var playerInput))
		{
			image.enabled = true;
		}
	}
}
