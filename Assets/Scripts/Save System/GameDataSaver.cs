// Assets/Scripts/Save System/GameDataSaver.cs
using PixelCrushers;
using System;
using System.Collections;
using UnityEngine;
using SkillTreeState = UI_SkillTree.SkillTreeState;

[DisallowMultipleComponent]
public class GameDataSaver : Saver
{
    [Header("Auto-found if not assigned")]
    [SerializeField] private Player player;
    [SerializeField] private Player_Stats stats;
    [SerializeField] private Entity_Health health;
    [SerializeField] private Entity_Mana mana;

    [Serializable]
    public class SaveBlob
    {
        // Vitals
        public float currentHealth;
        public float currentMana;

        // Normal Level/EXP
        public int level;
        public float currentExp;

        // Sex Level/EXP
        public int sexLevel;
        public float sexExp;

        // Scene & position
        public string lastScene;
        public Vector3 lastPlayerPosition;

        // Skill tree
        public SkillTreeState skillTree = new SkillTreeState();
    }

    private void Awake()
    {
        CacheRefs();
    }

    private void CacheRefs()
    {
        if (player == null) player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (stats == null) stats = player != null ? player.GetComponent<Player_Stats>() : null;
        if (health == null) health = player != null ? player.GetCurrentComponent<Entity_Health>() : null;
        if (mana == null) mana = player != null ? player.GetCurrentComponent<Entity_Mana>() : null;
    }

    public override string RecordData()
    {
        CacheRefs();

        var data = new SaveBlob();

        // Vitals
        data.currentHealth = health != null ? health.GetCurrentHealth() : 0f;
        data.currentMana = mana != null ? mana.GetCurrentMana() : 0f;

        // Normal Level/EXP
        if (stats != null)
        {
            data.level = stats.CurrentLevel;
            data.currentExp = stats.CurrentEXP;
        }

        // Sex Level/EXP
        if (player != null)
        {
            data.sexLevel = player.SexLevel;
            data.sexExp = player.CurrentSexExp;
        }

        // Scene & position
        data.lastScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (player != null) data.lastPlayerPosition = player.transform.position;

        // Skill tree
        data.skillTree = CaptureSkillTreeState();

        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        CacheRefs();
        if (string.IsNullOrEmpty(s)) return;

        var data = SaveSystem.Deserialize<SaveBlob>(s);
        if (data == null) return;

        // 1) Level first -> this rebuilds maxMana/maxHealth via your RebuildLevelUpBonuses...
        if (stats != null)
            stats.SetLevelAndExp(data.level, data.currentExp);

        // 2) Now vitals (clamp against the CORRECT max)
        if (health != null)
            health.SetCurrentHealth(Mathf.Clamp(
                data.currentHealth, 0f,
                stats != null ? stats.GetMaxHealth() : data.currentHealth));

        if (mana != null)
            mana.SetCurrentMana(Mathf.Clamp(
                data.currentMana, 0f,
                stats != null ? stats.GetMaxMana() : data.currentMana));

        // 3) Sex EXP
        if (player != null)
        {
            player.SetSexLevel(data.sexLevel);
            player.SetCurrentSexEXP(data.sexExp);
        }

        // UI refresh...
        player?.ui?.inGameUI?.UpdateExpBar();
        player?.ui?.inGameUI?.UpdateSexExpBar();
        player?.ui?.playerHealthBar?.UpdateHealth(health?.GetCurrentHealth() ?? 0, stats?.GetMaxHealth() ?? 0);
        player?.ui?.playerManaBar?.UpdateMana(mana?.GetCurrentMana() ?? 0, stats?.GetMaxMana() ?? 0);
        if (player?.ui?.StatusPanel != null && player.ui.StatusPanel.IsOpen)
            player.ui.StatusPanel.RefreshCurrentCharacter();

        StartCoroutine(ApplySkillTreeWhenReady(data.skillTree));
    }

   

    private SkillTreeState CaptureSkillTreeState()
    {
        var state = new SkillTreeState();
        var tree = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include);
        if (tree == null) return state;

        var nodes = tree.GetComponentsInChildren<UI_TreeNode>(true);
        foreach (var n in nodes)
        {
            if (n == null || n.skillData == null) continue;
            var name = n.skillData.name;
            if (n.isUnlocked) state.unlockedSkillNames.Add(name);
            if (n.isLocked) state.lockedSkillNames.Add(name);
        }

        state.combatSkillPoints = tree.GetCombatSkillPoints();
        state.sexSkillPoints = tree.GetSexSkillPoints();
        return state;
    }

    private IEnumerator ApplySkillTreeWhenReady(SkillTreeState state)
    {
        UI_SkillTree tree = null;
        const float timeout = 3f;
        float t = 0f;

        while (tree == null && t < timeout)
        {
            tree = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include);
            if (tree != null) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (tree != null)
        {
            yield return null; // ensure Start ran
            tree.ApplySaveState(state);
            tree.UpdateAllConnections();

            // If your UI needs a rebinding of icons after skill unlock:
            yield return null;
            var ui = FindFirstObjectByType<UI>(FindObjectsInactive.Include);
            ui?.inGameUI?.RefreshSkillSlotsFromTree(tree);
        }
        else
        {
            UI_SkillTree.SetPendingState(state);
        }
    }
}

// Small helper so null GetComponent logs don’t surprise you
public static class ComponentExt
{
    public static T GetCurrentComponent<T>(this Component c) where T : Component =>
        c != null ? c.GetComponent<T>() : null;
}
