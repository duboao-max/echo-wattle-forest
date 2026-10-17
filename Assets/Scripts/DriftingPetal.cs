using System.Collections;
using UnityEngine;

/// <summary>
/// DriftingPetal - 背景飘落的金合欢花瓣粒子。
/// 用于首页(3 朵飘落)与场景4(合唱后花瓣飘落)。
/// 挂在花瓣物体上,从顶部落到底部循环。
/// </summary>
public class DriftingPetal : MonoBehaviour
{
    [Header("Drift")]
    public float driftSpeed = 12f;           // 下落速度(px/s,对应 A1 的 12px/s)
    public float swayAmount = 20f;           // 左右摆动幅度
    public float swaySpeed = 1.5f;           // 摆动速度

    private RectTransform rt;
    private float startX;
    private float seed;
    private float topY = 1000f;
    private float bottomY = -100f;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startX = rt.anchoredPosition.x;
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        // 向下漂移
        Vector2 pos = rt.anchoredPosition;
        pos.y -= driftSpeed * Time.deltaTime;

        // 左右摆动
        pos.x = startX + Mathf.Sin(Time.time * swaySpeed + seed) * swayAmount;

        rt.anchoredPosition = pos;

        // 循环:到底后回到顶部
        if (pos.y < bottomY)
        {
            pos.y = topY;
            rt.anchoredPosition = pos;
        }
    }
}
