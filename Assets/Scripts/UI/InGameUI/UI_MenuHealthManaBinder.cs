using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UI_MenuHealthManaBinder : MonoBehaviour
{
    [Header("Menu UI Widgets (drag from MENU)")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TMP_Text manaText;

    [Header("Panel Root (optional)")]
    [SerializeField] private GameObject menuPanelRoot; // your Main Menu panel; if set, polling only when active

    [Header("Behavior")]
    [Tooltip("If ON, sliders expect 0..1 (like your HUD). If OFF, sliders use 0..Max and we set maxValue.")]
    [SerializeField] private bool useNormalizedSliders = true;
    [SerializeField] private float pollIntervalWhileOpen = 0.25f;
    [SerializeField] private bool logBinding = false;

    private Player player;
    private Player_Stats stats;

    private bool boundHealth;
    private bool boundMana;

    private Coroutine watchCo;

    private void OnEnable()
    {
        // Paint once immediately so the numbers are correct the first frame the menu opens.
        TryFindPlayer();
        RefreshHealth();
        RefreshMana();

        watchCo = StartCoroutine(WatchAndBindLoop());
    }

    private void OnDisable()
    {
        Unbind();
        if (watchCo != null) { StopCoroutine(watchCo); watchCo = null; }
    }



    private IEnumerator WatchAndBindLoop()
    {
        var wait = new WaitForSecondsRealtime(pollIntervalWhileOpen); // ✅ unscaled time (works while paused)

        while (true)
        {
            // Only try to bind/repaint while the menu is visible (if a root was assigned)
            if (menuPanelRoot == null || menuPanelRoot.activeInHierarchy)
            {
                if (player == null)
                {
                    TryFindPlayer();
                }
                else
                {
                    // Bind events once
                    if (!boundHealth && player.health != null)
                    {
                        player.health.OnHealthUpdate += RefreshHealth;
                        boundHealth = true;
                        if (logBinding) Debug.Log("[MenuBars] Bound Health event");
                        RefreshHealth();
                    }
                    if (!boundMana && player.mana != null)
                    {
                        player.mana.OnManaUpdate += RefreshMana;
                        boundMana = true;
                        if (logBinding) Debug.Log("[MenuBars] Bound Mana event");
                        RefreshMana();
                    }

                    // Fallback repaint every tick (covers edge cases)
                    RefreshHealth();
                    RefreshMana();
                }
            }

            yield return wait;

            // If player was destroyed (scene change), cleanly unbind and allow rebind next loop
            if (player == null)
                Unbind();
        }
    }

    private void TryFindPlayer()
    {
        var found = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (found != null && found != player)
        {
            Unbind();
            player = found;
            stats = player.stats as Player_Stats;

            if (logBinding) Debug.Log($"[MenuBars] Found Player: {player.name}");

            if (player.health != null)
            {
                player.health.OnHealthUpdate += RefreshHealth;
                boundHealth = true;
                if (logBinding) Debug.Log("[MenuBars] Bound Health event (initial)");
            }
            if (player.mana != null)
            {
                player.mana.OnManaUpdate += RefreshMana;
                boundMana = true;
                if (logBinding) Debug.Log("[MenuBars] Bound Mana event (initial)");
            }

            RefreshHealth();
            RefreshMana();
        }
    }

    private void Unbind()
    {
        if (player != null)
        {
            if (boundHealth && player.health != null)
                player.health.OnHealthUpdate -= RefreshHealth;
            if (boundMana && player.mana != null)
                player.mana.OnManaUpdate -= RefreshMana;
        }
        boundHealth = boundMana = false;
        stats = null;
    }

    private void RefreshHealth()
    {
        if (player == null || player.health == null) return;

        float cur = Mathf.RoundToInt(player.health.GetCurrentHealth());

        // ✅ Prefer live max from Health; fallback to stats if needed
        float max = player.health.GetMaxHealth();
        if (max <= 0 && stats != null) max = stats.GetMaxHealth();
        if (max <= 0) max = Mathf.Max(cur, 1); // final safety

        if (useNormalizedSliders)
        {
            if (healthSlider != null)
                healthSlider.value = max > 0 ? cur / max : 0f;
        }
        else
        {
            if (healthSlider != null)
            {
                if (!Mathf.Approximately(healthSlider.maxValue, max))
                    healthSlider.maxValue = max;
                healthSlider.value = Mathf.Clamp(cur, 0, max);
            }
        }

        if (healthText != null)
            healthText.text = $"{(int)cur}/{(int)max}";
    }

    private void RefreshMana()
    {
        if (player == null || player.mana == null) return;

        float cur = Mathf.RoundToInt(player.mana.GetCurrentMana());

        // ✅ Prefer live max from Mana; fallback to stats if needed
        float max = player.mana.GetMaxMana();
        if (max <= 0 && stats != null) max = stats.GetMaxMana();
        if (max <= 0) max = Mathf.Max(cur, 1); // final safety

        if (useNormalizedSliders)
        {
            if (manaSlider != null)
                manaSlider.value = max > 0 ? cur / max : 0f;
        }
        else
        {
            if (manaSlider != null)
            {
                if (!Mathf.Approximately(manaSlider.maxValue, max))
                    manaSlider.maxValue = max;
                manaSlider.value = Mathf.Clamp(cur, 0, max);
            }
        }

        if (manaText != null)
            manaText.text = $"{(int)cur}/{(int)max}";
    }

    // Optional: expose a manual nudge if you want to call it from UI.OpenMainMenuDirect()
    public void ForceFindAndRefresh()
    {
        TryFindPlayer();
        RefreshHealth();
        RefreshMana();
    }
}
