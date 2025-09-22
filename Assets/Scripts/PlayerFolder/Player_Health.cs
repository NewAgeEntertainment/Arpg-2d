// Player_Health.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player_Health : Entity_Health
{
    [SerializeField, Min(0f)] private float gameOverShowDelay = 0.75f;

    // -------- Invulnerability (Player-only) --------
    [Header("Invulnerability (Player)")]
    [SerializeField] private bool debugAlwaysInvulnerable = false;
    private readonly HashSet<string> _invulnSources = new HashSet<string>();
    public bool IsInvulnerable => debugAlwaysInvulnerable || _invulnSources.Count > 0;

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

    // ---- Player-only i-frame API ----
    public void AddInvulnerability(string source)
    {
        if (string.IsNullOrEmpty(source)) source = "anon";
        _invulnSources.Add(source);
    }

    public void RemoveInvulnerability(string source)
    {
        if (string.IsNullOrEmpty(source)) source = "anon";
        _invulnSources.Remove(source);
    }

    public Coroutine GrantInvulnerabilityFor(string source, float seconds)
    {
        if (seconds <= 0f) return null;
        AddInvulnerability(source);
        return StartCoroutine(RemoveInvulnerabilityAfterDelay(source, seconds));
    }

    private IEnumerator RemoveInvulnerabilityAfterDelay(string source, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        RemoveInvulnerability(source);
    }

    // ---- Short-circuit damage/knockback while invulnerable (player only) ----
    public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if (IsInvulnerable) return false;   // ignore all damage/KB during i-frames
        return base.TakeDamage(damage, elementalDamage, element, damageDealer);
    }
}
