using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedFire : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private GameObject fireUI;

	private void Awake()
	{
		fireUI.SetActive(false);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		fireUI.SetActive(true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		fireUI.SetActive(false);
	}
}
