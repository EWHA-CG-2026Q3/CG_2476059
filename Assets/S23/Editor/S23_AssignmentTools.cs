using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>Authoring/capture tools scoped to S23. Never writes an existing weekly/Mid asset.</summary>
[InitializeOnLoad]
public static class S23_AssignmentTools
{
    const string Root="Assets/S23/";
    const string ScenePath=Root+"Scenes/S23_CurvedPavilion.unity";
    static readonly string Review=Path.GetFullPath("Library/S23_Automation")+"/";
    static double next;
    static S23_AssignmentTools() { Directory.CreateDirectory(Review); EditorApplication.update+=Poll; }
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+0.5;
        string inbox=Review+"command.txt";
        if(!File.Exists(inbox))return;
        string command=File.ReadAllText(inbox).Trim();
        if(command==""||command==SessionState.GetString("S23_Command",""))return;
        SessionState.SetString("S23_Command",command);
        try {
            if(command.StartsWith("status")) File.WriteAllText(Review+"editor-status.txt",SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty+" play="+EditorApplication.isPlaying);
            else if(command.StartsWith("build")) Build();
            else if(command.StartsWith("capture")) CaptureAll();
            else if(command.StartsWith("polish")) Polish();
            else if(command.StartsWith("fitroof")) {
                if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Fit only S23 edit scene");
                FitCanopyToFrame();EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);File.WriteAllText(Review+"roof-fit-done.txt",DateTime.Now.ToString());
            }
            else if(command.StartsWith("roofcheck")) {
                if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Not S23 scene");
                var c=Camera.main;var position=c.transform.position;var rotation=c.transform.rotation;float fov=c.fieldOfView;
                try {
                    Capture(c,"S23_CanopyAttached_Overview",Review);
                    View(c,new Vector3(7,9,-8),new Vector3(0,4.2f,1.65f));c.fieldOfView=43;Capture(c,"S23_CanopyAttached_Above",Review);
                    View(c,new Vector3(.2f,1.5f,-.5f),new Vector3(0,4.3f,1.7f));c.fieldOfView=85;Capture(c,"S23_CanopyAttached_Below",Review);
                    Validate();
                } finally {c.transform.SetPositionAndRotation(position,rotation);c.fieldOfView=fov;}
            }
            else if(command.StartsWith("roofprobe")) {
                var sb=new StringBuilder();
                foreach(var mf in GameObject.Find("S23_Pavilion").GetComponentsInChildren<MeshFilter>()) {
                    sb.AppendLine(mf.name+" "+mf.GetComponent<Renderer>().bounds);
                    if(mf.name.IndexOf("roof",StringComparison.OrdinalIgnoreCase)>=0)continue;
                    foreach(var p in mf.sharedMesh.vertices.Select(v=>mf.transform.TransformPoint(v)).Where(v=>v.y>3.95f).Select(v=>v.ToString("F3")).Distinct().Take(100))sb.AppendLine(p);
                }
                File.WriteAllText(Review+"roof-probe.txt",sb.ToString());
            }
            else if(command.StartsWith("attach")) { AttachLeaves();EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);File.WriteAllText(Review+"attach-done.txt",DateTime.Now.ToString()); }
            else if(command.StartsWith("leafcheck")) {
                if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Not S23 scene");
                var c=Camera.main;var position=c.transform.position;var rotation=c.transform.rotation;float fov=c.fieldOfView;
                try { View(c,new Vector3(-.1f,3.4f,-7.1f),new Vector3(-1.55f,2.5f,-1.5f));c.fieldOfView=43;Capture(c,"S23_LeavesAttached",Review);Validate(); }
                finally {c.transform.SetPositionAndRotation(position,rotation);c.fieldOfView=fov;}
            }
            else if(command.StartsWith("game")) { ScreenCapture.CaptureScreenshot(Root+"Documentation/Screenshots/S23_00_GameView.png");Validate(); }
            else if(command.StartsWith("save")) { if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Only S23 edit scene can be saved");EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();AssetDatabase.Refresh(); }
            else if(command.StartsWith("play")) { if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open S23 scene first");EditorApplication.isPlaying=true; }
            else if(command.StartsWith("stop")) { EditorApplication.isPlaying=false; }
        } catch(Exception e){File.WriteAllText(Review+"error.txt",e.ToString());Debug.LogException(e);}
    }

    [MenuItem("S23/Build assignment scene")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode before building");
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Existing scene has unsaved changes; not closing it.");
        if(File.Exists(ScenePath))throw new Exception("S23 scene already exists. Refusing to overwrite.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.56f,.62f,.66f);
        var light=new GameObject("S23_Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-35,0);light.shadows=LightShadows.Soft;
        light.gameObject.AddComponent<UniversalAdditionalLightData>();
        Material green=Material("Stem", "Universal Render Pipeline/Lit",new Color(.10f,.24f,.055f));
        Material stone=Material("Ground","Universal Render Pipeline/Lit",new Color(.58f,.64f,.59f));
        Material wood=Material("Display","Universal Render Pipeline/Lit",new Color(.16f,.22f,.24f));
        Material canopy=Material("Canopy","CG/S23/DoubleSidedSurface",new Color(.86f,.71f,.43f));
        Material leaf=Material("Leaf","CG/S23/DoubleSidedSurface",Color.white);
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/Leaf_43b42237e5634af69e325bbbf6a2ef8b.png");
        leaf.SetTexture("_BaseMap",tex);
        Material final=Material("ToonVertexOutline","CG/S23/ToonVertexOutline",Color.white);
        final.SetFloat("_OutlineWidth",.025f);final.SetFloat("_ToonSteps",3);final.SetFloat("_Ambient",.24f);
        Material toon=Material("Separate_Toon","CG/S23/Reference/S20_Toon_Finish",Color.white);
        Material outline=Material("Separate_Outline","CG/S23/Reference/S20_Outline_Finish",Color.white);
        Material vertex=Material("Separate_VertexColor","CG/S23/Reference/S20_VertexColor_Finish",Color.white);
        Primitive("S23_Ground",PrimitiveType.Cube,new Vector3(0,-.15f,0),new Vector3(22,.2f,21),stone);
        var pavilion=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Pavilion/Prefabs/Pavilion.prefab"));pavilion.name="S23_Pavilion";
        foreach(var r in pavilion.GetComponentsInChildren<Renderer>())if(r.name.IndexOf("roof",StringComparison.OrdinalIgnoreCase)>=0){r.enabled=false;Debug.Log("S23 disabled instance roof: "+r.name+" "+r.bounds);}
        // A single non-planar saddle patch: opposite corners high, the other pair low.
        Patch("S23_Canopy",new Vector3(-.04f,4.65f,1.68f),new[]{new Vector3(-2.8f,.95f,-3.25f),new Vector3(2.8f,-.35f,-3.25f),new Vector3(-2.8f,-.35f,3.25f),new Vector3(2.8f,.95f,3.25f)},canopy,32);
        FitCanopyToFrame();
        // Vine stem and branch are actual Unity SplineContainer + SplineExtrude components.
        Vine("S23_VineStem",new Vector3(-1.95f,.95f,-1.65f),new[]{new float3(0,0,0),new float3(.18f,.7f,.05f),new float3(-.10f,1.45f,.12f),new float3(.24f,2.25f,.03f),new float3(.06f,3.2f,.2f)},green,.045f);
        // Texture's base-to-tip direction runs along UV (0,0) -> (1,1).
        for(int i=0;i<5;i++)
        {
            Vector3 origin=new Vector3(-1.9f,.95f+i*.55f+.45f,-1.65f);
            bool right=i%2==0;
            Vector3 side=right?Vector3.right:Vector3.left;
            var corners=new[]{Vector3.zero,side*.75f+new Vector3(0,.02f,-.08f),new Vector3(0,.75f,.08f),side*.92f+new Vector3(0,.96f,.23f)};
            Patch("S23_Leaf_"+(i+1),origin,corners,leaf,20);
        }
        AttachLeaves();
        // Comparison mesh has ONE submesh and explicit per-vertex colors.
        var temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh=UnityEngine.Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(temp);mesh.name="S23_VertexColorSphere";
        var colors=mesh.vertices.Select(v=>Color.Lerp(new Color(.16f,.48f,.80f),new Color(1,.45f,.12f),Mathf.InverseLerp(-.5f,.5f,v.y))).ToArray();mesh.colors=colors;
        AssetDatabase.CreateAsset(mesh,Root+"Meshes/S23_VertexColorSphere.asset");
        for(int i=0;i<2;i++)
        {
            Vector3 p=new Vector3(3.8f+i*2.5f,1.45f,-2.1f);
            Primitive("S23_Plinth_"+i,PrimitiveType.Cube,p+Vector3.down*.85f,new Vector3(2,.3f,2),wood);
            var go=new GameObject(i==0?"S23_ThreeMaterial_Comparison":"S23_TwoPass_OneMaterial");go.transform.position=p;go.transform.localScale=Vector3.one*1.55f;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=i==0?new[]{toon,outline,vertex}:new[]{final};
        }
        Label("SPLINE EXTRUDE / BILINEAR LEAVES",new Vector3(-1.8f,.25f,-3.1f),.13f);
        Label("3 MATERIALS",new Vector3(3.8f,.34f,-3.2f),.13f);
        Label("2 PASSES / 1 MATERIAL",new Vector3(6.3f,.34f,-3.2f),.12f);
        var camera=new GameObject("S23_MainCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.backgroundColor=new Color(.70f,.79f,.83f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=43;camera.farClipPlane=100;camera.nearClipPlane=.1f;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        View(camera,new Vector3(11,8,-14),new Vector3(1.4f,2.1f,.3f));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
        File.WriteAllText(Review+"build-done.txt","Built "+ScenePath+"\n"+DateTime.Now);
        Validate();
    }

    static Material Material(string name,string shaderName,Color color)
    {
        var shader=Shader.Find(shaderName);if(!shader)throw new Exception("Shader missing: "+shaderName);
        var mat=new Material(shader){name="Mat_S23_"+name};if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);
        AssetDatabase.CreateAsset(mat,Root+"Materials/"+mat.name+".mat");return mat;
    }
    static GameObject Primitive(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static void Vine(string name,Vector3 pos,float3[] points,Material mat,float radius)
    {
        var go=new GameObject(name);go.transform.position=pos;
        var container=go.AddComponent<SplineContainer>();container.Spline=new Spline(points,TangentMode.AutoSmooth);
        var extrude=go.AddComponent<SplineExtrude>();extrude.Container=container;extrude.Sides=8;extrude.Radius=radius;extrude.SegmentsPerUnit=12;extrude.Capped=true;extrude.Rebuild();go.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void Patch(string name,Vector3 pos,Vector3[] corners,Material mat,int segments)
    {
        var go=new GameObject(name);go.transform.position=pos;var patch=go.AddComponent<S23_BilinearPatch>();
        Transform[] refs=new Transform[4];string[] names={"Corner00","Corner10","Corner01","Corner11"};
        for(int i=0;i<4;i++){refs[i]=new GameObject(names[i]).transform;refs[i].SetParent(go.transform,false);refs[i].localPosition=corners[i];}
        patch.corner00=refs[0];patch.corner10=refs[1];patch.corner01=refs[2];patch.corner11=refs[3];patch.uSegments=patch.vSegments=segments;patch.Rebuild();go.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void Label(string text,Vector3 pos,float size)
    {
        var go=new GameObject("S23_Label_"+text);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(50,0,0);
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.anchor=TextAnchor.MiddleCenter;tm.fontSize=64;tm.characterSize=size*.17f;tm.color=new Color(.09f,.15f,.17f);
        go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void View(Camera c,Vector3 pos,Vector3 target){c.transform.position=pos;c.transform.LookAt(target);}
    static void FitCanopyToFrame()
    {
        var patch=GameObject.Find("S23_Canopy").GetComponent<S23_BilinearPatch>();
        // Measured top rail: y=4.087..4.088, x=-2.456 / 2.378, z=-1.15 / 4.50.
        // The low corners sit on the rails; two slim posts support the raised corners.
        Vector3[] points={new Vector3(-2.456f,5.187f,-1.15f),new Vector3(2.378f,4.087f,-1.15f),new Vector3(-2.456f,4.088f,4.50f),new Vector3(2.378f,5.188f,4.50f)};
        patch.transform.position=new Vector3(-.039f,4.0875f,1.675f);
        Transform[] corners={patch.corner00,patch.corner10,patch.corner01,patch.corner11};
        for(int i=0;i<4;i++)corners[i].position=points[i];
        patch.Rebuild();
        var wood=GameObject.Find("S23_Pavilion").GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Pavilion").sharedMaterial;
        foreach(int i in new[]{0,3}) {
            string name="S23_CanopySupport_"+i;
            var post=GameObject.Find(name);
            if(!post)post=Primitive(name,PrimitiveType.Cube,Vector3.zero,Vector3.one,wood);
            float bottom=i==0?4.087f:4.088f;
            post.transform.position=new Vector3(points[i].x,(bottom+points[i].y)*.5f,points[i].z);
            post.transform.localScale=new Vector3(.085f,points[i].y-bottom,.085f);
        }
        File.WriteAllText(Review+"roof-contact.txt","Corner10: y=4.087 (front rail); Corner01: y=4.088 (back rail).\nRaised corners supported by two 1.10 m posts.\n");
    }
    // Place each textured petiole on the REAL curved stem, rather than a fixed x/z line.
    static void AttachLeaves()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=ScenePath && File.Exists(ScenePath))throw new Exception("Attach only in S23 edit scene");
        var stem=GameObject.Find("S23_VineStem").GetComponent<SplineContainer>();
        var report=new StringBuilder();
        for(int i=0;i<5;i++)
        {
            var patch=GameObject.Find("S23_Leaf_"+(i+1)).GetComponent<S23_BilinearPatch>();
            // The visible stem tip is slightly inside the texture, not exactly at UV (0,0).
            const float u=.012f,v=.012f;
            Vector3 root=(1-u)*(1-v)*patch.corner00.position+u*(1-v)*patch.corner10.position
                        +(1-u)*v*patch.corner01.position+u*v*patch.corner11.position;
            float height=.95f+i*.55f+.45f;
            float best=float.MaxValue;Vector3 target=Vector3.zero;
            for(int sample=0;sample<=1024;sample++)
            {
                Vector3 point=(Vector3)stem.EvaluatePosition(sample/1024f);
                float distance=Mathf.Abs(point.y-height);
                if(distance<best){best=distance;target=point;}
            }
            // Embed the small textured stalk in the stem so alpha edges cannot leave a gap.
            patch.transform.position+=target-root;
            patch.Rebuild();
            report.AppendLine(patch.name+": petiole attached at "+target.ToString("F4"));
        }
        File.WriteAllText(Review+"leaf-attachment.txt",report.ToString());
    }
    [MenuItem("S23/Capture assignment views")]
    public static void CaptureAll()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Not S23 scene");
        var c=Camera.main;Vector3 old=c.transform.position;Quaternion rotation=c.transform.rotation;float fov=c.fieldOfView;
        try {
            Capture(c,"S23_01_Overview");
            View(c,new Vector3(-.1f,3.4f,-7.1f),new Vector3(-1.55f,2.5f,-1.5f));c.fieldOfView=43;Capture(c,"S23_02_VineLeaves");
            View(c,new Vector3(7,10,-8),new Vector3(0,4.2f,1.65f));c.fieldOfView=43;Capture(c,"S23_03_CanopyAbove");
            View(c,new Vector3(.2f,1.5f,-.5f),new Vector3(0,4.3f,1.7f));c.fieldOfView=85;Capture(c,"S23_04_CanopyBelow");
            View(c,new Vector3(5.05f,2.6f,-8.3f),new Vector3(5.05f,1.1f,-2.1f));c.fieldOfView=45;Capture(c,"S23_05_ShaderComparison");
        } finally {c.transform.SetPositionAndRotation(old,rotation);c.fieldOfView=fov;}
        Validate();
        File.WriteAllText(Review+"capture-done.txt",DateTime.Now.ToString());
    }
    static void Polish()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Polish only S23 edit scene");
        foreach(var tm in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))if(tm.name.StartsWith("S23_Label")){tm.characterSize=tm.text.StartsWith("SPLINE")?.017f:.024f;tm.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
        FitCanopyToFrame();
        var branch=GameObject.Find("S23_VineBranch");if(branch)UnityEngine.Object.DestroyImmediate(branch);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
        File.WriteAllText(Review+"polish-done.txt",DateTime.Now.ToString());
    }
    static void Capture(Camera c,string name,string outputFolder=null)
    {
        // Render through URP camera in the Editor/Play Mode; not an illustration or mockup.
        var previous=c.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);rt.Create();
        c.targetTexture=rt;c.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
        File.WriteAllBytes((outputFolder??Root+"Documentation/Screenshots/")+name+".png",tex.EncodeToPNG());
        c.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    [MenuItem("S23/Validate current S23 scene")]
    public static void Validate()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open S23 scene first");
        var sb=new StringBuilder();sb.AppendLine("S23 Unity verification "+DateTime.Now);sb.AppendLine("Scene: "+SceneManager.GetActiveScene().path);sb.AppendLine("Play Mode: "+Application.isPlaying);
        sb.AppendLine("Elapsed game seconds: "+Time.timeSinceLevelLoad+", frame="+Time.frameCount);
        int missing=0;
        foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        sb.AppendLine("Missing scripts: "+missing);
        foreach(var patch in UnityEngine.Object.FindObjectsByType<S23_BilinearPatch>(FindObjectsSortMode.None)){var m=patch.GetComponent<MeshFilter>().sharedMesh;sb.AppendLine(patch.name+": "+(m?m.vertexCount+" vertices, "+m.triangles.Length/3+" triangles":"MISSING MESH"));}
        foreach(var ex in UnityEngine.Object.FindObjectsByType<SplineExtrude>(FindObjectsSortMode.None))sb.AppendLine(ex.name+": SplineExtrude, knots="+ex.Container.Spline.Count+", vertices="+ex.GetComponent<MeshFilter>().sharedMesh.vertexCount);
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if(r.enabled)foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)sb.AppendLine("INVALID MATERIAL: "+r.name);
        var final=GameObject.Find("S23_TwoPass_OneMaterial").GetComponent<Renderer>();sb.AppendLine("Final material slots="+final.sharedMaterials.Length+", shader passes="+final.sharedMaterial.passCount);
        foreach(var guid in AssetDatabase.FindAssets("t:Shader",new[]{Root+"Shaders"})) {var s=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));foreach(var msg in ShaderUtil.GetShaderMessages(s))sb.AppendLine(s.name+": "+msg.severity+" "+msg.message);}
        var type=typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");var method=type.GetMethod("GetCountsByType",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);object[] counts={0,0,0};method.Invoke(null,counts);sb.AppendLine("Console errors/warnings/logs: "+string.Join("/",counts));
        File.WriteAllText(Root+"Documentation/S23_Validation.txt",sb.ToString());
    }
}
