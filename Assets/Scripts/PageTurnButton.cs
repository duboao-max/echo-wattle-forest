using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// PageTurnButton - 统一的翻页按钮(羽毛形)。
/// 交互:点击 → 按压缩放(96%)→ 播放翻页音效 → 切换到下一页。
/// 所有故事页使用相同样式,保证跨页一致性。
/// </summary>
public class PageTurnButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Navigation")]
    public string nextPageName = "scene2";   // 要跳转的页面名

    [Header("Animation")]
    public AudioClip pageTurnClip;           // 翻页音效
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 按压缩放 96%
        transform.localScale = originalScale * 0.96f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 播放翻页音效
        if (pageTurnClip != null)
            GameManager.Instance.sfxSource.PlayOneShot(pageTurnClip);

        // 切换页面
        GameManager.Instance.ShowPage(nextPageName);
    }
}
