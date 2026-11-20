using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Batch-only Play Mode integration check. Private fields are inspected only by this editor check.
[InitializeOnLoad]
public static class ShowcaseSmoke
{
    private const string Active = "GridForge.ShowcaseSmoke";
    private static IEnumerator _steps;
    private static double _deadline;
    private static string _error;
    private static int _lastFrame = -1;

    static ShowcaseSmoke() => EditorApplication.playModeStateChanged += ModeChanged;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run with -batchmode.");
        EditorSceneManager.OpenScene("Assets/Scenes/AppScene.unity");
        SessionState.SetBool(Active, true);
        EditorApplication.EnterPlaymode();
    }

    private static void ModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Active, false) || state != PlayModeStateChange.EnteredPlayMode) return;
        _deadline = EditorApplication.timeSinceStartup + 120;
        Application.logMessageReceived += Capture;
        _steps = Exercise();
        EditorApplication.update += Step;
    }

    private static void Capture(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            _error ??= message;
    }

    private static void Step()
    {
        try
        {
            Check(_error == null, _error);
            Check(EditorApplication.timeSinceStartup < _deadline, "Timed out waiting for scene or phase.");
            if (Time.frameCount == _lastFrame) return;
            _lastFrame = Time.frameCount;
            if (_steps.MoveNext()) return;
            Debug.Log("[ShowcaseSmoke] PASS");
            Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Finish(1);
        }
    }

    private static void Finish(int code)
    {
        Application.logMessageReceived -= Capture;
        EditorApplication.update -= Step;
        SessionState.EraseBool(Active);
        EditorApplication.Exit(code);
    }

    private static T Read<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    // Capture the match camera viewport. Screen-space overlay UI is outside this image.
    private static void CaptureBoard(Camera camera, string filename)
    {
        int width = 1600;
        var target = RenderTexture.GetTemporary(width, Mathf.RoundToInt(width / camera.aspect), 24);
        var previous = camera.targetTexture;
        var viewport = camera.rect;
        var active = RenderTexture.active;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes(filename, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previous;
            camera.rect = viewport;
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(image);
        }
    }

    private static TextMeshPro CheckName(UnitActor actor, string expected, Camera camera)
    {
        var label = actor.GetComponentsInChildren<TextMeshPro>().Single();
        label.ForceMeshUpdate();
        Check(label.text == expected && label.textInfo.characterCount == expected.Length &&
              label.textInfo.characterInfo.Take(expected.Length).All(c => c.isVisible && c.textElement.unicode == c.character),
              "Missing name or Chinese glyph: " + expected);
        var bodies = actor.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r != label.renderer);
        Check(label.transform.position.y > bodies.Max(r => r.bounds.max.y) &&
              Quaternion.Angle(label.transform.rotation, camera.transform.rotation) < .1f, "Label pose: " + expected);
        return label;
    }

    private static void CheckFacing(UnitActor actor, Vector3 towardsOpponent)
    {
        var visualRoot = actor.transform;
        var animator = actor.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            visualRoot = animator.transform;
            while (visualRoot.parent != actor.transform) visualRoot = visualRoot.parent;
        }
        Check(Vector3.Dot(visualRoot.forward, towardsOpponent.normalized) > .99f,
              "Incorrect starting direction: " + actor.name);
    }

    private static IEnumerator Exercise()
    {
        MainView main;
        while (!(main = Object.FindObjectOfType<MainView>())) yield return null;
        while (!main.IsOpen) yield return null;
        main.GetComponentInChildren<Button>().onClick.Invoke();
        BattleSceneRunner runner;
        while (!(runner = Object.FindObjectOfType<BattleSceneRunner>()) || !runner.CanEditPreparation) yield return null;
        yield return null;
        var modules = Read<ModuleManager>(runner, "_modules");
        var shop = modules.GetModule<ShopModule>().Controller;
        var bench = modules.GetModule<BenchModule>().Model;
        var board = modules.GetModule<BoardModule>().Model;
        var card = Object.FindObjectOfType<ShopView>().GetComponentsInChildren<ShopCard>()[0];
        var offer = shop.GetOfferVo(0);
        Check(offer.ConfigId == 91006, "Featured model missing from opening shop.");
        int gold = shop.Gold;
        Read<Button>(card, "_btnRoot").onClick.Invoke();
        var unit = bench.GetSlot(0);
        Check(unit != null && unit.ConfigId == offer.ConfigId && shop.Gold == gold - offer.Cost, "Purchase failed.");
        Check(shop.BuyOffer(0) == ShopPurchaseResult.AlreadyPurchased, "Duplicate buy.");
        yield return null;
        Canvas.ForceUpdateCanvases();
        Physics.SyncTransforms();
        var input = Object.FindObjectOfType<PlacementInput>();
        var camera = Read<Camera>(input, "_sceneCamera");
        var actor = Object.FindObjectsOfType<UnitActor>().Single(a => a.InstanceId == unit.InstanceId);
        var cell = GameObject.Find("Cell_1_0").transform;
        var enemyDirection = GameObject.Find("Cell_1_6").transform.position - cell.position;
        CheckFacing(actor, enemyDirection);
        var table = Read<UnitConfigTable>(Read<UnitActorManager>(Object.FindObjectOfType<MatchSceneRoot>(), "_unitActorManager"), "_unitConfigs");
        var animator = actor.GetComponentInChildren<Animator>();
        Check(animator && animator.runtimeAnimatorController.animationClips.Select(c => c.name).Distinct().Count() == 6 &&
              !actor.transform.Find("Capsule").GetComponent<Renderer>().enabled, "Expected six-clip model in place of capsule.");
        var name = CheckName(actor, offer.Name, camera);
        if (Environment.GetCommandLineArgs().Contains("-showcaseCapture"))
            CaptureBoard(camera, "Logs/bench-preview.png");
        var originalPosition = name.transform.position;
        var start = actor.GetComponentInChildren<Collider>().bounds.center;
        var destination = cell.position + start - actor.transform.position;
        // Drive the same pick and drop handlers as LateUpdate, with screen-space camera coordinates.
        input.SendMessage("TryBeginDrag", (Vector2)camera.WorldToScreenPoint(start));
        Check(Read<UnitActor>(input, "_draggedActor") == actor, "Bench picking failed.");
        input.SendMessage("FinishDrag", (Vector2)camera.WorldToScreenPoint(destination));
        Check(board.GetCell(1, 0) == unit && bench.GetSlot(0) == null, "Board drop failed.");
        yield return null;
        Check(CheckName(actor, offer.Name, camera) == name && (originalPosition - name.transform.position).sqrMagnitude > .1f, "Label did not follow placement.");
        CheckFacing(actor, enemyDirection);
        Debug.Log("[ShowcaseSmoke] Purchase and screen-space drag passed.");
        var button = Object.FindObjectOfType<BattlePhaseView>().GetComponent<Button>();
        var ui = Read<UIManager>(runner, "_ui");
        var resources = Read<Dictionary<string, ResourceEntry>>(Read<ResourceManager>(ui, "_resources"), "_entries");
        var listeners = Read<Dictionary<object, List<Delegate>>>(Read<EventBus>(ui, "_events"), "_events");
        var beforeResources = resources.ToDictionary(x => x.Key, x => x.Value.RefCount);
        var beforeListeners = listeners.ToDictionary(x => x.Key, x => x.Value.ToArray());
        // A batch editor has no focused Game View. Route the virtual keyboard to the player loop.
        var batchInput = Object.Instantiate(InputSystem.settings);
        batchInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        batchInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = batchInput;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        for (int round = 0; round < 4; round++)
        {
            button.onClick.Invoke();
            var battle = modules.GetModule<BattleModule>();
            Check(battle.Controller.IsRunning && battle.Model.Units.Count == 6 && !input.enabled, "Incorrect combat start.");
            yield return null;
            foreach (var fighter in battle.Model.Units)
            {
                var body = GameObject.Find($"Battle_{fighter.Team}_{fighter.BattleId}").GetComponent<UnitActor>();
                CheckFacing(body, fighter.Team == BattleTeam.Player ? enemyDirection : -enemyDirection);
                CheckName(body, table.Get(fighter.ConfigId).name, camera);
            }
            if (round == 0 && Environment.GetCommandLineArgs().Contains("-showcaseCapture"))
                CaptureBoard(camera, "Logs/combat-preview.png");
            var rotation = camera.transform.rotation;
            camera.transform.rotation *= Quaternion.Euler(0, 12, 0);
            yield return null;
            yield return null;
            foreach (var label in Object.FindObjectsOfType<TextMeshPro>())
                Check(Quaternion.Angle(label.transform.rotation, camera.transform.rotation) < .1f, "Camera turn failed.");
            camera.transform.rotation = rotation;
            Time.timeScale = 2f;
            var seenStates = new HashSet<string>();
            var playerUnit = battle.Model.Units.Single(x => x.Team == BattleTeam.Player);
            var combatAnimator = GameObject.Find($"Battle_Player_{playerUnit.BattleId}").GetComponentInChildren<Animator>();
            // Two ordinary battles, then deterministic victory and mutual elimination fixtures.
            if (round >= 2)
                foreach (var fighter in battle.Model.Units)
                    if (round == 3 || fighter.Team == BattleTeam.Enemy)
                        fighter.TakeDamage(fighter.MaxHp);
            while (battle.Controller.IsRunning)
            {
                foreach (var clip in combatAnimator.GetCurrentAnimatorClipInfo(0)) seenStates.Add(clip.clip.name);
                yield return null;
            }
            yield return null;
            foreach (var clip in combatAnimator.GetCurrentAnimatorClipInfo(0)) seenStates.Add(clip.clip.name);
            Debug.Log("[ShowcaseSmoke] Animated model clips seen: " + string.Join(", ", seenStates));
            if (round < 2)
                Check(seenStates.Contains("Chiikawa_Attack") && seenStates.Contains("Chiikawa_Move"), "Combat animation failed.");
            var outcome = battle.Model.Outcome;
            if (round == 2) Check(outcome == BattleOutcome.PlayerWin, "Victory fixture failed.");
            if (round == 3) Check(outcome == BattleOutcome.Draw, "Draw fixture failed.");
            var expectedTitle = outcome switch
            {
                BattleOutcome.EnemyWin => "失败",
                BattleOutcome.PlayerWin => "胜利",
                BattleOutcome.Draw => "平局",
                _ => throw new InvalidOperationException("Outcome missing.")
            };
            var resultLabel = button.GetComponentInChildren<TMP_Text>();
            resultLabel.ForceMeshUpdate();
            Check(resultLabel.GetParsedText().Contains(expectedTitle) &&
                  resultLabel.GetParsedText().Contains("按任意键返回准备阶段") &&
                  resultLabel.textInfo.characterInfo.Take(resultLabel.textInfo.characterCount)
                      .Where(c => !char.IsWhiteSpace(c.character)).All(c => c.isVisible && c.textElement.unicode == c.character),
                  "Result title, hint or glyph missing.");
            var rect = (RectTransform)button.transform;
            Check(rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one &&
                  rect.offsetMin == Vector2.zero && rect.offsetMax == Vector2.zero,
                  "Result overlay does not fill UI root.");
            yield return null;
            Check(!runner.CanEditPreparation, "Result dismissed without input.");
            Key dismiss = round switch { 0 => Key.Space, 1 => Key.B, 2 => Key.Enter, _ => Key.LeftShift };
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(dismiss));
            Debug.Log($"[ShowcaseSmoke] Result {outcome}, key {dismiss}");
            while (!runner.CanEditPreparation) yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Check(!button.transform.Find("Result banner").gameObject.activeSelf, "Result banner survived return.");
            Check(input.enabled && actor.gameObject.activeInHierarchy && board.GetCell(1, 0) == unit &&
                  battle.Model.Units.Count == 0 && Object.FindObjectsOfType<UnitActor>(true).Length == 1 &&
                  Object.FindObjectsOfType<TextMeshPro>(true).Length == 1 &&
                  Object.FindObjectOfType<ShopView>() && shop.Gold == gold - offer.Cost, "Incorrect preparation restore.");
            CheckFacing(actor, enemyDirection);
            Check(resources.Count == beforeResources.Count &&
                  beforeResources.All(x => resources.TryGetValue(x.Key, out var value) && value.RefCount == x.Value),
                  "Resource ownership changed across battle.");
            Check(listeners.Count == beforeListeners.Count &&
                  beforeListeners.All(x => listeners.TryGetValue(x.Key, out var value) &&
                  value.Count == x.Value.Length && x.Value.All(value.Contains)), "Subscriptions changed across battle.");
            Debug.Log("[ShowcaseSmoke] Round " + (round + 1) + " restored.");
        }
        InputSystem.RemoveDevice(keyboard);
        Check(shop.TrySellUnit(unit.InstanceId) == UnitSaleResult.Success, "Cleanup sale failed.");
        yield return null;
        yield return null;
        Check(Object.FindObjectsOfType<TextMeshPro>(true).Length == 0 && !actor, "Name survived actor release.");
        Time.timeScale = 1f;
    }
}
