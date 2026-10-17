using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FadeTransition - 页面淡入过渡(400ms ease-out)。
/// 用于页面切换时的视觉反馈,对应 A1 的"400ms fade"规范。
/// 挂在每个页面的 CanvasGroup 上。
/// </summary>
public class FadeTransition : MonoBehaviour
{
    [Header("Fade")]
    public float fadeDuration = 0.4f;        // 淡入时长(秒)
    public CanvasGroup canvasGroup;

    void OnEnable()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            // ease-out: 1 - (1-t)^2
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            canvasGroup.alpha = eased;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
