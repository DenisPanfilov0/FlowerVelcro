using System;
using Code.Progress.Data;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Tutorial
{
    public class Merge2Tutorial : MonoBehaviour
    {
        [SerializeField] private Button _endTutorial;
        
        private ProgressData _progressData;

        [Inject]
        public void Construct(ProgressData progressData)
        {
            _progressData = progressData;
        }

        private void Start()
        {
            if (_progressData.IsMerge2TutorialChecked)
            {
                Destroy(gameObject);
            }
            else
            {
                _endTutorial.onClick.AddListener(TutorialEnded);
            }
        }

        private void OnDestroy()
        {
            _endTutorial.onClick.RemoveListener(TutorialEnded);
        }

        private void TutorialEnded()
        {
            _progressData.SetMerge2TutorialChecked();
            Destroy(gameObject);
        }
    }
}