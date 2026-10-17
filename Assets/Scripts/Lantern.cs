using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Lantern - 场景3 的灯笼。
/// 交互:点击 → 发光呼吸动画 → 收集"点亮"反馈。
/// 是"响亮的声音"这一页的搜索焦点:唯一全亮区域。
/// </summary>
public class Lantern : MonoBehaviour, IPointerClickHandler
{
    [Header("Glow")]
    public Image glowImage;                  // 发光光晕
    public float breatheMin = 0.22f;         // 发光最小值(22%)
    public float breatheMax = 1.0f;          // 发光最大值(100%)
    public float breatheDuration = 0.9f;     // 呼吸周期(A1:900ms)

    [Header("Feedback")]
    public AudioClip lanternClip;
    public string soundId = "lantern";

    private bool lit = false;

    void Start()
    {
        StartCoroutine(GlowBreathe());
    }

    private IEnumerator GlowBreathe()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime / breatheDuration;
            float v = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) / 2f; // 0~1
            float alpha = Mathf.Lerp(breatheMin, breatheMax, v);
            if (glowImage != null)
            {
                Color c = glowImage.color;
                c.a = alpha;
                glowImage.color = c;
            }
            yield return null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (lit) return;
        lit = true;

        GameManager.Instance.CollectSound(soundId, lanternClip);
        // 点亮后光晕固定最亮
        if (glowImage != null)
        {
            Color c = glowImage.color;
            c.a = breatheMax;
            glowImage.color = c;
        }
    }
}
