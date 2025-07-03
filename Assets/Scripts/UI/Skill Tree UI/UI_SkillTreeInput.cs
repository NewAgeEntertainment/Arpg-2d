using UnityEngine;
using Rewired;

public class UI_SkillTreeInput : MonoBehaviour
{
    [SerializeField] private UI ui; // Reference to your UI manager
    [SerializeField] private int playerID = 0;
    [SerializeField] private string toggleSkillTreeAction = "OpenSkillTree";

    private Rewired.Player player;

    void Start()
    {
        player = ReInput.players.GetPlayer(playerID);

        if (ui == null)
            ui = FindObjectOfType<UI>();
    }

    void Update()
    {
        //if (player.GetButtonDown(toggleSkillTreeAction))
        //{
        //    ui.OpenSkillTreeUI();
        //}
    }
}
