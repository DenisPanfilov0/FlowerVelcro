using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Infrastructure.Loading
{
    public class SceneLoader : ISceneLoader
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly SceneLoaderUI _sceneLoaderUI;

        public SceneLoader(ICoroutineRunner coroutineRunner, SceneLoaderUI sceneLoaderUI)
        {
            _coroutineRunner = coroutineRunner;
            _sceneLoaderUI = sceneLoaderUI;
        }

        public void LoadScene(string name, Action onLoaded = null)
        {
            _coroutineRunner.StartCoroutine(Load(name, onLoaded));
        }

        public void RestartScene(Action enterRestartLevelState)
        {
            _coroutineRunner.StartCoroutine(Load(SceneManager.GetActiveScene().name, enterRestartLevelState));
        }

        private IEnumerator Load(string nextScene, Action onLoaded)
        {
            // if (SceneManager.GetActiveScene().name == nextScene)
            // {
                // onLoaded?.Invoke();
                // yield break;
            // }

            // Start loading animation (fill with black)
            bool loadingComplete = false;
            _sceneLoaderUI.StartLoadingAnimation(() => loadingComplete = true);
            while (!loadingComplete)
            {
                yield return null;
            }

            // Load scene asynchronously
            AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(nextScene);
            while (!waitNextScene.isDone)
            {
                yield return null;
            }

            // Start unloading animation (remove black)
            bool unloadingComplete = false;
            _sceneLoaderUI.StartUnloadingAnimation(() => unloadingComplete = true);
            while (!unloadingComplete)
            {
                yield return null;
            }

            onLoaded?.Invoke();
        }
    }
}