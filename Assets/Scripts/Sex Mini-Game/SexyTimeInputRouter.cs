using UnityEngine;
using Rewired;

[DisallowMultipleComponent]
public class SexyTimeInputRouter : MonoBehaviour
{
    [Header("Rewired")]
    [SerializeField] private int rewiredPlayerId = 0;

    [Header("Rewired Categories")]
    [SerializeField] private string gameplayCategory = "Gameplay";
    [SerializeField] private string sexyTimeCategory = "SexyTime";

    private Rewired.Player rPlayer;

    public static class Actions
    {
        public const string StartSexyTime = "StartSexyTime";
        public const string Stroke = "Stroke";
        public const string DeepBreathe = "DeepBreathe";
        public const string Thrust = "Thrust";          // optional
        public const string Pause = "PauseSexyTime";   // optional
        public const string Skip = "SkipMiniGame";    // optional
    }

    public void Init(int? overridePlayerId = null)
    {
        int id = overridePlayerId ?? rewiredPlayerId;
        rPlayer = ReInput.players.GetPlayer(id);
    }

    #region Category toggling

    public void EnableSexyTimeMaps()
    {
        if (rPlayer == null) return;
        rPlayer.controllers.maps.SetMapsEnabled(false, gameplayCategory);
        rPlayer.controllers.maps.SetMapsEnabled(true, sexyTimeCategory);
    }

    public void DisableSexyTimeMaps()
    {
        if (rPlayer == null) return;
        rPlayer.controllers.maps.SetMapsEnabled(false, sexyTimeCategory);
        rPlayer.controllers.maps.SetMapsEnabled(true, gameplayCategory);
    }

    #endregion

    #region Reads

    public bool StartPressed() => rPlayer != null && rPlayer.GetButtonDown(Actions.StartSexyTime);
    public bool StrokePressed() => rPlayer != null && rPlayer.GetButtonDown(Actions.Stroke);
    public bool DeepBreathePressed() => rPlayer != null && rPlayer.GetButtonDown(Actions.DeepBreathe);
    public bool PausePressed() => rPlayer != null && rPlayer.GetButtonDown(Actions.Pause);
    public bool SkipPressed() => rPlayer != null && rPlayer.GetButtonDown(Actions.Skip);
    public bool ThrustPressed() => rPlayer != null && rPlayer.GetButtonDown(Actions.Thrust);

    #endregion
}
