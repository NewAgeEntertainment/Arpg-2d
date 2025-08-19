// UIBootstrap.cs
using UnityEngine;

public class UIBootstrap : MonoBehaviour
{
    [SerializeField] private UI uiPrefab; // drag your UI prefab with the UI singleton component

    private void Awake()
    {
        if (UI.Instance == null && uiPrefab != null)
        {
            var ui = Instantiate(uiPrefab);
            ui.name = uiPrefab.name; // keeps the hierarchy tidy
        }

        // This object doesn't need to persist.
        Destroy(gameObject);
    }
}

