using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// SongRibbon - 尾羽 ribbon,上滑释放最终合唱(场景4)。
/// 交互:手指上滑 → ribbon 跟随手指(半速)→ 松手后向上 unspool → 播放合唱(按收集顺序 layering)。
/// 挂在 ribbon 物体上。
/// </summary>
public class SongRibbon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Chorus Layers")]
    public List<AudioClip> chorusLayers;     // 各声音珠对应的合唱层(按 soundId 顺序)

    [Header("Animation")]
    public float followSpeed = 0.5f;         // ribbon 跟随手指速度
    public float unspoolDuration = 0.7f;     // 松手后向上展开时长

    private RectTransform rt;
    private Vector3 startPos;
    private bool released = false;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (released) return;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (released) return;
        // ribbon 跟随手指(半速,向上)
        Vector2 delta = eventData.delta;
        rt.anchoredPosition += new Vector2(0, delta.y * followSpeed);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (released) return;

        // 判断是否为"上滑"手势
        if (eventData.delta.y > 0 || rt.anchoredPosition.y > startPos.y + 100f)
        {
            released = true;
            StartCoroutine(ReleaseSong());
        }
        else
        {
            // 不是有效上滑,回弹
            StartCoroutine(ReturnToStart());
        }
    }

    private IEnumerator ReleaseSong()
    {
        // ribbon 向上 unspool
        Vector3 target = startPos + new Vector3(0, 800f, 0);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / unspoolDuration;
            rt.anchoredPosition = Vector3.Lerp(startPos, target, t);
            yield return null;
        }

        // 播放合唱
        GameManager.Instance.PlayFinalChorus(chorusLayers);
    }

    private IEnumerator ReturnToStart()
    {
        Vector3 current = rt.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.3f;
            rt.anchoredPosition = Vector3.Lerp(current, startPos, t);
            yield return null;
        }
    }
}
