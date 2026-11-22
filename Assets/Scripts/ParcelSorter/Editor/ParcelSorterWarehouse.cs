using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class ParcelSorterSceneBuilder
{
    private const string Ozea = "Assets/Models/OZEA_STUDIO_ULTIMATE";

    [MenuItem("Tools/Parcel Sorter/Refresh Warehouse Art")]
    public static void RefreshWarehouseArt()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ScenePath);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ParcelSorter/DoHyeon SDF.asset");
        charcoal = AssetDatabase.LoadAssetAtPath<Material>(Materials + "Graphite.mat");
        DressWarehouse(scene.GetRootGameObjects().Single(root => root.name == "Sorting Station").transform);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void DressWarehouse(Transform station)
    {
        Transform previous = station.Find("Dispatch hall");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        Transform hall = Node("Dispatch hall", station, Vector3.zero).transform;
        Material concrete = Paint("Hall Concrete", new Color32(191, 205, 207, 255));
        Material panel = Paint("Hall Panel", new Color32(230, 239, 237, 255));
        Material frame = Paint("Hall Frame", new Color32(37, 54, 61, 255));
        Material yellow = Paint("Safety Amber", new Color32(231, 157, 56, 255));
        Material mint = Paint("Status Mint", new Color32(105, 217, 205, 255));
        Material white = Paint("Fixture White", new Color32(245, 255, 248, 255));

        station.Find("Warehouse floor").GetComponent<Renderer>().sharedMaterial = concrete;
        station.Find("Back wall").GetComponent<Renderer>().sharedMaterial = panel;
        foreach (Transform child in station)
            if (child.name == "Warehouse column") child.gameObject.SetActive(false);
        Light key = station.Find("Warehouse light").GetComponent<Light>();
        key.color = new Color32(255, 249, 229, 255);
        key.intensity = 0.82f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(155, 178, 183, 255);

        Block("Ceiling", hall, new Vector3(0, 3.30f, 1.4f), new Vector3(8, 0.10f, 5.5f), panel);
        for (int x = 0; x < 4; x++)
            for (int z = 0; z < 3; z++)
                Prop(hall, "SM_Floor_V2_Large", new Vector3(-3f + 2f * x, -0.020f, -1.2f + 2f * z), new Vector3(1.996f, 0.05f, 1.996f), 0);
        foreach (int side in new[] { -1, 1 })
        {
            float x = 3.95f * side;
            for (int i = 0; i < 3; i++)
                Prop(hall, "SM_Wall_V1", new Vector3(x, 1.63f, -0.75f + i * 1.8f), new Vector3(0.24f, 3.26f, 1.81f), side * 90f);
            Prop(hall, "SM_Wall_V3", new Vector3(side * 2.65f, 1.67f, 3.61f), new Vector3(2.15f, 2.88f, 0.28f), 180f);
            Prop(hall, "SM_WallDecor_WallConsole_Small", new Vector3(side * 2.65f, 1.46f, 3.42f), new Vector3(0.63f, 0.43f, 0.15f), 0f);
            Block("Aisle line", hall, new Vector3(1.73f * side, 0.005f, 1.35f), new Vector3(0.038f, 0.01f, 3.55f), mint);
            Prop(hall, "SM_Rectangular_Floor_Planter", new Vector3(side * 2.87f, 0.37f, 3.20f), new Vector3(1.28f, 0.74f, 0.48f), 0f);
            Prop(hall, "SM_Pallet", new Vector3(2.25f * side, 0.065f, 2.21f), new Vector3(1.24f, 0.13f, 0.85f), 0f);
            Prop(hall, "SM_Storage_Crate", new Vector3(2.25f * side, 0.375f, 2.21f), new Vector3(1.02f, 0.49f, 0.72f), 0f);
            if (side > 0)
                Prop(hall, "SM_Storage_Box", new Vector3(2.18f, 0.79f, 2.23f), new Vector3(0.68f, 0.34f, 0.55f), 12f);
            if (side < 0)
                Prop(hall, "SM_Terminal_Stand", new Vector3(-1.77f, 0.61f, 2.85f), new Vector3(0.47f, 1.22f, 0.43f), 195f);
            Prop(hall, "SM_Support_Beam_Large", new Vector3(side * 1.42f, 1.47f, 3.57f), new Vector3(0.26f, 2.94f, 0.24f), 0f);
        }

        Block("Door recess", hall, new Vector3(0, 1.28f, 3.67f), new Vector3(2.54f, 2.52f, 0.14f), panel);
        Prop(hall, "SM_Door_Double_V2L", new Vector3(-0.58f, 1.12f, 3.52f), new Vector3(1.15f, 2.22f, 0.16f), 0f);
        Prop(hall, "SM_Door_Double_V2R", new Vector3(0.58f, 1.12f, 3.52f), new Vector3(1.15f, 2.22f, 0.16f), 180f);
        Block("Dispatch sign", hall, new Vector3(0, 2.60f, 3.48f), new Vector3(2.55f, 0.31f, 0.13f), frame);
        Canvas sign = CanvasAt("Dispatch sign lettering", hall, new Vector3(0, 2.60f, 3.405f), 0.003f, new Vector2(780, 85));
        Label("Dispatch", sign.transform, "PARCEL DISPATCH", new Vector2(780, 85), 56).color = white.color;
        Block("Dispatch indicator", hall, new Vector3(0, 2.39f, 3.40f), new Vector3(0.59f, 0.027f, 0.025f), mint);
        for (int i = 0; i < 3; i++)
        {
            float z = -0.2f + 1.55f * i;
            Block("Ceiling girder", hall, new Vector3(0, 3.15f, z), new Vector3(7.83f, 0.16f, 0.22f), frame);
            Block("Ceiling luminaire", hall, new Vector3(0, 3.06f, z), new Vector3(3.2f, 0.035f, 0.10f), white);
        }

        Block("Conveyor chassis", hall, new Vector3(0, 0.735f, 0.55f), new Vector3(2.67f, 0.11f, 0.405f), panel);
        Block("Amber conveyor edge", hall, new Vector3(0, 0.77f, 0.342f), new Vector3(2.64f, 0.025f, 0.018f), yellow);
        Block("Sorting rack fascia", hall, new Vector3(0, 1.056f, 0.91f), new Vector3(1.20f, 0.079f, 0.33f), frame);
        Prop(hall, "SM_UtilityTable", new Vector3(0, 0.50f, 0.98f), new Vector3(1.25f, 1f, 0.42f), 0f);
        Prop(hall, "SM_WallDecor_TechPanel_Wide", new Vector3(0, 0.47f, 0.40f), new Vector3(1.72f, 0.45f, 0.17f), 0f);
        Prop(hall, "SM_Storage_Crate", new Vector3(1.52f, 0.23f, 0.55f), new Vector3(0.48f, 0.46f, 0.47f), 0f);
    }

    private static void Prop(Transform parent, string name, Vector3 center, Vector3 size, float yaw)
    {
        string path = AssetDatabase.FindAssets(name + " t:Prefab", new[] { Ozea })
            .Select(AssetDatabase.GUIDToAssetPath).Single(candidate => candidate.EndsWith("/" + name + ".prefab", StringComparison.Ordinal));
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Transform mount = Node(name.Substring(3).Replace('_', ' '), parent, Vector3.zero).transform;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mount);
        instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            bounds.Encapsulate(renderer.bounds);
        }
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
        mount.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        mount.localPosition = center - Vector3.Scale(mount.localScale, bounds.center - parent.position);
    }
}
