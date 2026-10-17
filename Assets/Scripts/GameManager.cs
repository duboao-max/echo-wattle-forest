using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameManager - 全局状态管理器。
/// 负责:页面切换、声音珠收集、尾羽 ribbon 进度更新、合唱合成、Quiet Mode、Read to Me 开关。
/// 采用单例模式,挂在场景中唯一的 GameManager 物体上。
///
/// 适配你已有的 Main.unity:
///   - ribbon 的 7 个槽位(Ribbon_Slot1~7)是场景里静态设计好的,本脚本【不销毁】它们,
///     而是用「点亮/置灰」的方式表示收集进度(改 slot 下 Image 的颜色)。
///   - 找不到 foundCounterText / ribbonBeadPrefab 时优雅跳过,不会报空引用。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Pages (assign all 6 page GameObjects)")]
    public GameObject homePage;
    public GameObject scene1Page;
    public GameObject scene2Page;
    public GameObject scene3Page;
    public GameObject scene4Page;
    public GameObject creditsPage;

    [Header("Song Ribbon UI")]
    public Transform ribbonSlotParent;      // 尾羽 ribbon 的 slot 容器(里面有 Ribbon_Slot1~7)
    public List<Transform> allRibbons = new List<Transform>();  // 所有页面的 Ribbon(可选,用于全页同步)
    public Text foundCounterText;            // "2 of 7 sounds found" 文本(可空)

    [Header("Feedback")]
    public AudioSource sfxSource;            // 一次性音效
    public AudioSource ambienceSource;       // 环境底噪
    public AudioSource chorusSource;         // 合唱播放

    // 收集到的声音珠(按收集顺序)
    private List<string> collectedSounds = new List<string>();
    private const int MAX_SLOTS = 7;

    // 全局开关
    public bool quietMode = false;
    public bool readToMe = false;

    // 已收集的颜色(点亮金色)与未收集颜色(置灰)
    private Color litColor = new Color(0.89f, 0.65f, 0.24f, 1f);    // wattle gold
    private Color dimColor = new Color(0.36f, 0.42f, 0.38f, 0.35f); // 半透明灰绿

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 注意:不要 DontDestroyOnLoad,因为这是单场景应用,保留会导致场景重载时出现两个实例
    }

    void Start()
    {
        // 初始只显示首页
        ShowPage("home");
    }

    /// <summary>
    /// 切换到指定页面(只显示一个页面,其余隐藏)。
    /// </summary>
    public void ShowPage(string pageName)
    {
        SetActiveSafe(homePage, false);
        SetActiveSafe(scene1Page, false);
        SetActiveSafe(scene2Page, false);
        SetActiveSafe(scene3Page, false);
        SetActiveSafe(scene4Page, false);
        SetActiveSafe(creditsPage, false);

        switch (pageName.ToLower())
        {
            case "home":    SetActiveSafe(homePage, true); break;
            case "scene1":  SetActiveSafe(scene1Page, true); break;
            case "scene2":  SetActiveSafe(scene2Page, true); break;
            case "scene3":  SetActiveSafe(scene3Page, true); break;
            case "scene4":  SetActiveSafe(scene4Page, true); break;
            case "credits": SetActiveSafe(creditsPage, true); break;
        }

        RefreshRibbon();
    }

    private void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    /// <summary>
    /// 收集一个声音珠。
    /// </summary>
    public void CollectSound(string soundId, AudioClip clip)
    {
        if (collectedSounds.Count >= MAX_SLOTS)
            return;

        collectedSounds.Add(soundId);

        // 播放收集音效
        if (!quietMode && clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);

        // 触觉反馈(仅移动端;WebGL 不支持震动,跳过)
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif

        RefreshRibbon();
    }

    /// <summary>
    /// 刷新尾羽 ribbon:点亮已收集的槽位,不销毁任何对象。
    /// 若 allRibbons 配置了多个 Ribbon,则全部同步更新。
    /// </summary>
    private void RefreshRibbon()
    {
        // 把主 ribbon 也加入刷新列表
        List<Transform> targets = new List<Transform>();
        if (ribbonSlotParent != null) targets.Add(ribbonSlotParent);
        if (allRibbons != null)
        {
            foreach (Transform r in allRibbons)
            {
                if (r != null && !targets.Contains(r)) targets.Add(r);
            }
        }

        foreach (Transform ribbon in targets)
        {
            for (int i = 0; i < ribbon.childCount; i++)
            {
                Transform slot = ribbon.GetChild(i);
                Image img = slot.GetComponent<Image>();
                if (img == null) img = slot.GetComponentInChildren<Image>();

                if (img != null)
                {
                    img.color = (i < collectedSounds.Count) ? litColor : dimColor;
                }
            }
        }

        if (foundCounterText != null)
            foundCounterText.text = collectedSounds.Count + " of " + MAX_SLOTS + " sounds found";
    }

    /// <summary>
    /// 合成并播放结尾合唱(按收集顺序 layering)。
    /// </summary>
    public void PlayFinalChorus(List<AudioClip> chorusLayers)
    {
        if (quietMode)
            return;

        StartCoroutine(LayerChorus(chorusLayers));
    }

    private IEnumerator LayerChorus(List<AudioClip> layers)
    {
        if (layers == null) yield break;
        foreach (AudioClip clip in layers)
        {
            if (clip != null && chorusSource != null)
                chorusSource.PlayOneShot(clip);
            yield return new WaitForSeconds(0.4f);
        }
    }

    /// <summary>
    /// 重玩:清空收集,回到场景1。
    /// </summary>
    public void ReadAgain()
    {
        collectedSounds.Clear();
        RefreshRibbon();
        ShowPage("scene1");
    }

    public List<string> GetCollectedSounds()
    {
        return collectedSounds;
    }

    public int GetCollectedCount()
    {
        return collectedSounds.Count;
    }
}
