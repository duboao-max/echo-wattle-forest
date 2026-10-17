using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BreathingButton - 呼吸动画按钮。
/// idle 时每 3 秒呼吸到 104%,提示可点击(visual affordance)。
/// 用于首页 "Begin the Story" 主按钮。
/// </summary>
public class BreathingButton : MonoBehaviour
{
    [Header("Breath")]
    public float breatheScale = 1.04f;       // 呼吸放大比例
    public float breathePeriod = 3f;         // 呼吸周期(秒)

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        // 用正弦波模拟呼吸(周期 3s)
        float t = (Mathf.Sin(Time.time / breathePeriod * Mathf.PI * 2f) + 1f) / 2f; // 0~1
        float scale = Mathf.Lerp(1f, breatheScale, t);
        transform.localScale = baseScale * scale;
    }
}
