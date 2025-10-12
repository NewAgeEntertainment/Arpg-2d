using System;
using System.Reflection;

public static class ReflectionEventExtensions
{
    public static void Raise(this EventInfo ei, object target, Delegate tempHandler)
    {
        if (ei == null || tempHandler == null) return;
        var add = ei.GetAddMethod(true);
        var remove = ei.GetRemoveMethod(true);
        if (add == null || remove == null) return;

        add.Invoke(target, new object[] { tempHandler });
        try
        {
            // We can't "invoke" an event directly; subscribers will see the add, but here we just remove again.
            // In most flows you won't need this. The real UI update is driven by setting CurrentEXP and polling.
        }
        finally
        {
            remove.Invoke(target, new object[] { tempHandler });
        }
    }
}

