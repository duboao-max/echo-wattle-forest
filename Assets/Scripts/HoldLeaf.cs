using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// HoldLeaf - 长按树叶变成屋顶(场景2)。
/// 交互:按住 1.4s → 进度弧 0→100% → 树叶升起成为屋顶 → 收集雨声。
/// 提前松手:进度保留 3s 后缓回,无惩罚。
/// 挂在一片可长按的树叶物体上。
/// </summary>
public class HoldLeaf : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Config")]
    public float holdDuration = 1.4f;        // 需要按住的时长
    public Image holdArc;                    // 进度弧(ember 色,fillAmount 0~1)
    public AudioClip rainClip;               // 收集到的雨声
    public string soundId = "rain";

    [Header("Shelter")]
    public RectTransform leafRect;           // 树叶(用于升起动画)
    public Vector2 shelterOffset = new Vector2(0, 300f); // 升起到屋顶的位移

    private bool holding = false;
    private bool completed = false;
    private float holdProgress = 0f;
    private Vector3 leafStartPos;

    void Start()
    {
        if (holdArc != null) holdArc.fillAmount = 0f;
        if (leafRect != null) leafStartPos = leafRect.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (completed) return;
        holding = true;
        StopAllCoroutines();
        StartCoroutine(HoldRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        holding = false;
        if (!completed)
        {
            // 提前松手:进度保留 3s 后缓回
            StopAllCoroutines();
            StartCoroutine(ReleaseHoldRoutine());
        }
    }

    private IEnumerator HoldRoutine()
    {
        holdProgress = 0f;
        while (holding && holdProgress < 1f)
        {
            holdProgress += Time.deltaTime / holdDuration;
            if (holdArc != null) holdArc.fillAmount = holdProgress;
            yield return null;
        }

        if (holdProgress >= 1f)
        {
            CompleteHold();
        }
    }

    private void CompleteHold()
    {
        completed = true;
        holding = false;

        // 收集雨声
        GameManager.Instance.CollectSound(soundId, rainClip);

        // 树叶升起成为屋顶
        StartCoroutine(LiftLeaf());
    }

    private IEnumerator LiftLeaf()
    {
        if (leafRect == null) yield break;
        Vector3 target = leafStartPos + (Vector3)shelterOffset;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.4f;
            leafRect.anchoredPosition = Vector3.Lerp(leafStartPos, target, t);
            yield return null;
        }
    }

    private IEnumerator ReleaseHoldRoutine()
    {
        // 保留 3 秒
        yield return new WaitForSeconds(3f);
        // 缓回
        float startProgress = holdProgress;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.6f;
            holdProgress = Mathf.Lerp(startProgress, 0f, t);
            if (holdArc != null) holdArc.fillAmount = holdProgress;
            yield return null;
        }
    }
}
