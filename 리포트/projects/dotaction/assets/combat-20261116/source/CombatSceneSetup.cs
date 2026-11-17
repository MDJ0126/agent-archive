using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TurnRoom.PixelCamera;
using TurnRoom.PixelCamera.URP;

namespace DotAction.Editor
{
    /// <summary>
    /// 편집 가능한 전투 씬·프리팹·재질을 최초 구성하고 기존 결과를 덮어쓰지 않는다.
    /// </summary>
    public static class CombatSceneSetup
    {
        public const string SCENE_PATH = "Assets/Scenes/PixelCombat.unity";
        private const string CREATURE_ROOT = "Assets/polyperfect/Low Poly Animated Fantasy Creatures";
        private static readonly Dictionary<Material, Material> MaterialCopies = new();

        public static void ApplyDiabloView()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH);
            CombatSession session = UnityEngine.Object.FindFirstObjectByType<CombatSession>();
            Camera main = Camera.main;
            main.orthographicSize = 6.2f;
            var follow = main.GetComponent<CombatCameraFollow>();
            if (follow == null)
            {
                follow = main.gameObject.AddComponent<CombatCameraFollow>();
            }
            follow.target = session.player.transform;
            main.transform.position = session.player.transform.position + follow.offset;
            main.transform.LookAt(session.player.transform.position);
            main.GetUniversalAdditionalCameraData().cameraStack.Clear();
            Canvas canvas = session.progressText != null ? session.progressText.canvas : null;
            if (canvas != null)
            {
                Camera uiCamera = canvas.worldCamera;
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                if (uiCamera != null)
                {
                    UnityEngine.Object.DestroyImmediate(uiCamera.gameObject);
                }
            }
            session.healthText = null;
            session.progressText = null;
            session.resultText = null;
            session.route.enabled = false;
            session.destinationMarker.gameObject.SetActive(false);
            Transform identification = session.player.transform.Find("Player identification");
            if (identification != null)
            {
                UnityEngine.Object.DestroyImmediate(identification.gameObject);
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SCENE_PATH);
            Debug.Log("DOTACTION_DIABLO_VIEW_SAVED: ortho=6.2; player follow; world health bars only; PC desktop");
        }

        public static void RepairHudLayer()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH);
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            canvas.gameObject.layer = 5;
            foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 5;
            }
            Camera hudCamera = canvas.worldCamera;
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.SetParent(hudCamera.transform, false);
            rect.localPosition = Vector3.forward;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = new Vector2(1280, 720);
            rect.localScale = Vector3.one * (hudCamera.orthographicSize * 2 / 720);
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(SCENE_PATH);
            Debug.Log("DOTACTION_HUD_LAYER_SAVED: camera-local WorldSpace canvas; 1280x720; UI layer");
        }

        public static void ConfigurePixels()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH);
            PixelCameraPreset preset = AssetDatabase.LoadAssetAtPath<PixelCameraPreset>("Assets/Data/CombatPixels.asset");
            // 도입한 현재 소스는 Selected Objects 경로를 제공하므로 실제 지원 경로를 사용한다.
            preset.pixelization = PixelCameraPixelization.SelectedObjects;
            GameObject environment = new GameObject("Pixel World — 2px");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != environment && root.GetComponent<CombatActor>() == null && root.GetComponent<RectTransform>() == null && root.GetComponentInChildren<Renderer>() != null)
                {
                    root.transform.SetParent(environment.transform, true);
                }
            }
            AddPixelMarker(environment);
            AddPixelMarker(UnityEngine.Object.FindFirstObjectByType<CombatActor>().gameObject);
            Camera main = Camera.main;
            main.cullingMask &= ~(1 << 5);
            var hudCameraObject = new GameObject("HUD Camera — native resolution", typeof(Camera));
            Camera hudCamera = hudCameraObject.GetComponent<Camera>();
            hudCamera.CopyFrom(main);
            hudCamera.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            hudCamera.cullingMask = 1 << 5;
            hudCamera.clearFlags = CameraClearFlags.Depth;
            UniversalAdditionalCameraData hudData = hudCamera.GetUniversalAdditionalCameraData();
            hudData.renderType = CameraRenderType.Overlay;
            hudData.SetRenderer(1);
            main.GetUniversalAdditionalCameraData().cameraStack.Add(hudCamera);
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            canvas.gameObject.layer = 5;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = hudCamera;
            canvas.planeDistance = 1;
            foreach (string name in new[] { "Ranger", "Goblin", "Skeleton", "AetherBolt" })
            {
                string path = "Assets/Prefabs/Combat/" + name + ".prefab";
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                AddPixelMarker(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            EditorUtility.SetDirty(preset);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SCENE_PATH);
            Debug.Log("DOTACTION_PIXEL_MARKERS_SAVED: world+actors; 2px at 720p; source-supported SelectedObjects path");
        }

        private static void AddPixelMarker(GameObject root)
        {
            PixelCameraObject marker = root.GetComponent<PixelCameraObject>();
            if (marker == null)
            {
                marker = root.AddComponent<PixelCameraObject>();
            }
            marker.pixelSize = 2;
            marker.stabilize = false;
            marker.outline = true;
            marker.outlineWidth = 1;
        }

        [MenuItem("DotAction/3D 픽셀 전투 씬 최초 구성")]
        public static void Create()
        {
            if (File.Exists(SCENE_PATH))
            {
                throw new InvalidOperationException("기존 PixelCombat 씬을 직접 편집하세요.");
            }
            foreach (string folder in new[] { "Materials/Combat", "Prefabs/Combat", "Animation", "Data" })
            {
                Directory.CreateDirectory("Assets/" + folder);
            }
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material stone = Material("Stone", new Color32(64, 72, 83, 255));
            Material dark = Material("Dark Stone", new Color32(35, 42, 53, 255));
            Material brass = Material("Antique Brass", new Color32(163, 117, 61, 255));
            Material teal = Material("Aether", new Color32(67, 230, 194, 255), true);
            Material red = Material("Enemy Vitality", new Color32(239, 105, 84, 255), true);

            GameObject gridObject = new GameObject("Combat Grid — 1m cells");
            CombatGrid grid = gridObject.AddComponent<CombatGrid>();
            grid.radius = 10;
            var blocked = new List<Vector2Int>();
            GameObject floor = new GameObject("Courtyard — editable tiles");
            UnityEngine.Random.InitState(157);
            for (int x = -10; x <= 10; x++)
            {
                for (int z = -10; z <= 10; z++)
                {
                    GameObject tile = Box("Cell " + x + "," + z, new Vector3(x, -0.15f, z), new Vector3(.965f, .25f, .965f), (x + z) % 3 == 0 ? dark : stone);
                    tile.transform.SetParent(floor.transform);
                }
            }
            Box("Floating foundation", new Vector3(0, -.55f, 0), new Vector3(21.5f, .6f, 21.5f), dark);
            foreach (Vector2Int cell in new[] { new Vector2Int(-4, -3), new Vector2Int(-4, -2), new Vector2Int(4, 3), new Vector2Int(4, 2), new Vector2Int(-4, 4), new Vector2Int(4, -4) })
            {
                blocked.Add(cell);
                Box("Blocked ruin " + cell, CombatGrid.World(cell) + Vector3.up * .65f, new Vector3(.85f, 1.3f, .85f), dark);
                Box("Ruin cap " + cell, CombatGrid.World(cell) + Vector3.up * 1.35f, new Vector3(.97f, .12f, .97f), brass);
            }
            grid.blockedCells = blocked.ToArray();
            for (int i = -10; i <= 10; i++)
            {
                foreach (Vector3 position in new[] { new Vector3(i, .1f, -10.7f), new Vector3(i, .1f, 10.7f), new Vector3(-10.7f, .1f, i), new Vector3(10.7f, .1f, i) })
                {
                    Box("Boundary curb", position, new Vector3(.9f, .35f, .9f), brass);
                }
            }
            foreach (Vector3 position in new[] { new Vector3(-9, 0, -9), new Vector3(-9, 0, 9), new Vector3(9, 0, -9), new Vector3(9, 0, 9) })
            {
                Box("Beacon plinth", position + Vector3.up * .8f, new Vector3(.5f, 1.6f, .5f), dark);
                GameObject crystal = Box("Aether beacon", position + Vector3.up * 1.9f, new Vector3(.35f, .65f, .35f), teal);
                crystal.transform.rotation = Quaternion.Euler(0, 45, 20);
            }
            var sun = new GameObject("Sun — warm key", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            Light light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1, .86f, .69f);
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.38f, .46f, .6f);

            Camera camera = CreateCamera();
            ArcBolt bolt = CreateBolt(teal);
            CombatActor rangerPrefab = CreateActor("Ranger", "Dwarf_Ranger_Male", 1, true, bolt, teal);
            CombatActor goblinPrefab = CreateActor("Goblin", "Goblin_Warrior_Male", 2, false, null, red);
            CombatActor skeletonPrefab = CreateActor("Skeleton", "Skeleton_Basic", 3, false, null, red);
            CombatActor player = (CombatActor)PrefabUtility.InstantiatePrefab(rangerPrefab);
            player.name = "Player — Dwarf Ranger";
            player.motor.grid = grid;
            player.transform.position = Vector3.zero;
            GameObject ring = Box("Player identification", new Vector3(0, .015f, 0), new Vector3(.8f, .025f, .8f), teal);
            ring.transform.SetParent(player.transform);
            ring.transform.localPosition = new Vector3(0, .015f, 0);

            CombatSession session = new GameObject("Combat Session").AddComponent<CombatSession>();
            session.player = player;
            session.worldCamera = camera;
            session.enemyPrefabs = new[] { goblinPrefab, skeletonPrefab };
            GameObject marker = Box("Move destination", new Vector3(0, .03f, 0), new Vector3(.8f, .035f, .8f), teal);
            session.destinationMarker = marker.transform;
            var route = new GameObject("Grid path preview").AddComponent<LineRenderer>();
            route.sharedMaterial = teal;
            route.startWidth = route.endWidth = .045f;
            route.positionCount = 0;
            route.shadowCastingMode = ShadowCastingMode.Off;
            session.route = route;
            CreateHud(session);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            var scenes = EditorBuildSettings.scenes.Where(item => item.path != SCENE_PATH).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(SCENE_PATH, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SCENE_PATH);
            Debug.Log("DOTACTION_COMBAT_SAVED_REOPENED: " + SCENE_PATH + "; grid=21x21; DDT=CombatStats; waves=3");
        }

        private static Camera CreateCamera()
        {
            UniversalRendererData renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name = "Pixel Combat Forward Renderer";
            string path = "Assets/Data/CombatRenderer.asset";
            AssetDatabase.CreateAsset(renderer, path);
            PixelCameraRendererFeature feature = ScriptableObject.CreateInstance<PixelCameraRendererFeature>();
            feature.name = "Pixel Camera";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
            renderer.SetDirty();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            var serialized = new SerializedObject(pipeline);
            SerializedProperty list = serialized.FindProperty("m_RendererDataList");
            int index = list.arraySize;
            list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            var preset = ScriptableObject.CreateInstance<PixelCameraPreset>();
            preset.name = "Ashen Court 360p";
            preset.pixelization = PixelCameraPixelization.FullCamera;
            preset.referenceHeight = 360;
            preset.resolutionMode = PixelCameraResolutionMode.FixedHeight;
            preset.colorLevels = 32;
            preset.outline = true;
            preset.outlineWidth = 1;
            preset.outlineColor = new Color(.07f, .09f, .13f, .65f);
            preset.stabilizeGrid = false;
            AssetDatabase.CreateAsset(preset, "Assets/Data/CombatPixels.asset");
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PixelCameraEffect));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12.8f;
            camera.transform.position = new Vector3(15, 23, -15);
            camera.transform.LookAt(Vector3.zero);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(14, 19, 29, 255);
            camera.farClipPlane = 100;
            camera.GetUniversalAdditionalCameraData().SetRenderer(index);
            cameraObject.GetComponent<PixelCameraEffect>().preset = preset;
            return camera;
        }

        private static CombatActor CreateActor(string name, string source, int id, bool isPlayer, ArcBolt bolt, Material healthMaterial)
        {
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CREATURE_ROOT + "/_Prefabs/Creatures/" + source + ".prefab"));
            model.transform.SetParent(root.transform, false);
            // 프리팹 연결을 해제한 프로젝트 소유 사본만 변경한다.
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
            {
                UnityEngine.Object.DestroyImmediate(behaviour);
            }
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer item in renderers)
            {
                bounds.Encapsulate(item.bounds);
                item.sharedMaterials = item.sharedMaterials.Select(CopyCreatureMaterial).ToArray();
            }
            float scale = 1.85f / bounds.size.y;
            model.transform.localScale *= scale;
            model.transform.localPosition = Vector3.up * (-bounds.min.y * scale);
            Animator animator = model.GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = CreateAnimator(name, animator.runtimeAnimatorController);
            GridMotor motor = root.AddComponent<GridMotor>();
            motor.visual = model.transform;
            motor.animator = animator;
            CombatActor actor = root.AddComponent<CombatActor>();
            actor.statsId = id;
            actor.isPlayer = isPlayer;
            actor.motor = motor;
            actor.animator = animator;
            actor.boltPrefab = bolt;
            GameObject background = Box("Vitality background", Vector3.zero, new Vector3(.95f, .06f, .13f), Material("Health Background", new Color32(18, 22, 31, 255)));
            background.transform.SetParent(root.transform, false);
            background.transform.localPosition = new Vector3(0, 2.1f, 0);
            GameObject fillPivot = new GameObject("Vitality fill pivot");
            fillPivot.transform.SetParent(root.transform, false);
            fillPivot.transform.localPosition = new Vector3(-.44f, 2.14f, 0);
            GameObject fill = Box("Vitality fill", Vector3.zero, new Vector3(.88f, .04f, .12f), healthMaterial);
            fill.transform.SetParent(fillPivot.transform, false);
            fill.transform.localPosition = new Vector3(.44f, 0, 0);
            actor.healthFill = fillPivot.transform;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Combat/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<CombatActor>();
        }

        private static AnimatorController CreateAnimator(string name, RuntimeAnimatorController original)
        {
            AnimationClip[] clips = original.animationClips;
            AnimationClip Find(string keyword) => clips.FirstOrDefault(clip => clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animation/" + name + ".controller");
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            idle.motion = Find("Idle");
            AnimatorState move = machine.AddState("Move");
            move.motion = Find("Run") ?? Find("Walk");
            AnimatorState attack = machine.AddState("Attack");
            attack.motion = Find("Attack1") ?? Find("Attack");
            AnimatorState dead = machine.AddState("Death");
            dead.motion = Find("Death") ?? Find("Die");
            if (idle.motion == null || move.motion == null || attack.motion == null || dead.motion == null)
            {
                throw new InvalidOperationException(name + " 애니메이션 누락: " + string.Join(", ", clips.Select(clip => clip.name)));
            }
            machine.defaultState = idle;
            Transition(idle, move, "Moving", AnimatorConditionMode.If);
            Transition(move, idle, "Moving", AnimatorConditionMode.IfNot);
            AnimatorStateTransition strike = machine.AddAnyStateTransition(attack);
            strike.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            strike.duration = .05f;
            strike.canTransitionToSelf = true;
            AnimatorStateTransition done = attack.AddTransition(idle);
            done.hasExitTime = true;
            done.exitTime = .85f;
            done.duration = .08f;
            AnimatorStateTransition death = machine.AddAnyStateTransition(dead);
            death.AddCondition(AnimatorConditionMode.If, 0, "Die");
            death.duration = .05f;
            death.canTransitionToSelf = false;
            return controller;
        }

        private static void Transition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode condition)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = .1f;
            transition.AddCondition(condition, 0, parameter);
        }

        private static Material CopyCreatureMaterial(Material source)
        {
            if (source == null)
            {
                return null;
            }
            if (MaterialCopies.TryGetValue(source, out Material existing))
            {
                return existing;
            }
            Material material = new Material(Shader.Find("TurnRoom/Pixel Camera/Toon"));
            material.name = source.name + " Combat";
            material.SetTexture("_BaseMap", source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture);
            material.SetColor("_BaseColor", source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color);
            material.SetFloat("_ToonBands", 4);
            material.SetFloat("_ToonSoftness", .45f);
            material.SetFloat("_AmbientStrength", .8f);
            material.SetColor("_ShadowTint", new Color(.38f, .45f, .58f));
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/Combat/" + material.name + ".mat"));
            MaterialCopies[source] = material;
            return material;
        }

        private static ArcBolt CreateBolt(Material material)
        {
            GameObject root = Box("Aether Bolt", Vector3.zero, new Vector3(.12f, .12f, .5f), material);
            var bolt = root.AddComponent<ArcBolt>();
            var trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = .12f;
            trail.startWidth = .13f;
            trail.endWidth = .01f;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Combat/AetherBolt.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<ArcBolt>();
        }

        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = "Assets/Materials/Combat/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.color = color;
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", .12f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        private static void CreateHud(CombatSession session)
        {
            GameObject root = new GameObject("Combat HUD — edit text and layout here", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            Color cream = new Color32(235, 224, 203, 255);
            Color muted = new Color32(149, 163, 174, 255);
            Color teal = new Color32(98, 225, 197, 255);
            Panel(root.transform, "Header", new Vector2(0, 316), new Vector2(1280, 88), new Color(.035f, .05f, .075f, .94f));
            Panel(root.transform, "Footer", new Vector2(0, -313), new Vector2(1280, 94), new Color(.035f, .05f, .075f, .94f));
            Label(root.transform, "Title", "D O T A C T I O N", new Vector2(-450, 331), new Vector2(330, 34), 26, cream);
            Label(root.transform, "Location", "THE ASHEN COURT   /   COMBAT STUDY 01", new Vector2(-380, 302), new Vector2(470, 24), 13, muted);
            Label(root.transform, "Rendering", "3D  /  PIXEL CAMERA\n640 x 360 WORLD", new Vector2(470, 321), new Vector2(290, 58), 15, teal, TextAnchor.MiddleRight);
            session.healthText = Label(root.transform, "Vitality", "VITALITY", new Vector2(-440, -296), new Vector2(350, 30), 20, teal);
            session.progressText = Label(root.transform, "Progress", "WAVE 0/3", new Vector2(270, -296), new Vector2(690, 30), 19, cream, TextAnchor.MiddleRight);
            Label(root.transform, "Controls", "LMB / HOLD  Move on grid     WASD  Step     AUTO  Aether bolt     R  Restart", new Vector2(0, -330), new Vector2(1220, 28), 15, muted, TextAnchor.MiddleCenter);
            session.resultText = Label(root.transform, "Result", "", new Vector2(0, 20), new Vector2(650, 150), 34, cream, TextAnchor.MiddleCenter);
        }

        private static void Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            root.layer = 5;
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            root.GetComponent<Image>().color = color;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            root.layer = 5;
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Text text = root.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }
    }
}

