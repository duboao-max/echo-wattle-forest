using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ToggleControl - Read to Me / Quiet Mode 开关。
/// 点击切换开关状态,状态带永久文字标签(色盲也可读)。
/// type 决定是哪个开关。
/// </summary>
public class ToggleControl : MonoBehaviour, IPointerClickHandler
{
    public enum ToggleType { ReadToMe, QuietMode }

    [Header("Config")]
    public ToggleType type = ToggleType.QuietMode;
    public Image toggleKnob;                 // 开关滑块
    public Text stateLabel;                  // 状态文字标签(ON/OFF 永久显示)
    public AudioClip toggleClip;             // 切换音效

    private bool isOn = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        isOn = !isOn;

        // 更新 GameManager 状态
        if (type == ToggleType.QuietMode)
            GameManager.Instance.quietMode = isOn;
        else
            GameManager.Instance.readToMe = isOn;

        // 播放音效
        if (toggleClip != null && !GameManager.Instance.quietMode)
            GameManager.Instance.sfxSource.PlayOneShot(toggleClip);

        // 更新视觉
        if (toggleKnob != null)
        {
            // 滑块左右移动(假设 knob 在父容器内)
            Vector3 pos = toggleKnob.rectTransform.anchoredPosition;
            pos.x = isOn ? 44f : -44f; // 左右滑动
            toggleKnob.rectTransform.anchoredPosition = pos;
        }
        if (stateLabel != null)
            stateLabel.text = isOn ? "ON" : "OFF";
    }
}
