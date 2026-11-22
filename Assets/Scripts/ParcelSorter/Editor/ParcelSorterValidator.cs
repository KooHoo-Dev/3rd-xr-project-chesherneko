using TMPro;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ParcelSorter;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Object = UnityEngine.Object;

public static class ParcelSorterValidator
{
    [MenuItem("Tools/Parcel Sorter/Validate Scene")]
    public static void ValidateAll()
    {
        Scene scene = EditorSceneManager.OpenScene(ParcelSorterSceneBuilder.ScenePath);
        GameObject[] roots = scene.GetRootGameObjects();
        Transform[] nodes = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (Transform node in nodes)
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0, node.name + " missing script");
        One<GameManager>(roots);
        SerializedObject data = new SerializedObject(One<ParcelSpawner>(roots));
        SerializedProperty parcels = data.FindProperty("parcels");
        Require(parcels.arraySize == 4, "four parcel prefabs");
        for (int i = 0; i < parcels.arraySize; i++)
        {
            ParcelItem item = (ParcelItem)parcels.GetArrayElementAtIndex(i).objectReferenceValue;
            Require(item.Region == (RegionType)i && item.GetComponentsInChildren<TextMeshProUGUI>().Length >= 4, "labelled region prefab");
            CheckFields(item);
        }
        foreach (MonoBehaviour script in roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)))
            if (script.GetType().Namespace == "ParcelSorter") CheckFields(script);
        Require(roots.SelectMany(root => root.GetComponentsInChildren<SortingSocket>()).Count() == 4, "four sockets");
        XROrigin rig = One<XROrigin>(roots);
        Require(rig.GetComponentsInChildren<XRPokeInteractor>().Length == 2, "two active poke interactors");
        Require(rig.GetComponentsInChildren<NearFarInteractor>().Count(hand => hand.enableUIInteraction) == 2, "two grab and UI interactors");
        Require(rig.GetComponentsInChildren<XRInteractionGroup>().Length >= 2, "controller interaction groups");
        Require(nodes.Single(node => node.name == "Locomotion").gameObject.activeInHierarchy == false, "stationary rig");
        One<XRInteractionManager>(roots);
        One<XRUIInputModule>(roots);
        One<EventSystem>(roots);
        One<TrackedDeviceGraphicRaycaster>(roots);
        One<ConveyorBelt>(roots);
        One<GameUIManager>(roots);
        One<PokeButton3D>(roots);
        One<XRPokeFilter>(roots);
        Require(EditorBuildSettings.scenes.First(entry => entry.enabled).path == scene.path, "launch scene");
        Require(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android, "Android build target");
        XRGeneralSettings general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        Require(general.Manager.activeLoaders.Any(loader => loader is OpenXRLoader), "Android OpenXR loader");
        OpenXRSettings xr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        Require(xr.GetFeature<MetaQuestFeature>().enabled && xr.GetFeature<OculusTouchControllerProfile>().enabled, "Meta Quest and Touch profiles");
        Require(PlayerSettings.Android.targetArchitectures.HasFlag(AndroidArchitecture.ARM64), "ARM64");
        Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, "IL2CPP");
        Debug.Log($"Parcel validation passed: {nodes.Length} scene nodes, zero missing scripts or required references, Android OpenXR/Quest/Touch enabled.");
    }

    private static T One<T>(GameObject[] roots) where T : Component =>
        roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static void CheckFields(MonoBehaviour script)
    {
        foreach (FieldInfo field in script.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (field.IsDefined(typeof(SerializeField), false) == false) continue;
            if (typeof(Object).IsAssignableFrom(field.FieldType))
                Require((Object)field.GetValue(script) != null, script.name + "." + field.Name);
            if (field.FieldType.IsArray && typeof(Object).IsAssignableFrom(field.FieldType.GetElementType()))
            {
                Object[] entries = (Object[])field.GetValue(script);
                Require(entries.Length > 0 && entries.All(entry => entry != null), field.Name);
            }
        }
    }

    private static void Require(bool valid, string condition)
    {
        if (valid == false) throw new BuildFailedException("Parcel Sorter validation failed: " + condition);
    }

    public static void BuildApk()
    {
        ValidateAll();
        Directory.CreateDirectory("Builds/Android");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ParcelSorterSceneBuilder.ScenePath },
            locationPathName = "Builds/Android/ParcelSorter.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log($"Parcel APK: {report.summary.result}, {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings.");
        Require(report.summary.result == BuildResult.Succeeded, "APK build");
    }
}
