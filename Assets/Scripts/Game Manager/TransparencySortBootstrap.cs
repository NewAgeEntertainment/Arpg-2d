using UnityEngine;
using UnityEngine.Rendering;

public class TransparencySortBootstrap : MonoBehaviour
{
    void Awake()
    {
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);
    }
}