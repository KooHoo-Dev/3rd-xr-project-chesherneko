using System;
using System.Linq;
using ParcelSorter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ParcelSorterPlayCheck
{
    private const string Active = "ParcelSorter.CliPlayCheck";
    private const string BestKey = "ParcelSorter.Best";
    private static int phase;
    private static float started;
    private static float deadline;
    private static ParcelItem parcel;
    private static GameManager game;
    private static Button action;
    private static bool oldBestPresent;
    private static int oldBest;
    private static ParcelItem contactProbe;
    private static Collider releasedCollider;
    private static Collider neighborCollider;
    private static float releasedAt;
    private static double realtimeLimit;

    static ParcelSorterPlayCheck()
    {
        if (SessionState.GetBool(Active, false)) EditorApplication.update += Step;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(ParcelSorterSceneBuilder.ScenePath);
        SessionState.SetBool(Active, true);
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
        EditorApplication.EnterPlaymode();
    }

    private static void Step()
    {
        if (realtimeLimit == 0d) realtimeLimit = EditorApplication.timeSinceStartup + 90d;
        if (EditorApplication.timeSinceStartup > realtimeLimit)
        {
            Debug.LogError($"Parcel play check stalled in phase {phase}, frame {Time.frameCount}.");
            EditorApplication.Exit(1);
            return;
        }
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        if (Application.isPlaying == false || Time.frameCount < 3) return;
        if (phase == 0)
        {
            oldBestPresent = PlayerPrefs.HasKey(BestKey);
            oldBest = PlayerPrefs.GetInt(BestKey);
            game = Object.FindAnyObjectByType<GameManager>();
            GameUIManager board = Object.FindAnyObjectByType<GameUIManager>();
            action = (Button)new SerializedObject(board).FindProperty("action").objectReferenceValue;
            if (game.State != GameState.Title) { Finish("Missing title"); return; }
            action.onClick.Invoke();
            if (game.State != GameState.Playing) { Finish("Board START did not start the timer"); return; }
            started = Time.time;
            deadline = started + 6f;
            phase = 1;
        }
        if (phase == 1 || phase == 3)
        {
            parcel = Object.FindObjectsByType<ParcelItem>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item.Rejected == false);
            if (parcel != null)
            {
                if (phase == 1 && releasedCollider == null)
                {
                    CheckOverlappingRelease();
                    return;
                }
                if (phase == 1)
                {
                    if (Time.fixedTime < releasedAt + 0.20f) return;
                    if (Physics.GetIgnoreCollision(releasedCollider, neighborCollider))
                    {
                        Finish("Separated parcels still ignore contact");
                        return;
                    }
                    if (releasedCollider.attachedRigidbody.linearVelocity.y >= 0f)
                    {
                        Finish("Released parcel did not fall");
                        return;
                    }
                    contactProbe.Consume();
                    parcel = releasedCollider.GetComponent<ParcelItem>();
                }
                int index = ((int)parcel.Region + (phase == 1 ? 1 : 0)) % 4;
                string prefix = ((RegionType)index).ToString();
                SortingSocket socket = Object.FindObjectsByType<SortingSocket>(FindObjectsSortMode.None)
                    .Single(bin => bin.name.StartsWith(prefix, StringComparison.Ordinal));
                parcel.Body.position = socket.transform.position;
                parcel.Body.linearVelocity = Vector3.zero;
                phase++;
            }
        }
        else if (phase == 2 && parcel.Rejected)
        {
            if (game.Score.Points != -5 || parcel.Grab.isSelected || parcel.Available)
            {
                Finish("Wrong-slot score, release or cooldown failed");
                return;
            }
            phase = 3;
            deadline = Time.time + 5f;
        }
        else if (phase == 4 && game.Score.Correct == 1)
        {
            if (game.Score.Points != 5) { Finish("Correct-slot score failed"); return; }
            phase = 5;
            deadline = started + 67f;
            Time.timeScale = 4f;
        }
        else if (phase == 5 && game.State == GameState.Result)
        {
            if (Time.time - started < 59.9f || game.Score.Attempts <= 2 || Object.FindAnyObjectByType<ParcelItem>() != null)
            {
                Finish("Round timer, conveyor misses or cleanup failed");
                return;
            }
            action.onClick.Invoke();
            if (game.State != GameState.Playing || game.Score.Attempts != 0 || game.Score.Points != 0)
            {
                Finish("Retry did not reset the round");
                return;
            }
            Finish("");
            return;
        }

        if (Time.time > deadline) Finish($"Timed out in phase {phase}, score {game.Score.Points}");
    }

    private static void CheckOverlappingRelease()
    {
        Vector3 position = new Vector3(2f, 2f, 1f);
        parcel.transform.position = position;
        Physics.SyncTransforms();
        contactProbe = Object.Instantiate(parcel, position, Quaternion.identity);
        contactProbe.Initialize(game);
        releasedCollider = parcel.GetComponent<BoxCollider>();
        neighborCollider = contactProbe.GetComponent<BoxCollider>();
        GameObject testHand = new GameObject("Test hand");
        testHand.transform.position = position;
        testHand.AddComponent<SphereCollider>().isTrigger = true;
        XRDirectInteractor hand = testHand.AddComponent<XRDirectInteractor>();
        hand.StartManualInteraction((IXRSelectInteractable)parcel.Grab);
        if (releasedCollider.isTrigger == false)
        {
            Finish("Held parcel still has solid contact");
            return;
        }
        hand.EndManualInteraction();
        Object.Destroy(testHand);
        if (releasedCollider.isTrigger || Physics.GetIgnoreCollision(releasedCollider, neighborCollider) == false)
        {
            Finish($"Overlapping release failed: trigger={releasedCollider.isTrigger}, parcel={releasedCollider.bounds.center}, neighbor={neighborCollider.bounds.center}, selected={parcel.Grab.isSelected}");
            return;
        }
        contactProbe.Body.position = position + Vector3.right;
        releasedAt = Time.fixedTime;
    }

    private static void Finish(string failure)
    {
        EditorApplication.update -= Step;
        SessionState.EraseBool(Active);
        if (oldBestPresent) PlayerPrefs.SetInt(BestKey, oldBest);
        else PlayerPrefs.DeleteKey(BestKey);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        if (failure.Length > 0) Debug.LogError(failure);
        else Debug.Log("Parcel play check passed: title, start, physical socket rejection and correction, belt misses, 60-second result, cleanup and retry.");
        EditorApplication.Exit(failure.Length > 0 ? 1 : 0);
    }
}
