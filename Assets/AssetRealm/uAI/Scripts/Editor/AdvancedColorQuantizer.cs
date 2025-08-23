using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace uAI.PixelArtScaler
{ 
    public static class AdvancedColorQuantizer
    {  
        public enum QuantizationStage
        {
            PreDownscaling,
            PostDownscaling,
            Both,
            None
        }

        public enum DominantColorMethod
        {
            Frequency,
            WeightedFrequency,
            PerceptualDistance
        }

        [System.Serializable]
        public class QuantizationSettings
        {
            [Header("Color Count Control")]
            public bool autoDetectColors = true;
            public int maxColors = 16;
            public int minColors = 2;

            [Header("Dominant Color Selection")]
            [Range(0.05f, 0.8f)]
            public float dominanceThreshold = 0.15f;
            public DominantColorMethod dominantMethod = DominantColorMethod.WeightedFrequency;

            [Header("Processing Stage")]
            public QuantizationStage quantizationStage = QuantizationStage.PreDownscaling;

            [Header("Quality vs Speed")]
            [Range(1, 5)]
            public int qualityLevel = 3;

            [Header("Advanced Options")]
            public bool preserveAlphaVariation = true;
        }

        public class QuantizationResult
        {
            public Texture2D quantizedTexture;
            public Color32[] finalPalette;
            public int originalColorCount;
            public int finalColorCount;
            public float compressionRatio;
            public QuantizationMetrics metrics;
        }

        public class QuantizationMetrics
        {
            public float averageError;
            public float maxError;
            public float perceptualError;
            public Dictionary<Color32, int> colorFrequency;
            public float processingTimeMs;
        }  
        
        public static QuantizationResult QuantizeTexture(Texture2D source, QuantizationSettings settings)
        {
            var startTime = Time.realtimeSinceStartup;

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var readableSource = PixelDetectorHelper.EnsureTextureIsReadable(source);
            if (readableSource == null)
                throw new InvalidOperationException("Could not make texture readable");

            var sourcePixels = readableSource.GetPixels32();
            var originalColorCount = CountUniqueColors(sourcePixels);


            int targetColors = settings.autoDetectColors
                ? DetectOptimalColorCount(sourcePixels, settings)
                : settings.maxColors;

            if (originalColorCount <= targetColors)
            {
                 return new QuantizationResult
                {
                    quantizedTexture = source,
                    finalPalette = ExtractPalette(sourcePixels),
                    originalColorCount = originalColorCount,
                    finalColorCount = originalColorCount,
                    compressionRatio = 1.0f,
                    metrics = new QuantizationMetrics { processingTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f }
                };
            }

            var quantizedPixels = PerformImprovedQuantization(sourcePixels, targetColors, settings);
            var finalPalette = ExtractPalette(quantizedPixels);

            var resultTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            resultTexture.SetPixels32(quantizedPixels);
            resultTexture.Apply();
            resultTexture.filterMode = FilterMode.Point;

            var metrics = CalculateMetrics(sourcePixels, quantizedPixels, startTime);

            return new QuantizationResult
            {
                quantizedTexture = resultTexture,
                finalPalette = finalPalette,
                originalColorCount = originalColorCount,
                finalColorCount = finalPalette.Length,
                compressionRatio = (float)originalColorCount / finalPalette.Length,
                metrics = metrics
            };
        }
 
        private static Color32[] PerformImprovedQuantization(Color32[] sourcePixels, int targetColors, QuantizationSettings settings)
        { 
            var initialPalette = ImprovedMedianCut(sourcePixels, targetColors); 
            var refinedPalette = RefinePalette(sourcePixels, initialPalette, settings); 
            return MapPixelsToClosestColors(sourcePixels, refinedPalette);
        } 
        private static Color32[] RefinePalette(Color32[] pixels, Color32[] initialPalette, QuantizationSettings settings)
        {
            if (settings.qualityLevel < 3 || initialPalette.Length <= 1) return initialPalette;

            const int maxIterations = 5;
            const float convergenceThreshold = 1.0f;

            var currentPalette = initialPalette.ToArray();
            var paletteLab = currentPalette.Select(CIELABConverter.ToCIELAB).ToArray();

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var clusterSums = new Vector3[currentPalette.Length];
                var clusterSizes = new int[currentPalette.Length];

                for (int i = 0; i < pixels.Length; i++)
                {
                    int nearestIdx = FindNearestColorIndex(pixels[i], paletteLab);
                    clusterSums[nearestIdx] += new Vector3(pixels[i].r, pixels[i].g, pixels[i].b);
                    clusterSizes[nearestIdx]++;
                }

                float totalMovement = 0f;
                var newPaletteLab = new CIELABConverter.CIELAB[currentPalette.Length];

                for (int i = 0; i < currentPalette.Length; i++)
                {
                    if (clusterSizes[i] > 0)
                    {
                        var avgColorVec = clusterSums[i] / clusterSizes[i];
                        var newColor = new Color32((byte)avgColorVec.x, (byte)avgColorVec.y, (byte)avgColorVec.z, currentPalette[i].a);
                        
                        currentPalette[i] = newColor;
                        newPaletteLab[i] = CIELABConverter.ToCIELAB(newColor);

                        totalMovement += CIELABConverter.ColorDistance(paletteLab[i], newPaletteLab[i]);
                    }
                    else
                    {
                        newPaletteLab[i] = paletteLab[i];
                    }
                }
                
                paletteLab = newPaletteLab;

                if (totalMovement < convergenceThreshold)
                    break;
            }

            return currentPalette;
        }
 
        private static Color32[] MapPixelsToClosestColors(Color32[] sourcePixels, Color32[] palette)
        {
            if (palette.Length == 0) return sourcePixels;

            var result = new Color32[sourcePixels.Length];
            var paletteLab = palette.Select(CIELABConverter.ToCIELAB).ToArray();

            var labCache = new Dictionary<Color32, CIELABConverter.CIELAB>(new ColorComparer());

            for (int i = 0; i < sourcePixels.Length; i++)
            {
                int nearestIdx = FindNearestColorIndex(sourcePixels[i], paletteLab, labCache);
                result[i] = palette[nearestIdx];
            }
            return result;
        }
         
        private static int FindNearestColorIndex(Color32 pixel, CIELABConverter.CIELAB[] paletteLab, Dictionary<Color32, CIELABConverter.CIELAB> cache = null)
        {
            CIELABConverter.CIELAB pixelLab;
            if (cache != null && cache.ContainsKey(pixel))
            {
                pixelLab = cache[pixel];
            }
            else
            {
                pixelLab = CIELABConverter.ToCIELAB(pixel);
                cache?.Add(pixel, pixelLab);
            }
            
            float bestDistanceSq = float.MaxValue;
            int bestIndex = 0;

            for (int i = 0; i < paletteLab.Length; i++)
            {
                float distanceSq = CIELABConverter.ColorDistance(pixelLab, paletteLab[i]);
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }
        
        #region Median Cut Quantization
         
        private static Color32[] ImprovedMedianCut(Color32[] pixels, int k)
        {
            var cubes = new List<ColorBox> { new ColorBox(pixels) };
            while (cubes.Count < k)
            {
                int largestIdx = -1;
                double largestVolume = -1;

                
                for (int i = 0; i < cubes.Count; i++)
                {
                    if (cubes[i].CanSplit())
                    {
                        double volume = cubes[i].Volume();
                        if (volume > largestVolume)
                        {
                            largestVolume = volume;
                            largestIdx = i;
                        }
                    }
                }

                if (largestIdx == -1) break; 

                var boxToSplit = cubes[largestIdx];
                cubes.RemoveAt(largestIdx);
                var (box1, box2) = boxToSplit.Split(); 
                cubes.Add(box1);
                cubes.Add(box2);
            }
            
            return cubes.Select(c => c.AverageColor()).ToArray();
        }
        
        private class ColorBox
        {
            private readonly Color32[] _pixels;
            private readonly Vector3 _min, _max;

            public ColorBox(Color32[] pixelArray)
            {
                _pixels = pixelArray;
                CalculateBounds(out _min, out _max);
            }

            private void CalculateBounds(out Vector3 min, out Vector3 max)
            {
                if (_pixels.Length == 0)
                {
                    min = max = Vector3.zero;
                    return;
                }
                min = new Vector3(255, 255, 255);
                max = new Vector3(0, 0, 0);
                foreach (var p in _pixels)
                {
                    min.x = Mathf.Min(min.x, p.r);
                    min.y = Mathf.Min(min.y, p.g);
                    min.z = Mathf.Min(min.z, p.b);
                    max.x = Mathf.Max(max.x, p.r);
                    max.y = Mathf.Max(max.y, p.g);
                    max.z = Mathf.Max(max.z, p.b);
                }
            }

            public double Volume() => (_max.x - _min.x + 1) * (_max.y - _min.y + 1) * (_max.z - _min.z + 1);
            public bool CanSplit() => _pixels.Length > 1;

            public (ColorBox, ColorBox) Split()
            {
                
                float rRange = _max.x - _min.x;
                float gRange = _max.y - _min.y;
                float bRange = _max.z - _min.z;
                
                int dim;
                if (rRange >= gRange && rRange >= bRange) dim = 0; 
                else if (gRange >= rRange && gRange >= bRange) dim = 1; 
                else dim = 2;  

                var pixelsToSort = _pixels.ToArray();

                
                Array.Sort(pixelsToSort, (a, b) =>
                {
                    if (dim == 0) return a.r.CompareTo(b.r);
                    if (dim == 1) return a.g.CompareTo(b.g);
                    return a.b.CompareTo(b.b);
                });

                int median = pixelsToSort.Length / 2;

                
                var box1Pixels = pixelsToSort.Take(median).ToArray();
                var box2Pixels = pixelsToSort.Skip(median).ToArray();

                return (new ColorBox(box1Pixels), new ColorBox(box2Pixels));
            }

            public Color32 AverageColor()
            {
                if (_pixels.Length == 0) return new Color32(0, 0, 0, 255);
                long r = 0, g = 0, b = 0;
                foreach (var p in _pixels) { r += p.r; g += p.g; b += p.b; }
                return new Color32((byte)(r / _pixels.Length), (byte)(g / _pixels.Length), (byte)(b / _pixels.Length), 255);
            }
        }
        #endregion

        #region Helper Methods
        private static int DetectOptimalColorCount(Color32[] pixels, QuantizationSettings settings)
        {
            var uniqueColors = CountUniqueColors(pixels);
            if (uniqueColors <= settings.minColors) return settings.minColors;
            if (uniqueColors <= settings.maxColors) return uniqueColors;

            var colorFreq = GetColorFrequency(pixels);
            var sortedColors = colorFreq.OrderByDescending(kv => kv.Value).ToArray();
            
            float totalPixels = pixels.Length;
            float cumulativeFreq = 0f;
            int optimalCount = settings.maxColors;

            for (int i = 0; i < Math.Min(sortedColors.Length, settings.maxColors); i++)
            {
                cumulativeFreq += sortedColors[i].Value;
                if ((cumulativeFreq / totalPixels) >= 0.95f)
                {
                    optimalCount = Math.Max(settings.minColors, i + 1);
                    break;
                }
            }
            
            int minRecommended = Mathf.RoundToInt(settings.maxColors * 0.75f);
            optimalCount = Math.Max(optimalCount, minRecommended);
            return Mathf.Clamp(optimalCount, settings.minColors, settings.maxColors);
        }

        private static int CountUniqueColors(Color32[] pixels)
        {
            return new HashSet<Color32>(pixels, new ColorComparer()).Count;
        }

        private static Dictionary<Color32, int> GetColorFrequency(Color32[] pixels)
        {
            var frequency = new Dictionary<Color32, int>(new ColorComparer());
            foreach (var pixel in pixels)
            {
                if (frequency.TryGetValue(pixel, out int count))
                {
                    frequency[pixel] = count + 1;
                }
                else
                {
                    frequency[pixel] = 1;
                }
            }
            return frequency;
        }

        private static Color32[] ExtractPalette(Color32[] pixels)
        {
            return new HashSet<Color32>(pixels, new ColorComparer()).ToArray();
        }

        private static QuantizationMetrics CalculateMetrics(Color32[] original, Color32[] quantized, float startTime)
        {
            float totalPerceptualError = 0f;
            float maxPerceptualError = 0f;
            
            for (int i = 0; i < original.Length; i++)
            {
                float pError = CIELABConverter.ColorDistance(CIELABConverter.ToCIELAB(original[i]), CIELABConverter.ToCIELAB(quantized[i]));
                totalPerceptualError += pError;
                if (pError > maxPerceptualError) maxPerceptualError = pError;
            }
            
            return new QuantizationMetrics
            {
                averageError = totalPerceptualError / original.Length,
                maxError = maxPerceptualError,
                perceptualError = totalPerceptualError / original.Length,
                colorFrequency = GetColorFrequency(quantized),
                processingTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f
            };
        }
        
        private class ColorComparer : IEqualityComparer<Color32>
        {
            public bool Equals(Color32 x, Color32 y) => x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
            public int GetHashCode(Color32 c) => (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
        }
        #endregion

        #region CIELABConverter
        
        
        
        public static class CIELABConverter
        {
            public struct CIELAB
            {
                public float L, A, B;
            }

            public static CIELAB ToCIELAB(Color32 c)
            {
                float rLinear = c.r / 255f;
                float gLinear = c.g / 255f;
                float bLinear = c.b / 255f;

                rLinear = (rLinear > 0.04045f) ? Mathf.Pow((rLinear + 0.055f) / 1.055f, 2.4f) : rLinear / 12.92f;
                gLinear = (gLinear > 0.04045f) ? Mathf.Pow((gLinear + 0.055f) / 1.055f, 2.4f) : gLinear / 12.92f;
                bLinear = (bLinear > 0.04045f) ? Mathf.Pow((bLinear + 0.055f) / 1.055f, 2.4f) : bLinear / 12.92f;

                float x = rLinear * 0.4124564f + gLinear * 0.3575761f + bLinear * 0.1804375f;
                float y = rLinear * 0.2126729f + gLinear * 0.7151522f + bLinear * 0.0721750f;
                float z = rLinear * 0.0193339f + gLinear * 0.1191920f + bLinear * 0.9503041f;
                
                const float refX = 0.95047f;
                const float refY = 1.00000f;
                const float refZ = 1.08883f;

                x /= refX;
                y /= refY;
                z /= refZ;

                x = (x > 0.008856f) ? Mathf.Pow(x, 1f/3f) : (7.787f * x) + (16f/116f);
                y = (y > 0.008856f) ? Mathf.Pow(y, 1f/3f) : (7.787f * y) + (16f/116f);
                z = (z > 0.008856f) ? Mathf.Pow(z, 1f/3f) : (7.787f * z) + (16f/116f);

                return new CIELAB
                {
                    L = (116f * y) - 16f,
                    A = 500f * (x - y),
                    B = 200f * (y - z)
                };
            }
            
            public static float ColorDistance(CIELAB a, CIELAB b)
            {
                float deltaL = a.L - b.L;
                float deltaA = a.A - b.A;
                float deltaB = a.B - b.B;
                return deltaL * deltaL + deltaA * deltaA + deltaB * deltaB;
            }
        }
        #endregion
    }
}