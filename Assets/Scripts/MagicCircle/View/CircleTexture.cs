// CircleTexture: the circle drawing as a white texture whose alpha is the ink.
// The editor panel tints it with the element color and the ground tints it
// white, so both always show the same drawing. Buffers are reused, so a redraw
// allocates nothing but the drawing itself.

using System;
using MagicCircleSim.Circle;
using UnityEngine;

namespace MagicCircleSim.View
{
    public sealed class CircleTexture : IDisposable
    {
        public readonly Texture2D Texture;
        readonly int size;
        readonly float[] coverage;
        readonly byte[] pixels;

        public CircleTexture(int size, string name = "MagicCircle Drawing")
        {
            this.size = size;
            coverage = new float[size * size];
            pixels = new byte[size * size * 4];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
            Texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 8,
            };
        }

        public void Draw(CircleDrawing drawing)
        {
            CircleRaster.Rasterize(drawing, size, coverage);
            for (var i = 0; i < coverage.Length; i++) pixels[i * 4 + 3] = (byte)(coverage[i] * 255f + 0.5f);
            Texture.SetPixelData(pixels, 0);
            Texture.Apply(true);
        }

        public void Dispose() => ViewUtil.Destroy(Texture);
    }
}
