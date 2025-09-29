using UnityEngine;
using UnityEngine.EventSystems;

public class SelectOnHover : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }
}

