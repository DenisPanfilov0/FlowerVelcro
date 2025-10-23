using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Code.Tutorial
{
    public class ScrollViewDisabling : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;

        private void Start()
        {
            _scrollRect.horizontal = false;
        }
    }
}