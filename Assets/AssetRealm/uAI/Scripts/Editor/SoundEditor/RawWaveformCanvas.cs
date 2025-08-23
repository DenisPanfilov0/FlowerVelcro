using UnityEngine;
using UnityEngine.UIElements;

namespace UAI
{
    public class RawWaveformCanvas : VisualElement
    {
        public float[] WaveformData { get; set; }
        public float VisibleTimeStart { get; set; } = 0f; 
        public float VisibleTimeEnd { get; set; } = 1f;
        public float SelectionStart { get; set; } = 0f; 
        public float SelectionEnd { get; set; } = 0f;  
        public bool HasSelection { get; set; } = false;
        public float CursorPosition { get; set; } = 0f; 
        public float AmplitudeScale { get; set; } = 1f;

        private readonly Color _waveformColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        private readonly Color _selectionColor = new Color(1f, 1f, 0f, 0.3f);
        private readonly Color _cursorColor = new Color(1f, 0f, 0f, 0.8f);
        private readonly Color _zeroLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);


        private const float VerticalPaddingFactor = 0.8f; 

        public RawWaveformCanvas()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(evt => MarkDirtyRepaint());
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (WaveformData == null || WaveformData.Length == 0) return;

            var painter = mgc.painter2D;
            
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;

            if (width <= 0 || height <= 0) return;

            float centerY = height / 2f;

            int totalWaveformSamples = WaveformData.Length;
            
            int viewStartIndex = Mathf.FloorToInt(VisibleTimeStart * totalWaveformSamples);
            int viewEndIndex = Mathf.CeilToInt(VisibleTimeEnd * totalWaveformSamples);
            
            viewStartIndex = Mathf.Clamp(viewStartIndex, 0, totalWaveformSamples);
            viewEndIndex = Mathf.Clamp(viewEndIndex, 0, totalWaveformSamples);

            if (viewStartIndex >= viewEndIndex)
            {
                DrawZeroLine(painter, width, centerY);
                DrawSelection(painter, width, height);
                DrawCursor(painter, width, height);
                return;
            }

            int numSamplesInView = viewEndIndex - viewStartIndex; 

            if (numSamplesInView > 0)
            { 
                painter.strokeColor = _waveformColor;
                painter.lineWidth = 1.5f; 
                painter.BeginPath();

                if (numSamplesInView == 1)
                {
                    float rawSample = WaveformData[viewStartIndex];
                    float amplifiedSample = rawSample * AmplitudeScale;
                    float clampedSample = Mathf.Clamp(amplifiedSample, -1f, 1f);
                    
                    float yValueOffset = clampedSample * centerY * VerticalPaddingFactor;
                    float yPos = centerY - yValueOffset;
                    yPos = Mathf.Clamp(yPos, 0, height); 

                    painter.MoveTo(new Vector2(0, yPos));
                    painter.LineTo(new Vector2(width, yPos));
                }
                else 
                {
                    for (int i = 0; i < numSamplesInView; i++)
                    {
                        float rawSample = WaveformData[viewStartIndex + i];
                        float amplifiedSample = rawSample * AmplitudeScale;
                        float clampedSample = Mathf.Clamp(amplifiedSample, -1f, 1f);

                        float yValueOffset = clampedSample * centerY * VerticalPaddingFactor;
                        float yPos = centerY - yValueOffset;
                        yPos = Mathf.Clamp(yPos, 0, height);

                        float xPos = ((float)i / (numSamplesInView - 1)) * width;
                        
                        if (i == 0)
                        {
                            painter.MoveTo(new Vector2(xPos, yPos));
                        }
                        else
                        {
                            painter.LineTo(new Vector2(xPos, yPos));
                        }
                    }
                }
                painter.Stroke();  
            }

            DrawZeroLine(painter, width, centerY);
            DrawSelection(painter, width, height);
            DrawCursor(painter, width, height);
        }

        private void DrawZeroLine(Painter2D painter, float width, float centerY)
        {
            painter.strokeColor = _zeroLineColor;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, centerY));
            painter.LineTo(new Vector2(width, centerY));
            painter.Stroke();
        }

        private void DrawSelection(Painter2D painter, float width, float height)
        {
            if (!HasSelection) return;

            float visNormStart = VisibleTimeStart; 
            float visNormEnd = VisibleTimeEnd;     
            float visNormDuration = visNormEnd - visNormStart;

            if (visNormDuration <= 0) return;

            float displaySelNormStart = Mathf.Min(SelectionStart, SelectionEnd);
            float displaySelNormEnd = Mathf.Max(SelectionStart, SelectionEnd);

            float selStartXOnScreenNorm = (displaySelNormStart - visNormStart) / visNormDuration;
            float selEndXOnScreenNorm = (displaySelNormEnd - visNormStart) / visNormDuration;

            float selX1 = Mathf.Clamp01(selStartXOnScreenNorm) * width;
            float selX2 = Mathf.Clamp01(selEndXOnScreenNorm) * width;
            
            if (selStartXOnScreenNorm < 0 && selEndXOnScreenNorm < 0 || selStartXOnScreenNorm > 1 && selEndXOnScreenNorm > 1) {
                 // Entirely outside view
            } else {
                 selX1 = Mathf.Max(0, selStartXOnScreenNorm * width);
                 selX2 = Mathf.Min(width, selEndXOnScreenNorm * width);
            }


            if (selX2 > selX1) 
            {
                painter.fillColor = _selectionColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(selX1, 0));
                painter.LineTo(new Vector2(selX2, 0));
                painter.LineTo(new Vector2(selX2, height));
                painter.LineTo(new Vector2(selX1, height));
                painter.ClosePath();
                painter.Fill();
            }
        }

        private void DrawCursor(Painter2D painter, float width, float height)
        {
            float visNormStart = VisibleTimeStart;
            float visNormEnd = VisibleTimeEnd;
            float visNormDuration = visNormEnd - visNormStart;

            if (visNormDuration <= 0) return;

            if (CursorPosition >= visNormStart && CursorPosition <= visNormEnd)
            {
                float cursorPosInViewNorm = (CursorPosition - visNormStart) / visNormDuration;
                float cursorX = cursorPosInViewNorm * width;
                cursorX = Mathf.Clamp(cursorX, 0, width); 

                painter.strokeColor = _cursorColor;
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(cursorX, 0));
                painter.LineTo(new Vector2(cursorX, height));
                painter.Stroke();
            }
        }
    }
}