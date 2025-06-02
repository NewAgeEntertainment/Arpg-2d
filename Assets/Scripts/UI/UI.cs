using UnityEngine;

public class UI : MonoBehaviour
{
    public UI_SkillToolTip skilltoolTip;

    private void Awake()
    {
        skilltoolTip = GetComponentInChildren<UI_SkillToolTip>();
    }
}
