// Attach this to any always-active object (like GameManager or SaveSystem)
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class CameraFollowManager : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var player = FindAnyObjectByType<Player>();
        var virtualCam = FindAnyObjectByType<CinemachineCamera>();

        if (player == null)
        {
            Debug.LogWarning("[CameraFollowManager] Player not found in scene.");
            return;
        }

        if (virtualCam == null)
        {
            Debug.LogWarning("[CameraFollowManager] Cinemachine Virtual Camera not found in scene.");
            return;
        }

        virtualCam.Follow = player.transform;
        virtualCam.LookAt = player.transform;

        Debug.Log($"[CameraFollowManager] Assigned player to virtual camera in scene: {scene.name}");
    }
}
