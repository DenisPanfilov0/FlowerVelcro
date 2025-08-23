using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace uAI.PixelArtScaler
{
    public static class PixelDetectorHelper
    {
        public static bool forceUniformSpacing = true;
 
 
        public struct GridSnapResult
        {
            public Texture2D snappedTexture;
            public Vector2Int cropOffset;
            public Vector2Int originalSize;
            public Vector2Int finalSize;
            public bool success;

            public GridSnapResult(Texture2D texture, Vector2Int offset, Vector2Int origSize, Vector2Int newSize, bool wasSuccessful)
            {
                snappedTexture = texture;
                cropOffset = offset;
                originalSize = origSize;
                finalSize = newSize;
                success = wasSuccessful;
            }
        }

        public static GridSnapResult ApplyGridSnapping(Texture2D source, float detectedScale, bool enableGridSnapping = true)
        {
            if (source == null)
            {
                Debug.LogError("ApplyGridSnapping: Source texture is null");
                return new GridSnapResult(null, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero, false);
            }

            if (!enableGridSnapping || detectedScale <= 1)
            { 
                return new GridSnapResult(source, Vector2Int.zero,
                    new Vector2Int(source.width, source.height),
                    new Vector2Int(source.width, source.height), true);
            }

            var readableSource = EnsureTextureIsReadable(source);
            if (readableSource == null)
            {
                Debug.LogError("ApplyGridSnapping: Could not make texture readable");
                return new GridSnapResult(null, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero, false);
            }
 

            try
            {
                var originalSize = new Vector2Int(source.width, source.height);
                int intScale = Mathf.RoundToInt(detectedScale);
 
                var cropOffset = FindOptimalCropOffset(readableSource, intScale);
 
                var newWidth = Mathf.FloorToInt((float)(source.width - cropOffset.x) / intScale) * intScale;
                var newHeight = Mathf.FloorToInt((float)(source.height - cropOffset.y) / intScale) * intScale;
                var finalSize = new Vector2Int(newWidth, newHeight);

                if (newWidth < intScale || newHeight < intScale)
                {
                    Debug.LogWarning($"Snapping failed: resulting image size ({newWidth}x{newHeight}) is too small. Skipping snap.");
                    return new GridSnapResult(source, Vector2Int.zero, originalSize, originalSize, false);
                }
 
                var croppedTexture = CropTexture(readableSource, cropOffset.x, cropOffset.y, newWidth, newHeight); 

                return new GridSnapResult(croppedTexture, cropOffset, originalSize, finalSize, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Grid snapping failed with exception: {e.Message}");
                return new GridSnapResult(source, Vector2Int.zero,
                    new Vector2Int(source.width, source.height),
                    new Vector2Int(source.width, source.height), false);
            }
        } 
        private static Vector2Int FindOptimalCropOffset(Texture2D texture, int scale)
        {
            var pixels = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;

            
            var grayscale = new float[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                
                grayscale[i] = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
            }

            
            var profileX = CalculateHorizontalGradientProfile(grayscale, width, height);
            var profileY = CalculateVerticalGradientProfile(grayscale, width, height);

            
            var bestOffsetX = FindBestOffset(profileX, scale);
            var bestOffsetY = FindBestOffset(profileY, scale);

            
            return new Vector2Int(bestOffsetX, bestOffsetY);
        }
 
        private static float[] CalculateHorizontalGradientProfile(float[] grayscale, int width, int height)
        {
            var profile = new float[width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 1; x < width; x++) 
                {
                    var idx1 = y * width + x;
                    var idx2 = y * width + (x - 1);
                    var gradient = Mathf.Abs(grayscale[idx1] - grayscale[idx2]);
                    profile[x] += gradient;
                }
            }

            return profile;
        }
 
        private static float[] CalculateVerticalGradientProfile(float[] grayscale, int width, int height)
        {
            var profile = new float[height];

            for (int y = 1; y < height; y++) 
            {
                for (int x = 0; x < width; x++)
                {
                    var idx1 = y * width + x;
                    var idx2 = (y - 1) * width + x;
                    var gradient = Mathf.Abs(grayscale[idx1] - grayscale[idx2]);
                    profile[y] += gradient;
                }
            }

            return profile;
        }
 
        private static int FindBestOffset(float[] profile, int scale)
        {
            var bestOffset = 0;
            var maxScore = -1f;
 
            for (int offset = 0; offset < scale; offset++)
            {
                var currentScore = 0f;
 
                for (int i = offset; i < profile.Length; i += scale)
                {
                    currentScore += profile[i];
                }

                if (currentScore > maxScore)
                {
                    maxScore = currentScore;
                    bestOffset = offset;
                }
            }

            return bestOffset;
        } 

        private static Texture2D CropTexture(Texture2D source, int x, int y, int width, int height)
        {
            var sourcePixels = source.GetPixels32();
            var croppedPixels = new Color32[width * height];

            for (int cy = 0; cy < height; cy++)
            {
                for (int cx = 0; cx < width; cx++)
                {
                    var sourceX = x + cx;
                    var sourceY = y + cy;

                    if (sourceX >= 0 && sourceX < source.width && sourceY >= 0 && sourceY < source.height)
                    {
                        var sourceIndex = sourceY * source.width + sourceX;
                        var cropIndex = cy * width + cx;
                        croppedPixels[cropIndex] = sourcePixels[sourceIndex];
                    } 
                }
            }

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(croppedPixels);
            result.Apply();
            result.filterMode = FilterMode.Point;

            return result;
        } 
         


        #region Unchanged Methods (Quantization, Palettes, KCentroid, etc.) 
        
        public static Texture2D ApplyCustomPalette(Texture2D source, Color32[] palette, bool ditherEnabled = false) { if (source == null || palette == null || palette.Length == 0) return source; var readableSource = EnsureTextureIsReadable(source); if (readableSource == null) return source; var pixels = readableSource.GetPixels32(); var quantizedPixels = new Color32[pixels.Length]; for (int i = 0; i < pixels.Length; i++) { quantizedPixels[i] = FindNearestPaletteColor(pixels[i], palette); } if (ditherEnabled) { quantizedPixels = ApplyFloydSteinbergDithering(pixels, quantizedPixels, source.width, source.height); } var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false); result.SetPixels32(quantizedPixels); result.Apply(); result.filterMode = FilterMode.Point; return result; }
        private static Color32[] ApplyFloydSteinbergDithering(Color32[] originalPixels, Color32[] quantizedPixels, int width, int height)
        {
            var result = new Color32[originalPixels.Length];

            
            var workingImage = new Vector3[width * height];
            for (int i = 0; i < originalPixels.Length; i++)
            {
                workingImage[i] = new Vector3(originalPixels[i].r, originalPixels[i].g, originalPixels[i].b);
            }

            
            var palette = new HashSet<Color32>(quantizedPixels, new ColorComparer()).ToArray();

            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Vector3 oldColor = workingImage[index];

                    
                    Color32 clampedColor = new Color32(
                        (byte)Mathf.Clamp(oldColor.x, 0, 255),
                        (byte)Mathf.Clamp(oldColor.y, 0, 255),
                        (byte)Mathf.Clamp(oldColor.z, 0, 255),
                        originalPixels[index].a
                    );

                    Color32 newColor = FindNearestPaletteColor(clampedColor, palette);
                    result[index] = newColor;

                    
                    Vector3 error = oldColor - new Vector3(newColor.r, newColor.g, newColor.b);

                    

                    
                    if (x + 1 < width)
                    {
                        workingImage[y * width + (x + 1)] += error * (7f / 16f);
                    }

                    
                    if (y + 1 < height)
                    {
                        
                        if (x > 0)
                        {
                            workingImage[(y + 1) * width + (x - 1)] += error * (3f / 16f);
                        }

                        
                        workingImage[(y + 1) * width + x] += error * (5f / 16f);

                        
                        if (x + 1 < width)
                        {
                            workingImage[(y + 1) * width + (x + 1)] += error * (1f / 16f);
                        }
                    }
                }
            }

            return result;
        }

        
        private static Color32 FindNearestPaletteColor(Color32 color, Color32[] palette) { if (palette.Length == 0) return color; var colorVector = new Vector3(color.r, color.g, color.b); float minDist = float.MaxValue; int bestIdx = 0; for (int i = 0; i < palette.Length; i++) { var paletteVector = new Vector3(palette[i].r, palette[i].g, palette[i].b); float dist = ColorDistance(colorVector, paletteVector); if (dist < minDist) { minDist = dist; bestIdx = i; } } return new Color32(palette[bestIdx].r, palette[bestIdx].g, palette[bestIdx].b, color.a); }
        private class OctreeQuantizer { private class OctreeNode { public OctreeNode[] children = new OctreeNode[8]; public OctreeNode parent; public bool isLeaf = false; public int pixelCount = 0; public long redSum = 0; public long greenSum = 0; public long blueSum = 0; public int level; private OctreeQuantizer quantizer; public OctreeNode(int level, OctreeQuantizer quantizer, OctreeNode parent = null) { this.level = level; this.quantizer = quantizer; this.parent = parent; if (level == 7) { isLeaf = true; quantizer.leafNodes.Add(this); } } public void AddColor(Color32 color, int level) { if (isLeaf) { pixelCount++; redSum += color.r; greenSum += color.g; blueSum += color.b; } else { int index = GetColorIndex(color, level); if (children[index] == null) { children[index] = new OctreeNode(level + 1, quantizer, this); } children[index].AddColor(color, level + 1); } } private int GetColorIndex(Color32 color, int level) { int index = 0; int mask = 0x80 >> level; if ((color.r & mask) != 0) index |= 4; if ((color.g & mask) != 0) index |= 2; if ((color.b & mask) != 0) index |= 1; return index; } public Color32 GetAverageColor() { if (pixelCount == 0) return new Color32(0, 0, 0, 255); return new Color32((byte)(redSum / pixelCount), (byte)(greenSum / pixelCount), (byte)(blueSum / pixelCount), 255); } public void ReduceNode() { foreach (var child in children) { if (child != null) { pixelCount += child.pixelCount; redSum += child.redSum; greenSum += child.greenSum; blueSum += child.blueSum; if (child.isLeaf) { quantizer.leafNodes.Remove(child); } } } isLeaf = true; quantizer.leafNodes.Add(this); children = new OctreeNode[8]; } } private OctreeNode root; private int maxColors; private List<OctreeNode> leafNodes; public OctreeQuantizer(int maxColors) { this.maxColors = maxColors; this.root = new OctreeNode(0, this); this.leafNodes = new List<OctreeNode>(); } public void AddColor(Color32 color) { root.AddColor(color, 0); } public Color32[] GetPalette() { while (leafNodes.Count > maxColors) { var leastPopular = leafNodes.OrderBy(n => n.pixelCount).First(); leastPopular.parent?.ReduceNode(); } return leafNodes.Select(n => n.GetAverageColor()).ToArray(); } public Color32 GetNearestColor(Color32 color) { var palette = GetPalette(); return FindNearestPaletteColor(color, palette); } }
        public static Texture2D KCentroid(Texture2D source, int targetWidth, int targetHeight, int centroids) { var srcPixels = source.GetPixels32(); int srcW = source.width, srcH = source.height; var downscaled = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false); var downPixels = new Color32[targetWidth * targetHeight]; float tileW = (float)srcW / targetWidth; float tileH = (float)srcH / targetHeight; for (int y = 0; y < targetHeight; y++) for (int x = 0; x < targetWidth; x++) downPixels[y * targetWidth + x] = ProcessTile(srcPixels, srcW, srcH, x * tileW, y * tileH, tileW, tileH, centroids); downscaled.SetPixels32(downPixels); downscaled.Apply(); return downscaled; }
        public static Texture2D[] TryDifferentGridOffsets(Texture2D source, int targetWidth, int targetHeight, int centroids, int offsetSteps = 8) { int srcW = source.width, srcH = source.height; float tileW = (float)srcW / targetWidth; float tileH = (float)srcH / targetHeight; var textures = new List<Texture2D>(); if (forceUniformSpacing) { for (int i = 0; i < offsetSteps; i++) { float ox = (i / (float)offsetSteps) * tileW; float oy = ox; float error; var candidate = KCentroidWithOffset(source, targetWidth, targetHeight, centroids, ox, oy, out error); textures.Add(candidate); } } else { for (int ix = 0; ix < offsetSteps; ix++) for (int iy = 0; iy < offsetSteps; iy++) { float ox = (ix / (float)offsetSteps) * tileW; float oy = (iy / (float)offsetSteps) * tileH; float error; var candidate = KCentroidWithOffset(source, targetWidth, targetHeight, centroids, ox, oy, out error); textures.Add(candidate); } } return textures.ToArray(); }
        private static Texture2D KCentroidWithOffset(Texture2D source, int targetWidth, int targetHeight, int centroids, float offsetX, float offsetY, out float totalError) { var srcPixels = source.GetPixels32(); int srcW = source.width, srcH = source.height; var downscaled = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false); var downPixels = new Color32[targetWidth * targetHeight]; float tileW = (float)srcW / targetWidth; float tileH = (float)srcH / targetHeight; totalError = 0f; for (int y = 0; y < targetHeight; y++) { for (int x = 0; x < targetWidth; x++) { float x0 = x * tileW + offsetX; float y0 = y * tileH + offsetY; var tileColor = ProcessTile(srcPixels, srcW, srcH, x0, y0, tileW, tileH, centroids); downPixels[y * targetWidth + x] = tileColor; int xStart = Mathf.FloorToInt(x0); int yStart = Mathf.FloorToInt(y0); int xEnd = Mathf.Min(Mathf.CeilToInt(x0 + tileW), srcW); int yEnd = Mathf.Min(Mathf.CeilToInt(y0 + tileH), srcH); for (int yy = yStart; yy < yEnd; yy++) for (int xx = xStart; xx < xEnd; xx++) { var p = srcPixels[yy * srcW + xx]; totalError += (p.r - tileColor.r) * (p.r - tileColor.r) + (p.g - tileColor.g) * (p.g - tileColor.g) + (p.b - tileColor.b) * (p.b - tileColor.b); } } } downscaled.SetPixels32(downPixels); downscaled.Apply(); return downscaled; }
        private static Color32 ProcessTile(Color32[] srcPixels, int srcW, int srcH, float x0, float y0, float width, float height, int centroids) { int startX = Mathf.Clamp(Mathf.FloorToInt(x0), 0, srcW - 1), startY = Mathf.Clamp(Mathf.FloorToInt(y0), 0, srcH - 1), endX = Mathf.Clamp(Mathf.CeilToInt(x0 + width), 0, srcW), endY = Mathf.Clamp(Mathf.CeilToInt(y0 + height), 0, srcH); var tilePixels = new List<Color32>(); for (int y = startY; y < endY; y++) { for (int x = startX; x < endX; x++) { float normalizedX = (x + 0.5f - x0) / width; float normalizedY = (y + 0.5f - y0) / height; if (normalizedX >= 0 && normalizedX < 1 && normalizedY >= 0 && normalizedY < 1) { int idx = y * srcW + x; if (idx >= 0 && idx < srcPixels.Length) tilePixels.Add(srcPixels[idx]); } } } if (tilePixels.Count == 0) return new Color32(0, 0, 0, 255); var colorCounts = new Dictionary<Color32, int>(new ColorComparer()); foreach (var pixel in tilePixels) { if (!colorCounts.ContainsKey(pixel)) colorCounts[pixel] = 0; colorCounts[pixel]++; } if (colorCounts.Count <= centroids) { return colorCounts.OrderByDescending(kv => kv.Value).First().Key; } var quantized = EnhancedQuantization(tilePixels.ToArray(), centroids); var quantizedCounts = new Dictionary<Color32, int>(new ColorComparer()); foreach (var color in quantized) { if (!quantizedCounts.ContainsKey(color)) quantizedCounts[color] = 0; quantizedCounts[color]++; } return quantizedCounts.OrderByDescending(kv => kv.Value).First().Key; }
        private static Color32[] EnhancedQuantization(Color32[] pixels, int k) { if (pixels.Length == 0 || k < 1) return new Color32[0]; var initialCentroids = ImprovedMedianCut(pixels, k); var refinedCentroids = RefineWithKMeans(pixels, initialCentroids); var result = new Color32[pixels.Length]; for (int i = 0; i < pixels.Length; i++) { result[i] = FindNearestCentroid(pixels[i], refinedCentroids); } return result; }
        private static Color32[] ImprovedMedianCut(Color32[] pixels, int k) { var cubes = new List<ColorBox> { new ColorBox(pixels) }; while (cubes.Count < k) { int largestIdx = -1; double largestVolume = -1; for (int i = 0; i < cubes.Count; i++) { double volume = cubes[i].Volume(); if (volume > largestVolume) { largestVolume = volume; largestIdx = i; } } if (largestIdx == -1 || largestVolume <= 0) break; var box = cubes[largestIdx]; if (box.CanSplit()) { cubes.RemoveAt(largestIdx); var splitResult = box.Split(); cubes.Add(splitResult.Item1); cubes.Add(splitResult.Item2); } else { break; } } return cubes.Select(c => c.AverageColor()).ToArray(); }
        private static Color32[] RefineWithKMeans(Color32[] pixels, Color32[] initialCentroids, int maxIterations = 12) { int k = initialCentroids.Length; var centroids = initialCentroids.ToArray(); var newCentroids = new Color32[k]; var centroidVectors = centroids.Select(c => new Vector3(c.r, c.g, c.b)).ToArray(); bool changed = true; double lastTotalDistance = double.MaxValue; for (int iter = 0; iter < maxIterations && changed; iter++) { var sums = new Vector3[k]; var counts = new int[k]; changed = false; double totalDistance = 0; foreach (var pixel in pixels) { var pixelVector = new Vector3(pixel.r, pixel.g, pixel.b); int nearestIdx = 0; float minDist = float.MaxValue; for (int i = 0; i < k; i++) { float dist = ColorDistance(pixelVector, centroidVectors[i]); if (dist < minDist) { minDist = dist; nearestIdx = i; } } sums[nearestIdx] += pixelVector; counts[nearestIdx]++; totalDistance += minDist; } for (int i = 0; i < k; i++) { if (counts[i] > 0) { Vector3 newCentroid = sums[i] / counts[i]; newCentroids[i] = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(newCentroid.x), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(newCentroid.y), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(newCentroid.z), 0, 255), 255); if (ColorDistance(newCentroid, centroidVectors[i]) > 0.5f) changed = true; centroidVectors[i] = newCentroid; } } double distanceChange = Math.Abs(lastTotalDistance - totalDistance) / Math.Max(1.0, lastTotalDistance); if (distanceChange < 0.01) changed = false; lastTotalDistance = totalDistance; Array.Copy(newCentroids, centroids, k); } return centroids; }
        private static Color32 FindNearestCentroid(Color32 color, Color32[] centroids) { if (centroids.Length == 0) return color; var colorVector = new Vector3(color.r, color.g, color.b); float minDist = float.MaxValue; int bestIdx = 0; for (int i = 0; i < centroids.Length; i++) { var centroidVector = new Vector3(centroids[i].r, centroids[i].g, centroids[i].b); float dist = ColorDistance(colorVector, centroidVector); if (dist < minDist) { minDist = dist; bestIdx = i; } } return centroids[bestIdx]; }
        private static float ColorDistance(Vector3 a, Vector3 b) { float dr = (a.x - b.x) * 0.3f; float dg = (a.y - b.y) * 0.59f; float db = (a.z - b.z) * 0.11f; return (dr * dr + dg * dg + db * db); }
        private class ColorComparer : IEqualityComparer<Color32> { public bool Equals(Color32 x, Color32 y) { return x.r == y.r && x.g == y.g && x.b == y.b; } public int GetHashCode(Color32 color) { return (color.r << 16) | (color.g << 8) | color.b; } }
        private class ColorBox { private Color32[] pixels; private Vector3 min, max; public ColorBox(Color32[] pixelArray) { pixels = pixelArray; CalculateBounds(); } private void CalculateBounds() { if (pixels.Length == 0) { min = max = Vector3.zero; return; } min = new Vector3(255, 255, 255); max = new Vector3(0, 0, 0); foreach (var p in pixels) { min.x = Mathf.Min(min.x, p.r); min.y = Mathf.Min(min.y, p.g); min.z = Mathf.Min(min.z, p.b); max.x = Mathf.Max(max.x, p.r); max.y = Mathf.Max(max.y, p.g); max.z = Mathf.Max(max.z, p.b); } } public double Volume() { return (max.x - min.x + 1) * (max.y - min.y + 1) * (max.z - min.z + 1); } public bool CanSplit() { return pixels.Length > 1; } public Tuple<ColorBox, ColorBox> Split() { int dim; float rRange = max.x - min.x, gRange = max.y - min.y, bRange = max.z - min.z; float rPerceptual = rRange * 0.3f, gPerceptual = gRange * 0.59f, bPerceptual = bRange * 0.11f; if (rPerceptual >= gPerceptual && rPerceptual >= bPerceptual) dim = 0; else if (gPerceptual >= rPerceptual && gPerceptual >= bPerceptual) dim = 1; else dim = 2; Array.Sort(pixels, (a, b) => { if (dim == 0) return a.r.CompareTo(b.r); if (dim == 1) return a.g.CompareTo(b.g); return a.b.CompareTo(b.b); }); int median = pixels.Length / 2; var box1Pixels = pixels.Take(median).ToArray(); var box2Pixels = pixels.Skip(median).ToArray(); return new Tuple<ColorBox, ColorBox>(new ColorBox(box1Pixels), new ColorBox(box2Pixels)); } public Color32 AverageColor() { if (pixels.Length == 0) return new Color32(0, 0, 0, 255); long r = 0, g = 0, b = 0; foreach (var p in pixels) { r += p.r; g += p.g; b += p.b; } return new Color32((byte)(r / pixels.Length), (byte)(g / pixels.Length), (byte)(b / pixels.Length), 255); } }
        public static Texture2D EnsureTextureIsReadable(Texture2D texture)
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(texture); if (string.IsNullOrEmpty(path)) return texture;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable) { if (EditorUtility.DisplayDialog("Set Texture Read/Write Enabled?", $"The texture '{texture.name}' is not marked as readable.\nDo you want to enable Read/Write?", "Yes", "No")) { importer.isReadable = true; importer.SaveAndReimport(); } else { return null; } }
#endif
            return texture;
        }
        #endregion

        #region Pixel Detection 

        public static Texture2D PixelDetect(Texture2D source, out float horizontalSpacing, out float verticalSpacing)
        {
            horizontalSpacing = 1f;
            verticalSpacing = 1f;

            if (source == null) return null;

            source = EnsureTextureIsReadable(source);
            if (source == null) return null;

            int w = source.width;
            int h = source.height;

            var pix = source.GetPixels32();
            var gray = new byte[pix.Length];
            for (int i = 0; i < pix.Length; i++)
            {
                gray[i] = (byte)(pix[i].r * 0.299f + pix[i].g * 0.587f + pix[i].b * 0.114f);
            }

            const int TILE_COUNT = 3;
            const float OVERLAP = 0.25f;

            var allScales = new List<float>();

            int tileW = w / TILE_COUNT;
            int tileH = h / TILE_COUNT;

            if (tileW < 50 || tileH < 50)
            {
                Debug.LogWarning("Image is too small for tiled detection, running on whole image.");
                RunDetectionOnRegion(gray, 0, 0, w, h, w, allScales);
            }
            else
            {
                int overlapW = (int)(tileW * OVERLAP);
                int overlapH = (int)(tileH * OVERLAP);

                for (int y = 0; y < TILE_COUNT; y++)
                {
                    for (int x = 0; x < TILE_COUNT; x++)
                    {
                        int roiX = Math.Max(0, x * tileW - overlapW);
                        int roiY = Math.Max(0, y * tileH - overlapH);
                        int roiW = Math.Min(w - roiX, tileW + 2 * overlapW);
                        int roiH = Math.Min(h - roiY, tileH + 2 * overlapH);
                        RunDetectionOnRegion(gray, roiX, roiY, roiW, roiH, w, allScales);
                    }
                }
            }

            float finalScale = 1f;
            if (allScales.Count > 0)
            {
                finalScale = CalculateMode(allScales); 
                
            }
            else
            {
                
                LegacyPixelDetect(pix, w, h, out horizontalSpacing, out verticalSpacing);
                finalScale = Mathf.Max(horizontalSpacing, verticalSpacing, 1f);
            }

            horizontalSpacing = finalScale;
            verticalSpacing = finalScale;

            if (forceUniformSpacing)
                horizontalSpacing = verticalSpacing = finalScale;

            int newW = Mathf.RoundToInt(w / Mathf.Max(1, horizontalSpacing));
            int newH = Mathf.RoundToInt(h / Mathf.Max(1, verticalSpacing));

            return KCentroid(source, newW, newH, 2);
        }

        private static void RunDetectionOnRegion(byte[] gray, int roiX, int roiY, int roiW, int roiH, int sourceWidth, List<float> allScales)
        {
            const double LOW_VARIANCE_THRESHOLD = 5.0; 

            if (roiW < 30 || roiH < 30) return;
            if (GetTileVariance(gray, sourceWidth, roiX, roiY, roiW, roiH) < LOW_VARIANCE_THRESHOLD) return;

            var hProfile = GetSobelProfile(gray, sourceWidth, roiX, roiY, roiW, roiH, true); 
            float hScale = DetectScaleFromProfile(hProfile);  

            var vProfile = GetSobelProfile(gray, sourceWidth, roiX, roiY, roiW, roiH, false); 
            float vScale = DetectScaleFromProfile(vProfile);  

            if (hScale > 1) allScales.Add(hScale);
            if (vScale > 1) allScales.Add(vScale);
        }
 
        private static float DetectScaleFromProfile(double[] signal)
        {
            if (signal.Length < 3) return 1f;
 
            double sum = signal.Sum();
            double mean = sum / signal.Length;
            double stdDev = Math.Sqrt(signal.Select(x => (x - mean) * (x - mean)).Sum() / signal.Length);
            double threshold = mean + 1.5 * stdDev;
 
            var peaks = new List<int>();
            for (int i = 1; i < signal.Length - 1; i++)
            {
                if (signal[i] > threshold && signal[i] > signal[i - 1] && signal[i] > signal[i + 1])
                { 
                    if (peaks.Count == 0 || i - peaks[peaks.Count - 1] > 2)
                    {
                        peaks.Add(i);
                    }
                }
            }

            if (peaks.Count <= 2) return 1f;
 
            var spacings = new List<float>();
            for (int i = 0; i < peaks.Count - 1; i++)
            {
                spacings.Add(peaks[i + 1] - peaks[i]);
            }
 
            float medianSpacing = CalculateMedian(spacings);
            int closeToMedianCount = spacings.Count(s => Math.Abs(s - medianSpacing) <= 2);

            if ((float)closeToMedianCount / spacings.Count > 0.7f)
            {
                return Mathf.Round(medianSpacing);
            }

            return CalculateMode(spacings);
        }

        private static double GetTileVariance(byte[] gray, int sourceWidth, int roiX, int roiY, int roiW, int roiH) { double sum = 0, sumSq = 0; int count = 0; for (int y = roiY; y < roiY + roiH; y++) { for (int x = roiX; x < roiX + roiW; x++) { byte val = gray[y * sourceWidth + x]; sum += val; sumSq += val * val; count++; } } if (count == 0) return 0; double mean = sum / count; return Math.Sqrt((sumSq / count) - (mean * mean)); }


        private static double[] GetSobelProfile(byte[] gray, int sourceWidth, int roiX, int roiY, int roiW, int roiH, bool isHorizontal)
        {
            var profile = new double[isHorizontal ? roiW : roiH];
            int[] kernel = isHorizontal ? new int[] { -1, 0, 1, -2, 0, 2, -1, 0, 1 } : new int[] { -1, -2, -1, 0, 0, 0, 1, 2, 1 };

            for (int y = 1; y < roiH - 1; y++)
            {
                for (int x = 1; x < roiW - 1; x++)
                {
                    int absX = roiX + x;
                    int absY = roiY + y;
                    float grad = 0;
                    grad += kernel[0] * gray[(absY - 1) * sourceWidth + (absX - 1)];
                    grad += kernel[1] * gray[(absY - 1) * sourceWidth + absX];
                    grad += kernel[2] * gray[(absY - 1) * sourceWidth + (absX + 1)];
                    grad += kernel[3] * gray[absY * sourceWidth + (absX - 1)];
                    grad += kernel[4] * gray[absY * sourceWidth + absX];
                    grad += kernel[5] * gray[absY * sourceWidth + (absX + 1)];
                    grad += kernel[6] * gray[(absY + 1) * sourceWidth + (absX - 1)];
                    grad += kernel[7] * gray[(absY + 1) * sourceWidth + absX];
                    grad += kernel[8] * gray[(absY + 1) * sourceWidth + (absX + 1)];

                    if (isHorizontal)
                    {
                        profile[x] += Math.Abs(grad);  
                    }
                    else
                    {
                        profile[y] += Math.Abs(grad); 
                    }
                }
            }
            return profile;
        }

        private static float CalculateMode(List<float> numbers) { if (numbers == null || numbers.Count == 0) return 1f; var roundedNumbers = numbers.Select(n => Mathf.RoundToInt(n)).ToList(); var counts = roundedNumbers.GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count()); if (counts.Count == 0) return 1f; return counts.OrderByDescending(kv => kv.Value).First().Key; }
        private static float CalculateMedian(List<float> numbers) { if (numbers.Count == 0) return 0f; var sorted = numbers.OrderBy(n => n).ToList(); int mid = sorted.Count / 2; return (sorted.Count % 2 != 0) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2f; }
        private static void LegacyPixelDetect(Color32[] pix, int w, int h, out float horizontalSpacing, out float verticalSpacing) { var hsum = new double[w - 1]; for (int y = 0; y < h; y++) { for (int x = 0; x < w - 1; x++) { var a = pix[y * w + x]; var b = pix[y * w + x + 1]; double dr = (a.r - b.r) * 0.3, dg = (a.g - b.g) * 0.59, db = (a.b - b.b) * 0.11; hsum[x] += Math.Sqrt(dr * dr + dg * dg + db * db); } } var vsum = new double[h - 1]; for (int y = 0; y < h - 1; y++) { for (int x = 0; x < w; x++) { var a = pix[y * w + x]; var b = pix[(y + 1) * w + x]; double dr = (a.r - b.r) * 0.3, dg = (a.g - b.g) * 0.59, db = (a.b - b.b) * 0.11; vsum[y] += Math.Sqrt(dr * dr + dg * dg + db * db); } } var hPeaks = AdvancedPeakDetection(hsum); var vPeaks = AdvancedPeakDetection(vsum); horizontalSpacing = CalculateMedianSpacing(hPeaks); verticalSpacing = CalculateMedianSpacing(vPeaks); }
        private static List<int> AdvancedPeakDetection(double[] signal) { if (signal.Length < 3) return new List<int>(); double maxSignal = signal.Max(), minSignal = signal.Min(), range = maxSignal - minSignal; if (range < 1e-10) return new List<int>(); var peakIndices = new List<int>(); for (int i = 1; i < signal.Length - 1; i++) { if (signal[i] > signal[i - 1] && signal[i] > signal[i + 1]) { if ((signal[i] - minSignal) / range >= 0.05) peakIndices.Add(i); } } if (peakIndices.Count == 0) return peakIndices; var prominences = CalculateProminences(signal, peakIndices); var peaksWithProminence = peakIndices.Zip(prominences, (idx, prom) => new { Index = idx, Prominence = prom }).OrderByDescending(p => p.Prominence).ToList(); var filteredPeaks = new List<int>(); var maskedIndices = new HashSet<int>(); foreach (var peak in peaksWithProminence) { if (maskedIndices.Contains(peak.Index)) continue; filteredPeaks.Add(peak.Index); for (int j = peak.Index - 1; j <= peak.Index + 1; j++) { if (j >= 0 && j < signal.Length) maskedIndices.Add(j); } } return filteredPeaks.OrderBy(i => i).ToList(); }
        private static List<double> CalculateProminences(double[] signal, List<int> peakIndices) { var prominences = new List<double>(); foreach (int peakIdx in peakIndices) { double peakHeight = signal[peakIdx], leftContour = double.MaxValue; bool leftHigherPeakFound = false; for (int i = peakIdx - 1; i >= 0; i--) { if (signal[i] > peakHeight) { leftHigherPeakFound = true; break; } leftContour = Math.Min(leftContour, signal[i]); } if (!leftHigherPeakFound && leftContour == double.MaxValue) leftContour = signal.Length > 0 ? signal[0] : 0; double rightContour = double.MaxValue; bool rightHigherPeakFound = false; for (int i = peakIdx + 1; i < signal.Length; i++) { if (signal[i] > peakHeight) { rightHigherPeakFound = true; break; } rightContour = Math.Min(rightContour, signal[i]); } if (!rightHigherPeakFound && rightContour == double.MaxValue) rightContour = signal.Length > 0 ? signal[signal.Length - 1] : 0; prominences.Add(peakHeight - Math.Max(leftContour, rightContour)); } return prominences; }
        private static float CalculateMedianSpacing(List<int> peaks) { if (peaks.Count < 2) return 1f; var spacings = new List<int>(); for (int i = 0; i < peaks.Count - 1; i++) { spacings.Add(peaks[i + 1] - peaks[i]); } if (spacings.Count > 4) { spacings.Sort(); float prelimMedian = spacings[spacings.Count / 2]; spacings = spacings.Where(s => s <= prelimMedian * 3 && s >= prelimMedian / 3).ToList(); } if (spacings.Count == 0) return 1f; spacings.Sort(); int middle = spacings.Count / 2; return spacings.Count % 2 == 1 ? spacings[middle] : (spacings[middle - 1] + spacings[middle]) * 0.5f; }

        #endregion
    }
}