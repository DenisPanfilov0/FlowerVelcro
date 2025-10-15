using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Features.CanvasOfFantasy
{
    public class ToolsPanelController : MonoBehaviour
    {
        [SerializeField] private CanvasOfFantasyWindow _canvasOfFantasyWindow;
        [SerializeField] private CanvasOfFantasyToolsRenderTexture _canvasOfFantasyToolsRenderTexture;

        [SerializeField] private Button _showPanel;
        [SerializeField] private List<GameObject> _showObjects;
        
        [SerializeField] private Button _hidePanel;
        [SerializeField] private List<GameObject> _hideObjects;

        private void Start()
        {
            _showPanel.onClick.AddListener(ShowPanel);
            _hidePanel.onClick.AddListener(HidePanel);
        }

        private void OnDestroy()
        {
            _showPanel.onClick.RemoveListener(ShowPanel);
            _hidePanel.onClick.RemoveListener(HidePanel);
        }

        private void ShowPanel()
        {
            foreach (var obj in _showObjects)
            {
                obj.SetActive(true);
            }
            
            foreach (var obj in _hideObjects)
            {
                obj.SetActive(false);
            }
        }
        
        private void HidePanel()
        {
            foreach (var obj in _hideObjects)
            {
                obj.SetActive(true);
            }

            foreach (var obj in _showObjects)
            {
                obj.SetActive(false);
            }
        }
    }
}