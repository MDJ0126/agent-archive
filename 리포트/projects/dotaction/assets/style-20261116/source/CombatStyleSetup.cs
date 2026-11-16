using System;
using System.IO;
using System.Linq;
using DotAction.Rendering;
using TurnRoom.PixelCamera;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DotAction.Editor
{
    /// <summary>
    /// 저장된 전투 자산에 빌보드·12fps 포즈·공통 픽셀 격자와 팔레트를 적용한다.
    /// </summary>
    public static class CombatStyleSetup
    {
        [Serializable] private sealed class PaletteSource { public PaletteColor[] colors; }
        [Serializable] private sealed class PaletteColor { public string hex; }

        public static void CaptureBefore()
        {
            SessionState.SetBool("DotAction.ShortCapture", true);
            CombatCapture.Run();
        }

        public static void ApplyAndCapture()
        {
            Apply();
            SessionState.SetBool("DotAction.ShortCapture", true);
            CombatCapture.Run();
        }

        [MenuItem("DotAction/전투 스타일/빌보드·12fps·팔레트·아이소메트릭 적용")]
        public static void Apply()
        {
            foreach (string name in new[] { "Ranger", "Goblin", "Skeleton" })
            {
                string path = "Assets/Prefabs/Combat/" + name + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                ConfigureActor(root.GetComponent<CombatActor>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            foreach (string path in new[] { "Assets/Scenes/ArtReference.unity", "Assets/Scenes/SampleScene.unity" })
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                var oldScene = EditorSceneManager.OpenScene(path);
                foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    camera.GetUniversalAdditionalCameraData().SetRenderer(0);
                }
                EditorSceneManager.SaveScene(oldScene);
            }
            var scene = EditorSceneManager.OpenScene(CombatSceneSetup.SCENE_PATH);
            Camera main = Camera.main;
            main.transform.rotation = Quaternion.Euler(35.2643897f, 45, 0);
            CombatCameraFollow follow = main.GetComponent<CombatCameraFollow>();
            follow.offset = -main.transform.forward * 20;
            main.transform.position = follow.target.position + follow.offset;
            CombatPixelStyle style = main.GetComponent<CombatPixelStyle>();
            if (style == null)
            {
                style = main.gameObject.AddComponent<CombatPixelStyle>();
            }
            style.referenceHeight = 360;
            style.palette = CreatePalette();
            // 전체 화면 확장은 선택 오브젝트 합성과 중복 적용하지 않는다.
            main.GetComponent<PixelCameraEffect>().enabled = false;
            foreach (CombatActor actor in UnityEngine.Object.FindObjectsByType<CombatActor>(FindObjectsSortMode.None))
            {
                ConfigureActor(actor);
            }
            foreach (PixelCameraObject marker in UnityEngine.Object.FindObjectsByType<PixelCameraObject>(FindObjectsSortMode.None))
            {
                marker.pixelSize = 1;
                marker.stabilize = false;
                marker.RefreshRenderers();
            }
            PixelCameraPreset preset = main.GetComponent<PixelCameraEffect>().preset;
            preset.colorLevels = 256;
            preset.palette = null;
            preset.orderedDithering = false;
            EditorUtility.SetDirty(preset);
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Data/CombatRenderer.asset");
            CombatPixelRendererFeature feature = renderer.rendererFeatures.OfType<CombatPixelRendererFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<CombatPixelRendererFeature>();
                feature.name = "Uniform pixels + DotAction 32 palette";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
            }
            feature.shader = Shader.Find("DotAction/Uniform Pixel Palette");
            feature.Create();
            EditorUtility.SetDirty(feature);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            var pipeline = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset"));
            pipeline.FindProperty("m_DefaultRendererIndex").intValue = 1;
            pipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(scene.path);
            Debug.Log("DOTACTION_STYLE_SAVED: default renderer=3D; isometric=(35.264,45); pose=12fps; palette=32; final integer grid");
        }

        private static PixelCameraPalette CreatePalette()
        {
            const string path = "Assets/Data/CombatPalette.asset";
            PixelCameraPalette palette = AssetDatabase.LoadAssetAtPath<PixelCameraPalette>(path);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<PixelCameraPalette>();
                AssetDatabase.CreateAsset(palette, path);
            }
            string sourcePath = Directory.GetFiles("art/palettes", "*.json").First();
            PaletteSource source = JsonUtility.FromJson<PaletteSource>(File.ReadAllText(sourcePath));
            palette.colors = source.colors.Select(item => ColorUtility.TryParseHtmlString(item.hex, out Color color) ? color : throw new FormatException(item.hex)).ToArray();
            palette.useLookup = false;
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static void ConfigureActor(CombatActor actor)
        {
            Transform group = actor.transform.Find("Health Billboard");
            if (group == null)
            {
                group = new GameObject("Health Billboard").transform;
                group.SetParent(actor.transform, false);
            }
            group.localPosition = new Vector3(0, 2.1f, 0);
            group.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.Euler(35.2643897f, 45, 0);
            if (group.GetComponent<HealthBillboard>() == null)
            {
                group.gameObject.AddComponent<HealthBillboard>();
            }
            Transform background = actor.GetComponentsInChildren<Transform>(true).First(item => item.name == "Vitality background");
            background.SetParent(group, false);
            background.localPosition = Vector3.zero;
            background.localRotation = Quaternion.identity;
            background.localScale = new Vector3(.95f, .10f, .035f);
            actor.healthFill.SetParent(group, false);
            actor.healthFill.localPosition = new Vector3(-.44f, 0, -.03f);
            actor.healthFill.localRotation = Quaternion.identity;
            Transform fill = actor.healthFill.GetChild(0);
            fill.localPosition = new Vector3(.44f, 0, 0);
            fill.localRotation = Quaternion.identity;
            fill.localScale = new Vector3(.88f, .06f, .03f);
            foreach (Renderer renderer in group.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            PixelCameraSteppedAnimator stepped = actor.animator.GetComponent<PixelCameraSteppedAnimator>();
            if (stepped == null)
            {
                stepped = actor.animator.gameObject.AddComponent<PixelCameraSteppedAnimator>();
            }
            stepped.framesPerSecond = 12;
            foreach (Renderer renderer in actor.motor.visual.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (actor.isPlayer && material != null && material.name.Contains("Dwarf_Ranger"))
                    {
                        Texture texture = material.GetTexture("_BaseMap");
                        material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                        material.SetTexture("_BaseMap", texture);
                        material.SetColor("_BaseColor", new Color(.85f, 1, 1, 1));
                        EditorUtility.SetDirty(material);
                        continue;
                    }
                    if (actor.isPlayer && material != null && material.HasProperty("_AmbientStrength"))
                    {
                        material.SetFloat("_AmbientStrength", 1.4f);
                        material.SetColor("_ShadowTint", new Color(.75f, .8f, .85f));
                        material.SetColor("_RimColor", new Color(.15f, .7f, .8f));
                        material.SetColor("_EmissionColor", new Color(.025f, .12f, .13f));
                        EditorUtility.SetDirty(material);
                    }
                }
            }
            PixelCameraObject marker = actor.GetComponent<PixelCameraObject>();
            if (marker != null)
            {
                marker.pixelSize = 1;
                marker.RefreshRenderers();
            }
        }
    }
}
