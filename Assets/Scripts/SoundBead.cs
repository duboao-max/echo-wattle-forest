using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// SoundBead - 点击收集的声音珠。
/// 交互:点击(Tap)→ 放大 flare → 飞到 ribbon → 播放音效 + 触觉反馈。
/// 包含 idle 漂浮动画(±6px)。
/// 挂在每个声音珠物体上,附带 CanvasGroup / RectTransform / Image。
/// </summary>
public class SoundBead : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    public string soundId = "wind";          // 声音珠唯一标识(用于合唱合成)
    public AudioClip collectClip;            // 收集时播放的音效

    [Header("Animation")]
    public float idleDriftAmount = 6f;       // idle 漂浮幅度(px)
    public float idleDriftSpeed = 2f;        // idle 漂浮速度
    public float flareScale = 1.6f;          // 按下放大倍数
    public float flyDuration = 0.32f;        // 飞到 ribbon 的时长

    private Vector3 startPos;
    private float idleSeed;
    private bool collected = false;
    private RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
        idleSeed = Random.Range(0f, 100f);   // 每个珠子相位不同
    }

    void Update()
    {
        if (collected) return;

        // idle 漂浮(垂直 ±6px,sin 波形)
        float offset = Mathf.Sin(Time.time * idleDriftSpeed + idleSeed) * idleDriftAmount;
        rt.anchoredPosition = startPos + new Vector3(0, offset, 0);
    }

    /// <summary>
    /// 点击收集。
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (collected) return;
        collected = true;

        // 通知 GameManager 收集
        GameManager.Instance.CollectSound(soundId, collectClip);

        // 播放收集动画:放大 → 飞向 ribbon
        StartCoroutine(CollectAnimation());
    }

    private IEnumerator CollectAnimation()
    {
        Vector3 originalScale = rt.localScale;

        // 1. flare 放大
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.12f;
            rt.localScale = Vector3.Lerp(originalScale, originalScale * flareScale, t);
            yield return null;
        }

        // 2. 飞向 ribbon 槽(用 GameManager 的 slot 位置)
        RectTransform target = GameManager.Instance.ribbonSlotParent as RectTransform;
        Vector3 targetWorld = target != null ? target.position : rt.position;

        t = 0f;
        Vector3 startWorld = rt.position;
        while (t < 1f)
        {
            t += Time.deltaTime / flyDuration;
            rt.position = Vector3.Lerp(startWorld, targetWorld, t);
            rt.localScale = Vector3.Lerp(originalScale * flareScale, originalScale * 0.5f, t);
            yield return null;
        }

        // 3. 隐藏本珠(ribbon 上已由 GameManager 生成新的显示珠)
        gameObject.SetActive(false);
    }
}
