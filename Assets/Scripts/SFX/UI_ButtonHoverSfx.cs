using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ButtonHoverSfx : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Header("Audio")]
    [SerializeField] private bool playOnPointerEnter = true;
    [SerializeField] private bool playOnSelect = false;
    [SerializeField] private string hoverSoundName = "UIButtonMove";

    private float lastPlayTime = -999f;
    private const float MinRepeatGap = 0.05f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playOnPointerEnter) return;
        PlayHoverSfx();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!playOnSelect) return;
        PlayHoverSfx();
    }

    private void PlayHoverSfx()
    {
        if (AudioManager.instance == null) return;
        if (string.IsNullOrWhiteSpace(hoverSoundName)) return;
        if (Time.unscaledTime - lastPlayTime < MinRepeatGap) return;

        lastPlayTime = Time.unscaledTime;
        AudioManager.instance.PlayGlobalSFX(hoverSoundName);
    }
}