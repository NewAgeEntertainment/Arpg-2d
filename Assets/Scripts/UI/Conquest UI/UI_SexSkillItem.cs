using TMPro;
using UnityEngine;

public class UI_SexSkillItem : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    public void Set(string text) { if (label) label.text = text; }
}
