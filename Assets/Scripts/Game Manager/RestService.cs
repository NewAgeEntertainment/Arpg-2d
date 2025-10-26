using UnityEngine;
using System;
using System.Reflection;

public static class RestService
{
    /// <summary>
    /// Restores both health and mana of the target actor to full.
    /// Returns true if anything was restored.
    /// </summary>
    public static bool RestoreActorFull(GameObject actor)
    {
        if (!actor) return false;

        bool did = false;
        did |= TryRestoreHealthFull(actor);
        did |= TryRestoreManaFull(actor);

        return did;
    }

    // ---------------- Health ----------------

    private static bool TryRestoreHealthFull(GameObject actor)
    {
        // Look for Entity_Health or a subclass (e.g., Player_Health)
        var health = actor.GetComponentInChildren<Component>(c => c != null && IsTypeNamed(c.GetType(), "Entity_Health"));
        if (health == null) return false;

        var t = health.GetType();

        // Preferred explicit APIs (any of these if present):
        // - void SetCurrentHealth(float)
        // - float GetMaxHealth()
        // - void HealToFull()
        // - void FullHeal()
        // - void Heal(float amount)
        // - currentHealth field
        // We’ll try clean methods first, then fallbacks.

        // 1) HealToFull / FullHeal
        if (InvokeIfExists(t, health, "HealToFull")) return true;
        if (InvokeIfExists(t, health, "FullHeal")) return true;

        // 2) SetCurrentHealth(GetMaxHealth())
        float max = 0f;
        if (TryCallFloat(t, health, "GetMaxHealth", out max))
        {
            if (InvokeIfExists(t, health, "SetCurrentHealth", max)) return true;

            // 3) No SetCurrentHealth? Try a public field 'currentHealth'
            if (TrySetField(t, health, "currentHealth", max)) return true;

            // 4) As a last resort, call Heal(max) or a huge amount
            if (InvokeIfExists(t, health, "Heal", max)) return true;
            if (InvokeIfExists(t, health, "IncreaseHealth", max)) return true;
            // Really last resort: Heal a very large amount
            if (InvokeIfExists(t, health, "Heal", 9_999_999f)) return true;
        }
        else
        {
            // No GetMaxHealth; try: HealToFull/FullHeal (already tried), or set currentHealth to a big number
            if (TrySetField(t, health, "currentHealth", 9_999_999f)) return true;
            if (InvokeIfExists(t, health, "Heal", 9_999_999f)) return true;
        }

        return false;
    }

    // ---------------- Mana ----------------

    private static bool TryRestoreManaFull(GameObject actor)
    {
        var mana = actor.GetComponentInChildren<Component>(c => c is MonoBehaviour && IsTypeNamed(c.GetType(), "Entity_Mana"));
        if (mana == null) return false;

        var t = mana.GetType();

        // We know from your script: SetCurrentMana(float) and GetMaxMana()
        float max;
        if (TryCallFloat(t, mana, "GetMaxMana", out max))
        {
            if (InvokeIfExists(t, mana, "SetCurrentMana", max)) return true;
            if (TrySetField(t, mana, "currentMana", max)) return true;
            if (InvokeIfExists(t, mana, "IncreaseMana", max)) return true;
            if (InvokeIfExists(t, mana, "IncreaseMana", 9_999_999f)) return true;
        }
        else
        {
            // Fallbacks if API differs
            if (InvokeIfExists(t, mana, "SetManaToPercent", 1f)) return true;
            if (TrySetField(t, mana, "currentMana", 9_999_999f)) return true;
        }

        return false;
    }

    // ---------------- Reflection helpers ----------------

    private static bool IsTypeNamed(Type type, string typeName)
    {
        while (type != null)
        {
            if (type.Name == typeName) return true;
            type = type.BaseType;
        }
        return false;
    }

    private static bool InvokeIfExists(Type t, object instance, string methodName)
    {
        var m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (m == null) return false;
        m.Invoke(instance, null);
        return true;
    }

    private static bool InvokeIfExists(Type t, object instance, string methodName, float arg)
    {
        var m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
        if (m == null) return false;
        m.Invoke(instance, new object[] { arg });
        return true;
    }

    private static bool TryCallFloat(Type t, object instance, string methodName, out float result)
    {
        result = 0f;
        var m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (m == null || m.ReturnType != typeof(float)) return false;
        result = (float)m.Invoke(instance, null);
        return true;
    }

    private static bool TrySetField(Type t, object instance, string fieldName, float value)
    {
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return false;
        if (f.FieldType == typeof(float))
        {
            f.SetValue(instance, value);
            // Try to notify UI if a suitable event exists
            // Mana has OnManaUpdate; Health often has OnHealthUpdate—best effort:
            TryInvokeUpdateEvent(t, instance);
            return true;
        }
        return false;
    }

    private static void TryInvokeUpdateEvent(Type t, object instance)
    {
        // Try common pattern: public event Action OnXUpdate;
        // We can't raise events via reflection easily; instead try calling methods often present:
        // UpdateHealthBar, UpdateManaBar, or OnHealthUpdate/OnManaUpdate as methods.
        if (!InvokeIfExists(t, instance, "UpdateHealthBar"))
            InvokeIfExists(t, instance, "UpdateManaBar");
    }

    // Convenience: LINQ-like finder without LINQ allocs
    private static T GetComponentInChildren<T>(this GameObject go, Predicate<T> pred) where T : Component
    {
        var all = go.GetComponentsInChildren<T>(true);
        for (int i = 0; i < all.Length; i++)
            if (pred(all[i])) return all[i];
        return null;
    }
}
