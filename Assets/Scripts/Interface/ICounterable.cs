using UnityEngine;

public interface ICounterable
{

    public bool CanBeCountered { get; } // Property to check if the entity can be countered.

    public void HandleCounter();
}
