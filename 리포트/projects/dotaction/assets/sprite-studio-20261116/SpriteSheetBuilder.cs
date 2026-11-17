using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DotAction.Editor.SpriteStudio
{
    /// <summary>Studio의 PNG/JSON을 고정 프레임과 실제 밀리초 길이를 보존해 가져옵니다.</summary>
    public static class SpriteSheetBuilder
    {
        [Serializable] private sealed class Sheet { public Frame[] frames; public Metadata meta; }
        [Serializable] private sealed class Frame { public string filename; public Area frame; public int duration; }
        [Serializable] private sealed class Area { public int x; public int y; public int w; public int h; }
        [Serializable] private sealed class Metadata { public string app; public Tag[] frameTags; }
        [Serializable] private sealed class Tag { public string name; public int from; public int to; }

        /// <summary>배치 모드에서 -spriteStudioSheet 경로의 시트를 가져옵니다.</summary>
        public static void BuildFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-spriteStudioSheet");
            if (index < 0 || index + 1 >= arguments.Length)
            {
                throw new ArgumentException("-spriteStudioSheet Assets/DotAction/Art/sprite-studio/파일.png 필요");
            }
            Build(arguments[index + 1]);
        }

        [MenuItem("DotAction/Sprite Studio/선택한 시트에서 애니메이션 만들기")]
        public static void BuildSelected()
        {
            string imagePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!imagePath.StartsWith("Assets/DotAction/Art/sprite-studio/", StringComparison.Ordinal)
                || !imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("동기화한 Art/sprite-studio 폴더의 PNG 시트를 선택하세요.");
            }
            Build(imagePath);
        }

        /// <summary>씬을 변경하지 않고 시트 옆에 새 클립·컨트롤러·프리팹을 저장합니다.</summary>
        public static void Build(string imagePath)
        {
            if (!imagePath.StartsWith("Assets/DotAction/Art/sprite-studio/", StringComparison.Ordinal)
                || imagePath.Contains("..") || !imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Studio 전용 출력 폴더의 PNG만 처리합니다.");
            }
            Sheet sheet = JsonUtility.FromJson<Sheet>(File.ReadAllText(Path.ChangeExtension(imagePath, ".json")));
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            if (sheet?.meta?.app != "DotAction Sprite Studio" || sheet.frames == null || sheet.frames.Length == 0 || texture == null)
            {
                throw new InvalidDataException("Studio PNG와 같은 이름의 유효한 JSON이 필요합니다.");
            }
            foreach (Frame frame in sheet.frames)
            {
                Area area = frame.frame;
                if (area == null || area.w <= 0 || area.h <= 0 || area.x < 0 || area.y < 0
                    || area.x + area.w > texture.width || area.y + area.h > texture.height || frame.duration < 10)
                {
                    throw new InvalidDataException("프레임 영역 또는 시간이 잘못되었습니다.");
                }
            }
            if (sheet.meta.frameTags == null || sheet.meta.frameTags.Length == 0)
            {
                throw new InvalidDataException("동작 태그가 없습니다.");
            }
            foreach (Tag tag in sheet.meta.frameTags)
            {
                if (tag.from < 0 || tag.to < tag.from || tag.to >= sheet.frames.Length)
                {
                    throw new InvalidDataException("태그 프레임 범위가 잘못되었습니다.");
                }
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
            SpriteRect[] rectangles = sheet.frames.Select((frame, index) => new SpriteRect
            {
                name = index + "_" + frame.filename,
                rect = new Rect(frame.frame.x, texture.height - frame.frame.y - frame.frame.h, frame.frame.w, frame.frame.h),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0f),
                spriteID = previous.TryGetValue(index + "_" + frame.filename, out GUID guid) ? guid : GUID.Generate()
            }).ToArray();
            provider.SetSpriteRects(rectangles);
            ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
            {
                names.SetNameFileIdPairs(rectangles.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            }
            provider.Apply();
            importer.SaveAndReimport();

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(imagePath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            string folder = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(imagePath, null) + "_animation");
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "/animation.controller");
            for (int tagIndex = 0; tagIndex < sheet.meta.frameTags.Length; tagIndex++)
            {
                Tag tag = sheet.meta.frameTags[tagIndex];
                AnimationClip clip = new AnimationClip { name = tag.name, frameRate = 100 };
                List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();
                float time = 0;
                for (int index = tag.from; index <= tag.to; index++)
                {
                    keys.Add(new ObjectReferenceKeyframe { time = time, value = sprites[rectangles[index].name] });
                    time += sheet.frames[index].duration / 1000f;
                }
                keys.Add(new ObjectReferenceKeyframe { time = time, value = keys[keys.Count - 1].value });
                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                settings.stopTime = time;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, folder + "/clip_" + tagIndex + ".anim");
                controller.AddMotion(clip);
            }
            GameObject character = new GameObject(Path.GetFileNameWithoutExtension(imagePath));
            try
            {
                character.AddComponent<SpriteRenderer>().sprite = sprites[rectangles[0].name];
                character.AddComponent<Animator>().runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(character, folder + "/character.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SPRITE_STUDIO_IMPORT_OK: " + sheet.frames.Length + " frames; " + folder);
        }
    }
}
