using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// BuildFinal - 一次性干净构建脚本。
/// 从 _Recovery/0.unity 的干净场景出发,完成:
///   1. 导入 Assets/Sprites 下的全部素材为 Sprite
///   2. 应用 6 页背景(Page_4 用 BG_Dawn)
///   3. 应用图标(声音珠/树叶/灯笼/羽毛按钮/花瓣/Echo 主角)
///   4. 挂载全部交互脚本(SoundBead/HoldLeaf/Lantern/SongRibbon/PageTurnButton/BreathingButton/DriftingPetal/FadeTransition)
///   5. 配置 GameManager 的页面引用与 ribbon 槽位
///   6. 保存场景
/// 幂等:可重复执行。
/// </summary>
public static class BuildFinal
{
    // 六色板(设计规范)
    private static readonly Color Ink        = Hex("#1F2E28");
    private static readonly Color WattleGold = Hex("#E3A73C");
    private static readonly Color EmberRust  = Hex("#C75B39");
    private static readonly Color GumMist    = Hex("#7E9C8C");
    private static readonly Color GullyPaper = Hex("#EEF2E6");
    private static readonly Color ShadowFern = Hex("#5C6B60");

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }

    [MenuItem("Echo/Build Final Scene")]
    public static void Run()
    {
        // 1. 确保场景已打开
        string scenePath = "Assets/Scenes/Main.unity";
        var current = EditorSceneManager.GetActiveScene();
        if (current.path != scenePath)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError("[BuildFinal] 找不到场景 " + scenePath);
                return;
            }
            current = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        // 2. 导入素材
        ImportSprites();
        ImportAudio();

        // 3. 查找根对象
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("[BuildFinal] 找不到 Canvas");
            return;
        }

        ApplyBackgrounds(canvasGO);
        ApplyIcons(canvasGO);
        WireInteractions(canvasGO);
        WireGameManager(canvasGO);
        ApplyAudio(canvasGO);

        // 4. 保存(强制刷新 active scene 引用,并用显式路径保存)
        current = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(current);
        bool saved = EditorSceneManager.SaveScene(current, scenePath, false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BuildFinal] ✅ 完成!场景已保存:" + scenePath + " (saved=" + saved + ", dirty=" + current.isDirty + ")");
    }

    private static void ImportSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            TextureImporter ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;

            bool changed = false;
            if (ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            // 关键:强制 Single 模式,避免 AI 素材被自动切片成多个小 sprite
            if (ti.spriteImportMode != SpriteImportMode.Single)
            {
                ti.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!ti.alphaIsTransparency)
            {
                ti.alphaIsTransparency = true;
                changed = true;
            }
            if (ti.mipmapEnabled)
            {
                ti.mipmapEnabled = false;
                changed = true;
            }
            // 关闭自动切片(关键!AI 图的透明区域会被误判为多个 sprite)
            ti.spritePixelsPerUnit = 100;
            if (changed)
            {
                ti.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("[BuildFinal] 素材导入完成,共 " + guids.Length + " 张");
    }

    private static Sprite LoadSprite(string fileName)
    {
        string p = "Assets/Sprites/" + fileName;
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    private static void ImportAudio()
    {
        // 显式导入所有音频,确保 clip 就绪(避免刚放入时加载为 null)
        string[] files = System.IO.Directory.GetFiles("Assets/Audio", "*.wav");
        foreach (string f in files)
        {
            AssetDatabase.ImportAsset(f, ImportAssetOptions.ForceUpdate);
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            AudioImporter ai = AssetImporter.GetAtPath(p) as AudioImporter;
            if (ai == null) continue;
            // 2D 音效(不随位置衰减),适合 UI 交互
            if (ai.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                var s = ai.defaultSampleSettings;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.PCM;
                ai.defaultSampleSettings = s;
                ai.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("[BuildFinal] 音频导入完成,共 " + guids.Length + " 个");
    }

    private static AudioClip LoadClip(string fileName)
    {
        string p = "Assets/Audio/" + fileName;
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
        if (clip == null)
            Debug.LogWarning("[BuildFinal] 音频加载失败: " + p);
        return clip;
    }

    // 递归查找子物体(按名字,精确匹配)
    private static GameObject FindChild(GameObject root, string name)
    {
        Transform t = root.transform.Find(name);
        if (t != null) return t.gameObject;

        foreach (Transform child in root.transform)
        {
            GameObject r = FindChild(child.gameObject, name);
            if (r != null) return r;
        }
        return null;
    }

    private static void SetSprite(GameObject go, Sprite sp)
    {
        if (go == null || sp == null) return;
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = sp;
        img.color = Color.white;
        img.raycastTarget = false;
    }

    private static void ApplyBackgrounds(GameObject canvasGO)
    {
        GameObject home = FindChild(canvasGO, "Page_Home");
        GameObject p1 = FindChild(canvasGO, "Page_1_Tap");
        GameObject p2 = FindChild(canvasGO, "Page_2_Shelter");
        GameObject p3 = FindChild(canvasGO, "Page_3_Thunder");
        GameObject p4 = FindChild(canvasGO, "Page_4_Song");
        GameObject pc = FindChild(canvasGO, "Page_Credits");

        SetSprite(FindChild(home, "BG"), LoadSprite("BG_首页_黄昏山谷.png"));
        SetSprite(FindChild(p1, "BG"),   LoadSprite("BG_场景1_清晨山谷.png"));
        SetSprite(FindChild(p2, "BG"),   LoadSprite("BG_场景2_暴雨来临.png"));
        SetSprite(FindChild(p3, "BG"),   LoadSprite("BG_场景3_夜晚灯笼.png"));
        // Page_4 的背景对象叫 BG_Dawn(不是 BG)
        SetSprite(FindChild(p4, "BG_Dawn"), LoadSprite("BG_场景4_黎明山脊.png"));
        SetSprite(FindChild(pc, "BG"),   LoadSprite("BG_首页_黄昏山谷.png"));

        Debug.Log("[BuildFinal] 背景已应用");
    }

    private static void ApplyIcons(GameObject canvasGO)
    {
        Sprite bead = LoadSprite("UI_声音珠.png");
        Sprite leaf = LoadSprite("UI_树叶.png");
        Sprite lantern = LoadSprite("UI_灯笼.png");
        Sprite feather = LoadSprite("UI_羽毛按钮.png");
        Sprite petal = LoadSprite("UI_金合欢花瓣.png");
        Sprite echo = LoadSprite("Echo_主角.png");

        // 声音珠(所有 Bead_*)
        foreach (string n in new[] {
            "Bead_wind","Bead_dew","Bead_magpie",
            "Bead_rail","Bead_ridge","Bead_fern",
            "Bead_lantern","Bead_thunder","Bead_laugh"
        })
        {
            GameObject b = FindChild(canvasGO, n);
            if (b != null) SetSprite(b, bead);
        }

        // 主角 Echo
        GameObject echoGO = FindChild(canvasGO, "Echo");
        if (echoGO != null) SetSprite(echoGO, echo);

        // 树叶(场景2的交互树叶,名字可能是 Bead_rail/ridge/fern 或单独树叶对象)
        // 灯笼(场景3的 lantern bead 用灯笼图标)
        GameObject lanternBead = FindChild(canvasGO, "Bead_lantern");
        if (lanternBead != null) SetSprite(lanternBead, lantern);

        // 羽毛按钮(Btn_Begin / Btn_Next / Btn_Credits / Btn_ReadAgain)
        foreach (string n in new[] { "Btn_Begin","Btn_Next","Btn_Credits","Btn_ReadAgain" })
        {
            GameObject b = FindChild(canvasGO, n);
            if (b != null)
            {
                Image img = b.GetComponent<Image>();
                if (img != null) img.sprite = feather;
            }
        }

        Debug.Log("[BuildFinal] 图标已应用");
    }

    private static void WireInteractions(GameObject canvasGO)
    {
        // 声音珠 → SoundBead
        var beadIds = new Dictionary<string,string> {
            {"Bead_wind","wind"}, {"Bead_dew","dew"}, {"Bead_magpie","magpie"},
            {"Bead_rail","rain"}, {"Bead_ridge","ridge"}, {"Bead_fern","fern"},
            {"Bead_lantern","lantern"}, {"Bead_thunder","thunder"}, {"Bead_laugh","laugh"}
        };
        foreach (var kv in beadIds)
        {
            GameObject b = FindChild(canvasGO, kv.Key);
            if (b == null) continue;
            Image img = b.GetComponent<Image>();
            if (img != null) img.raycastTarget = true; // 确保可点击
            if (b.GetComponent<SoundBead>() == null)
            {
                var sb = b.AddComponent<SoundBead>();
                sb.soundId = kv.Value;
            }
        }

        // 场景2 长按树叶 → HoldLeaf(用 Bead_rail 作为树叶屋顶)
        GameObject rail = FindChild(canvasGO, "Bead_rail");
        if (rail != null && rail.GetComponent<HoldLeaf>() == null)
        {
            var hl = rail.AddComponent<HoldLeaf>();
            hl.soundId = "rain";
            hl.leafRect = rail.GetComponent<RectTransform>();
        }

        // 场景3 灯笼 → Lantern
        GameObject lanternBead = FindChild(canvasGO, "Bead_lantern");
        if (lanternBead != null && lanternBead.GetComponent<Lantern>() == null)
        {
            lanternBead.AddComponent<Lantern>();
        }

        // 场景4 SongRibbon → SongRibbon 上滑释放
        GameObject songRibbon = FindChild(canvasGO, "SongRibbon");
        if (songRibbon != null)
        {
            Image img = songRibbon.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            if (songRibbon.GetComponent<SongRibbon>() == null)
                songRibbon.AddComponent<SongRibbon>();
        }

        // 翻页按钮 → PageTurnButton
        WirePageTurn(canvasGO);

        // 首页 Begin 按钮 → BreathingButton
        GameObject begin = FindChild(canvasGO, "Btn_Begin");
        if (begin != null && begin.GetComponent<BreathingButton>() == null)
            begin.AddComponent<BreathingButton>();

        Debug.Log("[BuildFinal] 交互脚本已挂载");
    }

    private static void WirePageTurn(GameObject canvasGO)
    {
        // 映射:每个页面内的 Btn_Next/Btn_Begin 跳到哪个页面
        // 用「按钮所在页面的父页面」来判定跳转目标

        // 首页 Btn_Begin → scene1
        WireButton(FindChild(canvasGO, "Btn_Begin"), "scene1");

        // 首页 Btn_Credits → credits
        WireButton(FindChild(canvasGO, "Btn_Credits"), "credits");

        // Page_1 Btn_Next → scene2
        WireButton(FindNextOfPage(canvasGO, "Page_1_Tap"), "scene2");
        // Page_2 Btn_Next → scene3
        WireButton(FindNextOfPage(canvasGO, "Page_2_Shelter"), "scene3");
        // Page_3 Btn_Next → scene4
        WireButton(FindNextOfPage(canvasGO, "Page_3_Thunder"), "scene4");
        // Page_4 Btn_Next → credits
        WireButton(FindNextOfPage(canvasGO, "Page_4_Song"), "credits");

        // 返回按钮(所有 Btn_Back 默认回上一页,简化:统一回 home;实际按需)
        // 这里按页面精确绑定:
        WireBack(FindChild(canvasGO, "Page_1_Tap"), "home");
        WireBack(FindChild(canvasGO, "Page_2_Shelter"), "scene1");
        WireBack(FindChild(canvasGO, "Page_3_Thunder"), "scene2");
        WireBack(FindChild(canvasGO, "Page_4_Song"), "scene3");
        WireBack(FindChild(canvasGO, "Page_Credits"), "scene4");

        // Credits Btn_ReadAgain → scene1
        WireButton(FindChild(canvasGO, "Btn_ReadAgain"), "scene1");
    }

    private static GameObject FindNextOfPage(GameObject canvasGO, string pageName)
    {
        GameObject page = FindChild(canvasGO, pageName);
        if (page == null) return null;
        GameObject btn = FindChild(page, "Btn_Next");
        return btn;
    }

    private static void WireButton(GameObject btn, string targetPage)
    {
        if (btn == null) return;
        if (btn.GetComponent<PageTurnButton>() == null)
        {
            var pt = btn.AddComponent<PageTurnButton>();
            pt.nextPageName = targetPage;
        }
    }

    private static void WireBack(GameObject page, string targetPage)
    {
        if (page == null) return;
        GameObject btn = FindChild(page, "Btn_Back");
        WireButton(btn, targetPage);
    }

    private static void WireGameManager(GameObject canvasGO)
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            // 创建 GameManager 物体
            GameObject gmGO = new GameObject("GameManager");
            gm = gmGO.AddComponent<GameManager>();
        }

        gm.homePage    = FindChild(canvasGO, "Page_Home");
        gm.scene1Page  = FindChild(canvasGO, "Page_1_Tap");
        gm.scene2Page  = FindChild(canvasGO, "Page_2_Shelter");
        gm.scene3Page  = FindChild(canvasGO, "Page_3_Thunder");
        gm.scene4Page  = FindChild(canvasGO, "Page_4_Song");
        gm.creditsPage = FindChild(canvasGO, "Page_Credits");

        // ribbon 槽位(用首页的 Ribbon)
        GameObject ribbon = FindChild(canvasGO, "Ribbon");
        if (ribbon != null)
        {
            gm.ribbonSlotParent = ribbon.transform;
            gm.allRibbons.Clear();
            // 收集所有页面的 Ribbon(同名)
            foreach (var r in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (r.name == "Ribbon" || r.name == "SongRibbon")
                    gm.allRibbons.Add(r);
            }
        }

        Debug.Log("[BuildFinal] GameManager 已配置");
    }

    private static void ApplyAudio(GameObject canvasGO)
    {
        // 声音珠音效映射(soundId -> 文件名)
        var clipMap = new Dictionary<string,string> {
            {"wind","bead_wind"}, {"dew","bead_dew"}, {"magpie","bead_magpie"},
            {"rain","bead_rain"}, {"ridge","bead_ridge"}, {"fern","bead_fern"},
            {"lantern","bead_lantern"}, {"thunder","bead_thunder"}, {"laugh","bead_laugh"}
        };

        // 1. 给每个 SoundBead 赋 collectClip
        // 关键:珠子在 Page_1/2/3 里,这些页面初始非激活,必须用按名字递归查找(不依赖激活状态)
        int beadCount = 0;
        foreach (var kv in new Dictionary<string,string> {
            {"Bead_wind","wind"}, {"Bead_dew","dew"}, {"Bead_magpie","magpie"},
            {"Bead_rail","rain"}, {"Bead_ridge","ridge"}, {"Bead_fern","fern"},
            {"Bead_lantern","lantern"}, {"Bead_thunder","thunder"}, {"Bead_laugh","laugh"}
        })
        {
            GameObject b = FindChild(canvasGO, kv.Key);
            if (b == null) continue;
            SoundBead sb = b.GetComponent<SoundBead>();
            if (sb == null) continue;
            if (clipMap.TryGetValue(kv.Value, out string fn))
                sb.collectClip = LoadClip(fn + ".wav");
            beadCount++;
        }

        // 2. HoldLeaf → rainClip
        GameObject rail = FindChild(canvasGO, "Bead_rail");
        if (rail != null)
        {
            HoldLeaf holdLeaf = rail.GetComponent<HoldLeaf>();
            if (holdLeaf != null)
                holdLeaf.rainClip = LoadClip("bead_rain.wav");
        }

        // 3. Lantern → lanternClip
        GameObject lanternGO = FindChild(canvasGO, "Bead_lantern");
        if (lanternGO != null)
        {
            Lantern lantern = lanternGO.GetComponent<Lantern>();
            if (lantern != null)
                lantern.lanternClip = LoadClip("bead_lantern.wav");
        }

        // 4. PageTurnButton → pageTurnClip
        AudioClip pageClip = LoadClip("ui_page_turn.wav");
        foreach (string n in new[] { "Btn_Begin","Btn_Next","Btn_Credits","Btn_ReadAgain","Btn_Back" })
        {
            var found = FindAllChildren(canvasGO, n);
            foreach (var btn in found)
            {
                PageTurnButton pt = btn.GetComponent<PageTurnButton>();
                if (pt != null) pt.pageTurnClip = pageClip;
            }
        }

        // 5. SongRibbon → chorusLayers(结尾合唱)
        GameObject songRibbonGO = FindChild(canvasGO, "SongRibbon");
        if (songRibbonGO != null)
        {
            SongRibbon songRibbon = songRibbonGO.GetComponent<SongRibbon>();
            if (songRibbon != null)
                songRibbon.chorusLayers = new List<AudioClip> { LoadClip("chorus_final.wav") };
        }

        // 6. GameManager → 三个 AudioSource(sfx/ambience/chorus)
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            // GameManager 挂在场景根的 GameManager 物体上,应能找到;找不到就按名字找
            GameObject gmGO = GameObject.Find("GameManager");
            if (gmGO != null) gm = gmGO.GetComponent<GameManager>();
        }
        if (gm != null)
        {
            gm.sfxSource = EnsureAudioSource(gm.gameObject, "SfxSource");
            gm.ambienceSource = EnsureAudioSource(gm.gameObject, "AmbienceSource");
            gm.chorusSource = EnsureAudioSource(gm.gameObject, "ChorusSource");

            if (gm.ambienceSource != null)
            {
                gm.ambienceSource.clip = LoadClip("ambience_forest.wav");
                gm.ambienceSource.loop = true;
                gm.ambienceSource.volume = 0.12f; // 很轻的背景底噪,不抢珠子音效
                gm.ambienceSource.playOnAwake = true;
            }
        }

        Debug.Log("[BuildFinal] 音频已挂载(声音珠 " + beadCount + " 个)");
    }

    // 查找所有同名子物体(返回列表,不限于第一个)
    private static List<GameObject> FindAllChildren(GameObject root, string name)
    {
        List<GameObject> result = new List<GameObject>();
        FindAllChildrenRecursive(root, name, result);
        return result;
    }

    private static void FindAllChildrenRecursive(GameObject node, string name, List<GameObject> result)
    {
        foreach (Transform child in node.transform)
        {
            if (child.name == name)
                result.Add(child.gameObject);
            FindAllChildrenRecursive(child.gameObject, name, result);
        }
    }

    // 为 GameManager 的某个 AudioSource 字段,查找/创建独立子物体上的 AudioSource
    private static AudioSource EnsureAudioSource(GameObject go, string name)
    {
        // 找子物体里同名的 AudioSource
        Transform child = go.transform.Find(name);
        if (child != null)
        {
            AudioSource existing = child.GetComponent<AudioSource>();
            if (existing != null) return existing;
        }

        GameObject sub = new GameObject(name);
        sub.transform.SetParent(go.transform, false);
        AudioSource src = sub.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        src.loop = false;
        return src;
    }
}
