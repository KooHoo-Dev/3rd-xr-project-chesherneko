using System.Linq;
using ParcelSorter;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Object = UnityEngine.Object;

public static partial class ParcelSorterSceneBuilder
{
    [MenuItem("Tools/Parcel Sorter/Refresh Round Controls")]
    public static void RefreshRoundControls()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ScenePath);
        font = BuildFont();
        GameObject[] roots = scene.GetRootGameObjects();
        GameUIManager ui = roots.SelectMany(root => root.GetComponentsInChildren<GameUIManager>(true)).Single();
        GameObject rig = roots.Single(root => root.GetComponent<XROrigin>() != null);
        ConfigureRoundControls(ui, rig);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureRoundControls(GameUIManager ui, GameObject rig)
    {
        foreach (NearFarInteractor hand in rig.GetComponentsInChildren<NearFarInteractor>(true))
        {
            hand.selectInput.inputActionReferencePerformed = hand.activateInput.inputActionReferencePerformed;
            hand.selectInput.inputActionReferenceValue = hand.activateInput.inputActionReferenceValue;
            hand.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            PrefabUtility.RecordPrefabInstancePropertyModifications(hand);
        }

        Transform board = (Transform)new SerializedObject(ui).FindProperty("panel").objectReferenceValue;
        RectTransform start = (RectTransform)board.Find("Ray action");
        start.anchoredPosition = new Vector2(276, -113);
        start.sizeDelta = new Vector2(180, 61);
        Transform oldHard = board.Find("HARD  1.5x");
        if (oldHard != null) Object.DestroyImmediate(oldHard.gameObject);
        oldHard = board.Find("HARD 2.5x");
        if (oldHard != null) Object.DestroyImmediate(oldHard.gameObject);
        Link(ui, "easy", DifficultyButton(board, "EASY", -287));
        Link(ui, "normal", DifficultyButton(board, "NORMAL 1.5x", -109));
        Link(ui, "hard", DifficultyButton(board, "HARD 2x", 69));
    }

    private static Button DifficultyButton(Transform board, string caption, float x)
    {
        Transform previous = board.Find(caption);
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        Vector2 size = new Vector2(166, 61);
        Image backing = BoardFill(caption, board, new Vector2(x, -113), size, Color.white);
        backing.raycastTarget = true;
        Button button = backing.gameObject.AddComponent<Button>();
        button.targetGraphic = backing;
        TextMeshProUGUI label = Label("Label", backing.transform, caption, size, 25);
        label.color = new Color32(20, 47, 55, 255);
        return button;
    }
}
