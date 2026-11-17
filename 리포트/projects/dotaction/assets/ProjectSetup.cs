using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace DotAction.Editor
{
    /// <summary>최초 아트 확인 씬을 저장하는 명시적 에디터 도구입니다.</summary>
    public static class ProjectSetup
    {
        private const string SCENE_PATH = "Assets/DotAction/Scenes/ArtReference.unity";
        private const float PIXELS_PER_UNIT = 16f;

        [MenuItem("DotAction/아트 기준 씬 최초 구성")]
        public static void CreateReferenceScene()
        {
            if (File.Exists(SCENE_PATH))
            {
                throw new InvalidOperationException("기존 씬을 보호하기 위해 재생성을 중단했습니다. 저장된 씬을 직접 편집하세요.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory("Assets/DotAction/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 11.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(21, 21, 34, 255);
            camera.transform.position = new Vector3(0, 0, -10);
            PixelPerfectCamera pixelCamera = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelCamera.assetsPPU = 16;
            pixelCamera.refResolutionX = 640;
            pixelCamera.refResolutionY = 360;
            pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelCamera.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;

            AddSprite("Reference Background", "Previews/reference-background.png", 0, 0, 640, 360, -10);
            AddSprite("Player", "characters/player/chr_player.png", 124, 219, 32, 32, 2);
            AddSprite("Enemy Scrapper", "characters/enemies/chr_enemy_scrapper.png", 272, 219, 32, 32, 2);
            string[] symbols = { "sym_blade", "sym_bolt", "sym_guard" };
            for (int index = 0; index < symbols.Length; index++)
            {
                AddSprite("Slot Cell " + index, "ui/ui_slot_cell.png", 192 + index * 32, 132, 32, 32, 1);
                AddSprite("Slot Symbol " + index, "symbols/" + symbols[index] + ".png", 200 + index * 32, 140, 16, 16, 2);
            }
            AddSprite("Augment Card Frame", "cards/ui_card_frame.png", 476, 110, 96, 128, 1);
            AddSprite("Card Bolt Shared Symbol", "symbols/sym_bolt.png", 516, 146, 16, 16, 2);
            AddText("Card Title", "BOLT AUGMENT", 484, 122, 0.65f, new Color32(231, 220, 203, 255));
            AddText("Card Description", "BOLT EFFECT\nUPGRADE", 484, 182, 0.55f, new Color32(131, 214, 190, 255));
            AddText("Card Rarity", "AUGMENT II", 484, 222, 0.5f, new Color32(210, 172, 224, 255));
            AddText("Player Label", "PLAYER", 122, 263, 0.45f, new Color32(131, 214, 190, 255));
            AddText("Enemy Label", "SCRAPPER", 264, 263, 0.45f, new Color32(245, 160, 129, 255));
            AddText("Slot Label", "SLOT", 228, 174, 0.45f, new Color32(242, 198, 109, 255));

            PlayerSettings.companyName = "DotAction";
            PlayerSettings.productName = "DotAction";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            QualitySettings.vSyncCount = 1;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE_PATH, true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            Debug.Log("DOTAction_SETUP_OK: persisted and reopened ArtReference scene; 640x360, PPU16, Point sprites.");
        }

        private static void AddSprite(string name, string relativePath, int x, int y, int width, int height, int order)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DotAction/Art/" + relativePath);
            if (sprite == null)
            {
                throw new FileNotFoundException("필수 아트가 없습니다: " + relativePath);
            }

            GameObject item = new GameObject(name, typeof(SpriteRenderer));
            item.transform.position = new Vector3((x + width * 0.5f - 320) / PIXELS_PER_UNIT,
                (180 - y - height * 0.5f) / PIXELS_PER_UNIT, 0);
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            Directory.CreateDirectory("Assets/DotAction/Materials");
            string materialPath = "Assets/DotAction/Materials/" + sprite.name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                material.mainTexture = sprite.texture;
                AssetDatabase.CreateAsset(material, materialPath);
            }
            renderer.sharedMaterial = material;
        }

        private static void AddText(string name, string value, int x, int y, float size, Color color)
        {
            GameObject item = new GameObject(name, typeof(TextMesh));
            item.transform.position = new Vector3((x - 320) / PIXELS_PER_UNIT, (180 - y) / PIXELS_PER_UNIT, -0.1f);
            TextMesh text = item.GetComponent<TextMesh>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.characterSize = 0.18f;
            text.anchor = TextAnchor.UpperLeft;
            text.color = color;
            MeshRenderer renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = text.font.material;
            renderer.sortingOrder = 5;
        }

        /// <summary>실제 씬을 렌더하고 별도 파일에 저장합니다. 테스트가 아닌 실행 근거입니다.</summary>
        public static void CaptureReferenceScene()
        {
            BeginCapture(SCENE_PATH, "art/previews/unity-art-reference.png");
        }

        public static void CaptureTemplateScene()
        {
            BeginCapture("Assets/Scenes/SampleScene.unity", "art/previews/unity-before.png");
        }

        private static void BeginCapture(string scenePath, string outputPath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SessionState.SetString("DotAction.CapturePath", outputPath);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void ResumeCaptureAfterReload()
        {
            string outputPath = SessionState.GetString("DotAction.CapturePath", string.Empty);
            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }
            int frames = 0;
            EditorApplication.CallbackFunction capture = null;
            capture = () =>
            {
                if (!EditorApplication.isPlaying || ++frames < 60)
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    return;
                }
                EditorApplication.update -= capture;
                SessionState.EraseString("DotAction.CapturePath");
                try
                {
                    CaptureCamera(outputPath);
                    EditorApplication.Exit(0);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    EditorApplication.Exit(1);
                }
            };
            EditorApplication.update += capture;
        }

        private static void CaptureCamera(string outputPath)
        {
            Camera camera = Camera.main;
            RenderTexture target = new RenderTexture(1280, 720, 24);
            UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
            request.destination = target;
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            Directory.CreateDirectory("art/previews");
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            Scene scene = SceneManager.GetActiveScene();
            Debug.Log("DOTAction_CAPTURE_OK: " + scene.path + "; objects=" + scene.rootCount + "; output=" + outputPath);
        }
    }
}
