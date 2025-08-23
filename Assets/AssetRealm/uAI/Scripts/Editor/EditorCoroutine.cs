using UnityEditor;

namespace uAI
{
    // Helper class for editor coroutines
    public class EditorCoroutine
    {
        public static EditorCoroutine StartCoroutine(System.Collections.IEnumerator routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(routine);
            coroutine.Start();
            return coroutine;
        }

        private readonly System.Collections.IEnumerator _routine;
        
        EditorCoroutine(System.Collections.IEnumerator routine)
        {
            _routine = routine;
        }

        void Start()
        {
            EditorApplication.update += Update;
        }
        
        public void Stop()
        {
            EditorApplication.update -= Update;
        }

        void Update()
        {
            if (!_routine.MoveNext())
            {
                Stop();
            }
        }
    }
}