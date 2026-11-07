using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only authoring of two static accessories. Does not touch the core scripts,
/// camera, existing materials or Documentation. No component from this tool runs in a build.
/// </summary>
[InitializeOnLoad]
public static class Mid_WindPropTools
{
    const string ScenePath = "Assets/Mid/Scenes/Mid_WindPavilion.unity";
    const string CompassPath = "Assets/Mid/Prefabs/Mid_WindCompassPlate.prefab";
    const string RackPath = "Assets/Mid/Prefabs/Mid_ScrollRack.prefab";
    static double nextPoll;
    static float started, minY, maxY, minYaw, maxYaw;
    static int errors, samples;
    static bool captureA, captureB;

    static Mid_WindPropTools()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, stack, type) => {
            if (SessionState.GetBool("Mid_PropVerifying", false) &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) errors++;
        };
    }

    static string Output => SessionState.GetString("Mid_PropOutput", "");

    [MenuItem("Mid/Wind/Accessories/Add compass and scroll rack")]
    public static void BuildAndVerify()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before adding props.");
        string output = GetArgument("-midPropReview");
        if (string.IsNullOrEmpty(output)) output = Path.Combine(Path.GetTempPath(), "MidWindPropReview");
        Directory.CreateDirectory(output);
        SessionState.SetString("Mid_PropOutput", output);
        if (!File.Exists(Path.Combine(output, "vcs-mode-before.txt")))
            File.WriteAllText(Path.Combine(output, "vcs-mode-before.txt"), VersionControlSettings.mode);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (GameObject.Find("Mid_WindCompassPlate") != null || GameObject.Find("Mid_ScrollRack") != null ||
            File.Exists(CompassPath) || File.Exists(RackPath))
            throw new InvalidOperationException("Accessory already exists; no asset was overwritten.");

        Transform station = GameObject.Find("Mid_WindRecorder").transform;
        BuildCompass(station);
        BuildRack(station);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        StartReview();
    }

    static void BuildCompass(Transform parent)
    {
        // 고정된 8방위 기준판. 풍향판의 회전 Pivot과 분리되어 함께 회전하지 않는다.
        var root = new GameObject("Mid_WindCompassPlate").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(-0.86f, 0.07f, -0.255f);
        Part(PrimitiveType.Cylinder, "Mid_CompassRim", root, new Vector3(0, 0.013f, 0),
            new Vector3(0.43f, 0.013f, 0.43f), Mat("WindVane"));
        Part(PrimitiveType.Cylinder, "Mid_CompassFace", root, new Vector3(0, 0.029f, 0),
            new Vector3(0.395f, 0.004f, 0.395f), Mat("WindRecordDrum"));
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            bool cardinal = i % 2 == 0;
            var mark = Part(PrimitiveType.Cube, "Mid_CompassMark_" + i, root,
                rotation * new Vector3(0, 0.038f, 0.13f),
                new Vector3(cardinal ? 0.019f : 0.011f, 0.008f, cardinal ? 0.095f : 0.060f),
                Mat(i == 0 ? "WindNeedle" : "Paper"));
            mark.transform.localRotation = rotation;
        }
        Part(PrimitiveType.Cylinder, "Mid_CompassHub", root, new Vector3(0, 0.038f, 0),
            new Vector3(0.038f, 0.006f, 0.038f), Mat("WindVane"));
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, CompassPath, InteractionMode.AutomatedAction);
    }

    static void BuildRack(Transform parent)
    {
        var root = new GameObject("Mid_ScrollRack").transform;
        root.SetParent(parent, false);
        // Empty tabletop strip to the right of the instrument base; no overlap with the moving rail.
        root.localPosition = new Vector3(1.24f, 0f, -0.02f);
        Part(PrimitiveType.Cube, "Mid_RackBase", root, new Vector3(0, 0.018f, 0),
            new Vector3(0.32f, 0.036f, 0.40f), Mat("Worktable"));
        for (int side = -1; side <= 1; side += 2)
            Part(PrimitiveType.Cube, "Mid_RackSupport", root, new Vector3(0, 0.050f, side * 0.12f),
                new Vector3(0.31f, 0.030f, 0.028f), Mat("WindVane"));
        for (int i = 0; i < 3; i++)
        {
            var roll = new GameObject("Mid_ObservationScroll_" + (i + 1)).transform;
            roll.SetParent(root, false); roll.localPosition = new Vector3((i - 1) * 0.095f, 0.102f, 0);
            roll.localRotation = Quaternion.Euler(90, 0, 0);
            Part(PrimitiveType.Cylinder, "Mid_RolledPaper", roll, Vector3.zero,
                new Vector3(0.074f, 0.16f, 0.074f), Mat("Paper"));
            Part(PrimitiveType.Cylinder, "Mid_ScrollBinding", roll, Vector3.zero,
                new Vector3(0.077f, 0.012f, 0.077f), Mat("WindRecordDrum"));
            for (int end = -1; end <= 1; end += 2)
                Part(PrimitiveType.Cylinder, "Mid_ScrollCore", roll, new Vector3(0, end * 0.162f, 0),
                    new Vector3(0.023f, 0.002f, 0.023f), Mat("Frame"));
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, RackPath, InteractionMode.AutomatedAction);
    }

    static Material Mat(string suffix)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mid/Materials/Mat_Mid_" + suffix + ".mat");
        if (material == null) throw new InvalidOperationException("Missing Mid material: " + suffix);
        return material;
    }

    static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    [MenuItem("Mid/Wind/Accessories/Verify existing scene")]
    public static void StartReview()
    {
        if (SceneManager.GetActiveScene().path != ScenePath || EditorApplication.isPlaying)
            throw new InvalidOperationException("Open the saved Mid scene in Edit Mode.");
        var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(type).Show();
        SessionState.SetBool("Mid_PropVerifying", true);
        SessionState.SetBool("Mid_PropStarted", false);
        SessionState.SetString("Mid_PropDrumMaterial", AssetDatabase.GetAssetPath(GameObject.Find("Mid_WindRecordDrum").GetComponent<Renderer>().sharedMaterial));
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(Output)) return;
        if (SessionState.GetBool("Mid_PropVerifying", false))
        {
            if (!Application.isPlaying) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (!SessionState.GetBool("Mid_PropStarted", false))
            {
                SessionState.SetBool("Mid_PropStarted", true);
                SessionState.SetBool("Mid_PropPreviousBackground", Application.runInBackground);
                Application.runInBackground = true;
                started = Time.timeSinceLevelLoad; samples = errors = 0;
                minY = minYaw = float.PositiveInfinity; maxY = maxYaw = float.NegativeInfinity;
                captureA = captureB = false;
            }
            float y = GameObject.Find("Mid_WindNeedle").transform.localPosition.y;
            float yaw = Mathf.DeltaAngle(0, GameObject.Find("Mid_WindVane").transform.localEulerAngles.y);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            minYaw = Mathf.Min(minYaw, yaw); maxYaw = Mathf.Max(maxYaw, yaw); samples++;
            float elapsed = Time.timeSinceLevelLoad - started;
            if (elapsed > 2 && !captureA) { captureA = true; ScreenCapture.CaptureScreenshot(Path.Combine(Output, "props-game-a.png")); }
            if (elapsed > 7 && !captureB) { captureB = true; ScreenCapture.CaptureScreenshot(Path.Combine(Output, "props-game-b.png")); }
            if (elapsed < 16) return;
            bool sameMaterial = SessionState.GetString("Mid_PropDrumMaterial", "") == AssetDatabase.GetAssetPath(GameObject.Find("Mid_WindRecordDrum").GetComponent<Renderer>().sharedMaterial);
            int missing = MissingReferences();
            bool pass = missing == 0 && errors == 0 && !EditorUtility.scriptCompilationFailed && sameMaterial && maxY - minY > 0.45f && maxYaw - minYaw > 20;
            File.WriteAllText(Path.Combine(Output, "validation.txt"),
                $"PASS={pass}\nGame time={elapsed:F2}; samples={samples}\nNeedle Y={minY:F4}..{maxY:F4}\nVane Y={minYaw:F2}..{maxYaw:F2}\nMissing={missing}; Play errors={errors}; Compilation failed={EditorUtility.scriptCompilationFailed}\nDrum material preserved={sameMaterial}\n" + ConsoleCounts());
            SessionState.SetBool("Mid_PropVerifying", false);
            Application.runInBackground = SessionState.GetBool("Mid_PropPreviousBackground", false);
            EditorApplication.isPlaying = false;
            return;
        }
        if (EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string path = Path.Combine(Output, "request.txt");
        if (!File.Exists(path)) return;
        string command = File.ReadAllText(path).Trim();
        if (command == SessionState.GetString("Mid_PropRequest", "")) return;
        SessionState.SetString("Mid_PropRequest", command);
        if (command.StartsWith("review")) StartReview();
        if (command.StartsWith("close"))
        {
            // Only undo Unity's automatic VCS activation back to this session's original mode.
            string vcs = Path.Combine(Output, "vcs-mode-before.txt");
            if (File.Exists(vcs)) VersionControlSettings.mode = File.ReadAllText(vcs).Trim();
            EditorApplication.Exit(0);
        }
    }

    static int MissingReferences()
    {
        int missing = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            var mesh = t.GetComponent<MeshFilter>(); if (mesh != null && mesh.sharedMesh == null) missing++;
            foreach (var renderer in t.GetComponents<Renderer>())
            foreach (var material in renderer.sharedMaterials)
                if (material == null || material.shader == null || !material.shader.isSupported) missing++;
        }
        return missing;
    }

    static string ConsoleCounts()
    {
        var type = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
        var method = type?.GetMethod("GetCountsByType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null) return "";
        object[] counts = { 0, 0, 0 }; method.Invoke(null, counts);
        return $"Console errors/warnings/logs={counts[0]}/{counts[1]}/{counts[2]}\n";
    }

    static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
        return "";
    }
}
