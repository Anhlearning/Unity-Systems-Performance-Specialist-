// Import settings for the particle sprite atlases and body stroke textures
// Art~/generate_particles.py draws: straight alpha, clamped so a frame never
// samples its neighbour across the atlas edge, and high-quality compression so
// the soft falloffs do not band.

using UnityEditor;
using UnityEngine;

namespace MagicCircleSim.EditorTools
{
    public sealed class ParticleSpriteImporter : AssetPostprocessor
    {
        static readonly string[] Folders = { "/Resources/MagicCircleParticles/", "/Resources/MagicCircleBodies/" };

        void OnPreprocessTexture()
        {
            if (!System.Array.Exists(Folders, folder => assetPath.Contains(folder))) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 512;
        }
    }
}
