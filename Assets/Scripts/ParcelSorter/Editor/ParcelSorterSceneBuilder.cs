using TMPro;
using System;
using System.IO;
using System.Linq;
using ParcelSorter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Object = UnityEngine.Object;

public static partial class ParcelSorterSceneBuilder
{
    internal const string ScenePath = "Assets/Scenes/ParcelSorter.unity";
    private const string Prefabs = "Assets/Prefabs/ParcelSorter/";
    private const string Materials = "Assets/Materials/ParcelSorter/";
    private const string Rig = "Assets/Plugins/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private static readonly string[] labels = { "서울", "부산", "대구", "광주" };
    private static readonly Color32[] colors =
    {
        new Color32(201, 62, 58, 255), new Color32(44, 105, 199, 255),
        new Color32(40, 146, 90, 255), new Color32(232, 180, 54, 255)
    };
    private static TMP_FontAsset font;
    private static Material charcoal;
    private static Material steel;
    private static Material accent;

    [MenuItem("Tools/Parcel Sorter/Build Scene")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Prefabs);
        Directory.CreateDirectory(Materials);
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        font = BuildFont();
        charcoal = Paint("Graphite", new Color32(30, 45, 54, 255));
        steel = Paint("Steel", new Color32(79, 101, 105, 255));
        accent = Paint("Amber", new Color32(255, 176, 49, 255));
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Rig));
        rig.GetComponent<Unity.XR.CoreUtils.XROrigin>().RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
        Transform locomotion = rig.GetComponentsInChildren<Transform>(true).Single(child => child.name == "Locomotion");
        locomotion.gameObject.SetActive(false);
        GameObject systems = new GameObject("Parcel Sorter");
        systems.AddComponent<XRInteractionManager>();
        GameManager game = systems.AddComponent<GameManager>();
        ParcelSpawner spawner = systems.AddComponent<ParcelSpawner>();
        GameUIManager ui = systems.AddComponent<GameUIManager>();
        FeedbackManager feedback = systems.AddComponent<FeedbackManager>();
        AudioSource source = systems.AddComponent<AudioSource>();
        source.playOnAwake = false;
        Link(feedback, "source", source);
        Links(feedback, "clips", ParcelSorterAudioGenerator.Generate());
        Transform inventory = Node("Active Parcels", systems.transform, Vector3.zero).transform;
        Transform spawn = Node("Belt Infeed", systems.transform, new Vector3(-1.15f, 1f, 0.55f)).transform;
        ParcelItem[] parcels = new ParcelItem[4];
        SortingSocket[] sockets = new SortingSocket[4];
        for (int i = 0; i < 4; i++)
        {
            Material paint = Paint(((RegionType)i).ToString(), colors[i]);
            parcels[i] = BuildParcel(i, paint);
            sockets[i] = BuildSocket(i, paint, game);
        }
        Link(game, "feedback", feedback);
        Link(game, "ui", ui);
        Link(game, "spawner", spawner);
        Links(spawner, "parcels", parcels);
        Links(game, "sockets", sockets);
        Link(spawner, "inventory", inventory);
        Link(spawner, "spawn", spawn);
        Transform station = Station(game);
        Interface(ui, game);
        ConfigureRoundControls(ui, rig);
        DressWarehouse(station);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettingsScene[] others = EditorBuildSettings.scenes.Where(entry => entry.path != ScenePath).ToArray();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(others).ToArray();
        PlayerSettings.productName = "택배 분류소 (Parcel Sorter)";
        ConfigureQuest();
        AssetDatabase.SaveAssets();
        Debug.Log("Parcel Sorter scene generated.");
    }

    private static void ConfigureQuest()
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        foreach (OpenXRFeature feature in settings.GetFeatures())
        {
            SerializedObject data = new SerializedObject(feature);
            string id = data.FindProperty("featureIdInternal").stringValue;
            if (id.StartsWith("com.unity.openxr.feature.arfoundation-meta-", StringComparison.Ordinal)
                || id == "com.unity.openxr.feature.androidxr-support"
                || id == "com.unity.openxr.feature.meta-boundary-visibility")
            {
                feature.enabled = false;
                EditorUtility.SetDirty(feature);
            }
            if (feature is FoveatedRenderingFeature)
            {
                data.FindProperty("useEyeTracking").boolValue = false;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static Material Paint(string name, Color color)
    {
        string path = Materials + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static TMP_FontAsset BuildFont()
    {
        const string path = "Assets/Fonts/ParcelSorter/DoHyeon SDF.asset";
        TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset == null)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ParcelSorter/DoHyeon-Regular.ttf");
            asset = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            asset.name = "DoHyeon SDF";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (Texture2D atlas in asset.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, asset);
        }
        SerializedObject serialized = new SerializedObject(asset);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        string glyphs = "택배 분류소 제작 여기에 본인 이름 입력 트리거로 상자를 잡고 같은 지역 칸에 놓으세요 서울 부산 대구 광주 초 점 정답 처리 콤보 완료 등급 정확도 최고 기록";
        glyphs += new string(Enumerable.Range(32, 95).Select(code => (char)code).ToArray());
        string unbaked = new string(glyphs.Distinct().Where(character => asset.HasCharacter(character) == false).ToArray());
        if (unbaked.Length > 0 && asset.TryAddCharacters(unbaked, out string missing) == false)
            throw new InvalidOperationException("Missing typeface characters: " + missing);
        EditorUtility.SetDirty(asset.atlasTexture);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static GameObject Node(string name, Transform parent, Vector3 position)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = position;
        return node;
    }

    private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = size;
        block.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(block.GetComponent<Collider>());
        return block;
    }

    private static void Link(Object owner, string name, Object target)
    {
        SerializedObject serialized = new SerializedObject(owner);
        serialized.FindProperty(name).objectReferenceValue = target;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Links(Object owner, string name, Object[] targets)
    {
        SerializedObject serialized = new SerializedObject(owner);
        SerializedProperty property = serialized.FindProperty(name);
        property.arraySize = targets.Length;
        for (int i = 0; i < targets.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Region(Object owner, int index)
    {
        SerializedObject serialized = new SerializedObject(owner);
        serialized.FindProperty("region").enumValueIndex = index;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI Label(string name, Transform parent, string value, Vector2 size, int fontSize)
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        node.transform.SetParent(parent, false);
        TextMeshProUGUI text = node.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.text = value;
        text.color = Color.white;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = size;
        return text;
    }

    private static Canvas CanvasAt(string name, Transform parent, Vector3 pos, float scale, Vector2 size)
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        node.transform.SetParent(parent, false);
        node.transform.localPosition = pos;
        node.transform.localScale = Vector3.one * scale;
        Canvas canvas = node.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ((RectTransform)node.transform).sizeDelta = size;
        return canvas;
    }

    private static TextMeshProUGUI Sign(Transform parent, string value, Vector3 pos, Quaternion rotation, float scale)
    {
        Vector2 size = new Vector2(150, 64);
        Canvas canvas = CanvasAt("Label " + value, parent, pos, scale, size);
        canvas.transform.localRotation = rotation;
        TextMeshProUGUI text = Label("Print", canvas.transform, value, size, 44);
        text.color = charcoal.color;
        return text;
    }

    private static GameObject SavePrefab(GameObject obj, string name)
    {
        string path = Prefabs + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static ParcelItem BuildParcel(int index, Material paint)
    {
        string name = ((RegionType)index).ToString();
        GameObject box = new GameObject(name + " Parcel");
        BoxCollider collider = box.AddComponent<BoxCollider>();
        collider.size = Vector3.one * 0.17f;
        Prop(box.transform, "SM_Storage_Box", Vector3.zero, collider.size * 0.97f, 0f);
        for (int face = 0; face < 5; face++)
        {
            Quaternion rotation = face == 4 ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, face * 90f, 0f);
            Vector3 normal = rotation * Vector3.back;
            GameObject sticker = Block("Routing tag", box.transform, normal * 0.0852f, new Vector3(0.113f, 0.051f, 0.001f), paint);
            sticker.transform.localRotation = rotation;
            Sign(box.transform, labels[index], normal * 0.086f, rotation, 0.00078f).color = index == 3 ? charcoal.color : Color.white;
        }
        Rigidbody body = box.AddComponent<Rigidbody>();
        body.mass = 0.4f;
        body.sleepThreshold = 0f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        XRGrabInteractable grab = box.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.unparentTransformOnGrab = false;
        grab.trackScale = false;
        grab.colliders.Add(collider);
        ParcelItem item = box.AddComponent<ParcelItem>();
        Region(item, index);
        Link(item, "grab", grab);
        Link(item, "body", body);
        return SavePrefab(box, name).GetComponent<ParcelItem>();
    }

    private static SortingSocket BuildSocket(int index, Material paint, GameManager game)
    {
        string name = ((RegionType)index).ToString();
        GameObject bin = new GameObject(name + " Bin");
        BoxCollider trigger = bin.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(0.24f, 0.22f, 0.22f);
        SortingSocket socket = bin.AddComponent<SortingSocket>();
        socket.recycleDelayTime = 0f;
        Region(socket, index);
        Prop(bin.transform, "SM_Storage_Crate", new Vector3(0f, -0.13f, -0.004f), new Vector3(0.269f, 0.079f, 0.246f), 0f);
        Block("Intake liner", bin.transform, new Vector3(0, -0.092f, 0.006f), new Vector3(0.244f, 0.013f, 0.213f), charcoal);
        foreach (float side in new[] { -1f, 1f })
            Block("Intake guide", bin.transform, new Vector3(side * 0.123f, -0.046f, 0.026f), new Vector3(0.015f, 0.082f, 0.18f), steel);
        GameObject rim = Block("Status rail", bin.transform, new Vector3(0f, -0.087f, -0.12f), new Vector3(0.27f, 0.03f, 0.018f), paint);
        Block("Sign", bin.transform, new Vector3(0f, 0.10f, 0.10f), new Vector3(0.27f, 0.095f, 0.018f), paint);
        Sign(bin.transform, labels[index], new Vector3(0, 0.10f, 0.088f), Quaternion.identity, 0.0015f).color =
            index == 3 ? charcoal.color : Color.white;
        ParticleSystem burst = Node("Sort burst", bin.transform, Vector3.zero).AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = burst.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startSpeed = 0.45f;
        main.startSize = 0.018f;
        main.startLifetime = 0.25f;
        main.maxParticles = 12;
        ParticleSystem.EmissionModule emission = burst.emission;
        emission.enabled = false;
        burst.GetComponent<ParticleSystemRenderer>().sharedMaterial = paint;
        Link(socket, "burst", burst);
        Link(socket, "rim", rim.GetComponent<Renderer>());
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(SavePrefab(bin, name + " Bin"));
        float x = (index - 1.5f) * 0.29f;
        instance.transform.position = new Vector3(x, 1.22f, 0.91f - Mathf.Abs(x) * 0.2f);
        socket = instance.GetComponent<SortingSocket>();
        Link(socket, "game", game);
        return socket;
    }

    private static Transform Station(GameManager game)
    {
        GameObject station = new GameObject("Sorting Station");
        Transform root = station.transform;
        GameObject floor = Block("Warehouse floor", root, new Vector3(0, -0.04f, 1), new Vector3(8, 0.08f, 8), steel);
        floor.AddComponent<BoxCollider>();
        Block("Back wall", root, new Vector3(0, 1.5f, 3.8f), new Vector3(8, 3, 0.1f), charcoal);
        for (int i = -3; i <= 3; i++) Block("Warehouse column", root, new Vector3(i, 1.5f, 3.70f), new Vector3(0.08f, 3, 0.14f), steel);
        Light light = Node("Warehouse light", root, Vector3.up * 3f).AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.shadows = LightShadows.None;
        Material tread = Paint("Belt", Color.white);
        Texture2D pattern = new Texture2D(32, 4, TextureFormat.RGB24, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 32; x++)
                pattern.SetPixel(x, y, x < 3 ? steel.color : charcoal.color);
        pattern.Apply();
        string texturePath = Materials + "Tread.png";
        File.WriteAllBytes(texturePath, pattern.EncodeToPNG());
        Object.DestroyImmediate(pattern);
        AssetDatabase.ImportAsset(texturePath);
        tread.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        tread.mainTextureScale = new Vector2(12, 1);
        GameObject belt = Block("Conveyor", root, new Vector3(0, 0.81f, 0.55f), new Vector3(2.6f, 0.08f, 0.33f), tread);
        belt.AddComponent<BoxCollider>();
        ConveyorBelt conveyor = belt.AddComponent<ConveyorBelt>();
        Link(conveyor, "surface", belt.GetComponent<Renderer>());
        Link(conveyor, "game", game);
        foreach (float x in new[] { -1f, 1f }) Block("Support", root, new Vector3(x, 0.4f, 0.55f), new Vector3(0.1f, 0.8f, 0.26f), steel);
        GameObject chute = Node("Floor collection", root, new Vector3(0, 0.18f, 0));
        BoxCollider zone = chute.AddComponent<BoxCollider>();
        zone.size = new Vector3(30, 0.42f, 30);
        zone.isTrigger = true;
        Link(chute.AddComponent<MissZone>(), "game", game);
        GameObject outfeed = Node("Outfeed chute", root, new Vector3(1.50f, 0.57f, 0.55f));
        BoxCollider mouth = outfeed.AddComponent<BoxCollider>();
        mouth.size = new Vector3(0.44f, 0.15f, 0.5f);
        mouth.isTrigger = true;
        Link(outfeed.AddComponent<MissZone>(), "game", game);
        return root;
    }

    private static void Interface(GameUIManager ui, GameManager game)
    {
        new GameObject("XR UI Events", typeof(EventSystem), typeof(XRUIInputModule));
        Canvas panel = CanvasAt("Dispatch Board", null, new Vector3(0, 1.6f, 1.4f), 0.001f, new Vector2(840, 330));
        panel.worldCamera = Camera.main;
        panel.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        Image backing = panel.gameObject.AddComponent<Image>();
        backing.color = new Color32(247, 252, 251, 255);
        backing.raycastTarget = false;
        Color ink = new Color32(20, 47, 55, 255);
        Color muted = new Color32(59, 87, 94, 255);
        Color teal = new Color32(0, 109, 106, 255);
        BoardFill("Header rule", panel.transform, new Vector2(0, 162f), new Vector2(840, 6), teal);
        TextMeshProUGUI section = BoardLabel("Section", panel.transform, new Vector2(0, 118), new Vector2(760, 36), 25, teal);
        section.fontStyle = FontStyles.Bold;
        TextMeshProUGUI message = BoardLabel("Board text", panel.transform, new Vector2(0, 57), new Vector2(770, 83), 64, ink);
        TextMeshProUGUI detail = BoardLabel("Detail", panel.transform, Vector2.down * 21, new Vector2(770, 76), 30, muted);
        Image progress = BoardFill("Time track", panel.transform, new Vector2(0, -83), new Vector2(690, 12), new Color32(219, 232, 231, 255));
        Image fill = BoardFill("Time remaining", progress.transform, new Vector2(-345, 0), progress.rectTransform.sizeDelta, teal);
        fill.rectTransform.pivot = new Vector2(0, 0.5f);
        GameObject target = new GameObject("Ray action", typeof(RectTransform), typeof(Image), typeof(Button));
        target.transform.SetParent(panel.transform, false);
        RectTransform rect = (RectTransform)target.transform;
        rect.sizeDelta = new Vector2(298, 61);
        rect.anchoredPosition = Vector2.down * 113f;
        target.GetComponent<Image>().color = teal;
        TextMeshProUGUI caption = Label("Action label", rect, "START", rect.sizeDelta, 42);
        caption.color = Color.white;
        GameObject console = new GameObject("Poke console");
        BoxCollider hit = console.AddComponent<BoxCollider>();
        hit.size = new Vector3(0.16f, 0.08f, 0.04f);
        XRSimpleInteractable interactable = console.AddComponent<XRSimpleInteractable>();
        interactable.colliders.Add(hit);
        XRPokeFilter filter = console.AddComponent<XRPokeFilter>();
        filter.pokeInteractable = interactable;
        filter.pokeCollider = hit;
        GameObject cap = Node("Plunger", console.transform, Vector3.zero);
        Block("Amber cap", cap.transform, Vector3.zero, hit.size, accent);
        TextMeshProUGUI legend = Sign(cap.transform, "START", Vector3.back * 0.021f, Quaternion.identity, 0.001f);
        PokeButton3D poke = console.AddComponent<PokeButton3D>();
        Link(poke, "cap", cap.transform);
        Link(poke, "filter", filter);
        Link(poke, "button", interactable);
        Link(poke, "caption", legend);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(SavePrefab(console, "Console"));
        instance.transform.position = new Vector3(0.32f, 0.97f, 0.38f);
        instance.transform.rotation = Quaternion.Euler(40, 0, 0);
        Link(ui, "console", instance.GetComponent<PokeButton3D>());
        Link(ui, "message", message);
        Link(ui, "section", section);
        Link(ui, "detail", detail);
        Link(ui, "progress", fill);
        Link(ui, "caption", caption);
        Link(ui, "action", target.GetComponent<Button>());
        Link(ui, "panel", panel.transform);
        Link(ui, "game", game);
    }

    private static TextMeshProUGUI BoardLabel
    (
        string name, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color
    )
    {
        TextMeshProUGUI text = Label(name, parent, "", size, fontSize);
        text.rectTransform.anchoredPosition = position;
        text.color = color;
        return text;
    }

    private static Image BoardFill(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject node = new GameObject(name, typeof(RectTransform), typeof(Image));
        node.transform.SetParent(parent, false);
        Image image = node.GetComponent<Image>();
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
