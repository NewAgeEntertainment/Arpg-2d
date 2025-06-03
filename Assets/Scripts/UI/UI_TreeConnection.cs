using UnityEngine;
using UnityEngine.UI;

public class UI_TreeConnection : MonoBehaviour
{

    [SerializeField] private RectTransform rotationPoint;
    [SerializeField] private RectTransform connectionLength;
    [SerializeField] private RectTransform childNodeConnectionPoint;

    public void DirectConnection(NodeDirectionType direction, float length, float offset)
    {
        bool shouldReActive = direction != NodeDirectionType.None;
        float finalLength = shouldReActive ? length : 0f;
        float angle = GetDirectionAngle(direction);

        rotationPoint.localRotation = Quaternion.Euler(0f, 0f, angle + offset);
        connectionLength.sizeDelta = new Vector2(finalLength, connectionLength.sizeDelta.y);
    }

    // get Image from the connection line Game object and use it assign it somewhere
    public Image GetConnectionImage() => connectionLength.GetComponent<Image>();

    public Vector2 GetConnectionPoint(RectTransform rect)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            rect.parent as RectTransform, // Parent RectTransform
            childNodeConnectionPoint.position,
            null, // Use the current camera
            out var localPosition        
        );

        return localPosition;
    }

    private float GetDirectionAngle(NodeDirectionType type)
    {
        switch (type)
        {
            case NodeDirectionType.UpLeft:
                return 135f;
            case NodeDirectionType.Up:
                return 90f;
            case NodeDirectionType.UpRight:
                return 45f;
            case NodeDirectionType.Left:
                return 180f;
            case NodeDirectionType.Right:
                return 0f;
            case NodeDirectionType.DownLeft:
                return -135f;
            case NodeDirectionType.Down:
                return -90f;
            case NodeDirectionType.DownRight:
                return -45f;
            default: return 0f;
        }
    }


}
    public enum NodeDirectionType
    {
        None,
        UpLeft,
        Up,
        UpRight,
        Left,
        Right,
        DownLeft,
        Down,
        DownRight

    }
