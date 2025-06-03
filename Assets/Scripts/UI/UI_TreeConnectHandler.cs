using System;
using UnityEngine.UI;
using UnityEngine;


[Serializable]
public class UI_TreeConnectionDetails
{
    public UI_TreeConnectHandler childNode; // reference to the child node handler
    // direction type
    public NodeDirectionType direction;
    // length of the connection
    [Range(100f, 350f)] public float length;
    [Range(-50f, 50f)] public float rotation;
}
public class UI_TreeConnectHandler : MonoBehaviour
{
    private RectTransform rect => GetComponent<RectTransform>();
    [SerializeField] private UI_TreeConnectionDetails[] connectionDetails;
    [SerializeField] private UI_TreeConnection[] connections;

    // variable for connection line image
    private Image connectionImage;
    private Color OriginalColor;

    private void Awake()
    {
        if(connectionImage != null)
            OriginalColor = connectionImage.color;
    }

    private void OnValidate()
    {

        if (connectionDetails.Length <= 0)
            return;
                

        if (connectionDetails.Length != connections.Length)
        {
            Debug.Log("Amount of Details should be same as amount of connections. - " + gameObject.name);
            return;
        }

        UpdateConnections();
    }

    public void UpdateConnections()
    {
        for (int i = 0; i < connectionDetails.Length; i++)
        {
            var detail = connectionDetails[i];
            var connection = connections[i];
            
            Vector2 targetPosition = connection.GetConnectionPoint(rect);
            Image connectionImage = connection.GetConnectionImage();

            connection.DirectConnection(detail.direction, detail.length, detail.rotation);
            
            if (detail.childNode == null)
                continue;

            detail.childNode.SetPosition(targetPosition);
            detail.childNode.SetConnectionImage(connectionImage);
            detail.childNode.transform.SetAsLastSibling(); // Ensure the child node is rendered above the connection line
        }
    }

    public void UpdateAllConnections()
    {
        UpdateConnections(); // Call the method to update connections

        foreach (var node in connectionDetails)
        {
            if (node.childNode == null) continue;
            node.childNode?.UpdateConnections(); // Update child nodes connections
        }
    }

    public void UnlockConnectionImage(bool unlocked)
    {
        if (connectionImage == null)
            return;

        connectionImage.color = unlocked ? Color.white : OriginalColor;
    }


    public void SetConnectionImage(Image image) => connectionImage = image; 
    public void SetPosition(Vector2 position) => rect.anchoredPosition = position;
}
