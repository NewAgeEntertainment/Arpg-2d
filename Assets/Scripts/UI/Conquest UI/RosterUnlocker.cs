using UnityEngine;

public class RosterUnlocker : MonoBehaviour
{
    public CharacterProfileSO profile; // assign in Inspector
    public Entity_Stats stats;         // optional

    public void Unlock()
    {
        ConquestRosterManager.Instance?.Unlock(profile, stats);

        // optional: refresh immediately if the roster is showing
        var ui = UI.Instance ? UI.Instance.GetComponentInChildren<UI_Conquest>(true) : null;
        ui?.ForceRosterRefresh();
    }
}
