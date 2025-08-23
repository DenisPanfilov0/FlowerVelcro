using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uAI.PixelArtScaler
{
    public static class PixelArtScalingMethods
    {
        public enum ScalingMethod
        {
            KCentroid,
            NearestNeighbor,
            MostCommonColor 
        }

        public static Texture2D ScaleTexture(Texture2D source, int newWidth, int newHeight, ScalingMethod method)
        {
            if (source == null)
                return null;

            Texture2D readableSource = MakeReadableCopy(source);

            Texture2D result;

            switch (method)
            {
                case ScalingMethod.NearestNeighbor:
                    result = NearestNeighbor(readableSource, newWidth, newHeight);
                    break;

                case ScalingMethod.MostCommonColor:
                    result = DownscaleTexture(readableSource, newWidth, newHeight);
                    break;
   
                default:
                    result = NearestNeighbor(readableSource, newWidth, newHeight);
                    break;
            }
            
            if (readableSource != source)
                Object.DestroyImmediate(readableSource);

            return result;
        }
        
        private static Texture2D MakeReadableCopy(Texture2D source)
        {            
            if (source.isReadable)
                return source;

            RenderTexture rt = RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D readableCopy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readableCopy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readableCopy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return readableCopy;
        }
        
        private static Texture2D NearestNeighbor(Texture2D src, int newW, int newH)
        {
            var dst = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
            var srcPixels = src.GetPixels();
            var dstPixels = new Color[newW * newH];

            float xRatio = src.width / (float)newW;
            float yRatio = src.height / (float)newH;

            for (int y = 0; y < newH; y++)
            {
                int sY = Mathf.Clamp(
                    Mathf.RoundToInt((y + 0.5f) * yRatio - 0.5f),
                    0, src.height - 1
                );

                for (int x = 0; x < newW; x++)
                {
                    int sX = Mathf.Clamp(
                        Mathf.RoundToInt((x + 0.5f) * xRatio - 0.5f),
                        0, src.width - 1
                    );
                    dstPixels[y * newW + x] = srcPixels[sY * src.width + sX];
                }
            }

            dst.SetPixels(dstPixels);
            dst.Apply();

            
            dst.filterMode = FilterMode.Point;
            return dst;
        }
        
        
        private static Texture2D DownscaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.filterMode = FilterMode.Point;

            var sourcePixels = source.GetPixels32();
            var targetPixels = new Color32[targetWidth * targetHeight];

            float xRatio = (float)source.width / targetWidth;
            float yRatio = (float)source.height / targetHeight;

            // Pre-allocate dictionary to avoid repeated allocations
            var colorCounts = new Dictionary<Color32, int>(new ColorComparer());

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    Color32 sampledColor = SamplePixelBlock(
                        sourcePixels,
                        source.width,
                        source.height,
                        x * xRatio,
                        y * yRatio,
                        xRatio,
                        yRatio,
                        colorCounts
                    );
                    targetPixels[y * targetWidth + x] = sampledColor;
                }
            }

            result.SetPixels32(targetPixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Improved pixel block sampling with better boundary handling and precision
        /// </summary>
        private static Color32 SamplePixelBlock(
            Color32[] sourcePixels, 
            int sourceWidth, 
            int sourceHeight, 
            float centerX, 
            float centerY, 
            float blockWidth, 
            float blockHeight,
            Dictionary<Color32, int> reusableColorCounts)
        {
            // Clear the reusable dictionary for this sample
            reusableColorCounts.Clear();
            
            // Calculate precise integer bounds for the block
            // Use floor/ceil to ensure we capture all pixels that contribute to this target pixel
            int startX = Mathf.FloorToInt(centerX);
            int startY = Mathf.FloorToInt(centerY);
            int endX = Mathf.CeilToInt(centerX + blockWidth);
            int endY = Mathf.CeilToInt(centerY + blockHeight);

            // Clamp to valid bounds
            startX = Mathf.Max(0, startX);
            startY = Mathf.Max(0, startY);
            endX = Mathf.Min(sourceWidth, endX);
            endY = Mathf.Min(sourceHeight, endY);

            // Count pixel colors in the block
            int totalPixels = 0;
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Color32 pixel = sourcePixels[y * sourceWidth + x];
                    
                    if (reusableColorCounts.TryGetValue(pixel, out int count))
                    {
                        reusableColorCounts[pixel] = count + 1;
                    }
                    else
                    {
                        reusableColorCounts[pixel] = 1;
                    }
                    totalPixels++;
                }
            }

            // Handle edge case where no pixels were sampled
            if (totalPixels == 0 || reusableColorCounts.Count == 0)
            {
                // Fallback to the nearest pixel
                int fallbackX = Mathf.Clamp(Mathf.RoundToInt(centerX), 0, sourceWidth - 1);
                int fallbackY = Mathf.Clamp(Mathf.RoundToInt(centerY), 0, sourceHeight - 1);
                return sourcePixels[fallbackY * sourceWidth + fallbackX];
            }

            // Find the most common color using LINQ for clarity and performance
            return reusableColorCounts.Aggregate((current, next) => 
                next.Value > current.Value ? next : current).Key;
        }
 
        private class ColorComparer : IEqualityComparer<Color32>
        {
            public bool Equals(Color32 x, Color32 y)
            {
                return x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
            }

            public int GetHashCode(Color32 color)
            {
                // Improved hash function with better distribution
                // Using prime numbers to reduce hash collisions
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + color.r;
                    hash = hash * 31 + color.g;
                    hash = hash * 31 + color.b;
                    hash = hash * 31 + color.a;
                    return hash;
                }
            }
        }
 
    }
}