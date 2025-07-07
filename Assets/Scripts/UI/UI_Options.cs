using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Options : MonoBehaviour
{
    private Player player;
    [SerializeField] private Toggle healthBarToggle;
    [SerializeField] private Toggle manaBarToggle;


    private void Start()
    {
        player = FindFirstObjectByType<Player>();

        healthBarToggle.onValueChanged.AddListener(OnHealthToggleChanged);
        manaBarToggle.onValueChanged.AddListener(OnManaToggleChanged);

    }

    private void OnHealthToggleChanged(bool isOn)
    {
        player.health.EnableHealthBar(isOn);
    }

    private void OnManaToggleChanged(bool isOn)
    {
        player.mana.EnableManaBar(isOn);
    }
}
