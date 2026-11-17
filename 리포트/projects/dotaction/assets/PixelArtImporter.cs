using UnityEditor;
using UnityEngine;

namespace DotAction.Editor
{
    /// <summary>프로젝트 소유 픽셀 아트를 압축·필터링 없이 가져옵니다.</summary>
    public sealed class PixelArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/DotAction/Art/"))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = assetPath.StartsWith("Assets/DotAction/Art/sprite-studio/")
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
        }
    }
}
