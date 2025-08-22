// Player_Health.cs
using UnityEngine;
using System.Collections;

public class Player_Health : Entity_Health
{
    [SerializeField, Min(0f)] private float gameOverShowDelay = 0.75f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable() { Player.OnPlayerDeath += HandlePlayerDeath; }
    private void OnDisable() { Player.OnPlayerDeath -= HandlePlayerDeath; }

    private void HandlePlayerDeath()
    {
        StartCoroutine(ShowGameOverAfterDelay());
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(gameOverShowDelay);
        UI_GameOver.ShowStatic(); // finds the panel (even if inactive) and shows it
    }
}
