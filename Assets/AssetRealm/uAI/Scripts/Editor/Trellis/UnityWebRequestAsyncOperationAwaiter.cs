using System.Threading.Tasks;
using UnityEngine.Networking;
namespace UAI
{
    public static class UnityWebRequestAsyncOperationAwaiter
    {
        public static System.Runtime.CompilerServices.TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<object>();
            asyncOp.completed += obj => { tcs.SetResult(null); };
            return ((Task)tcs.Task).GetAwaiter();
        }
        public static System.Runtime.CompilerServices.TaskAwaiter<UnityWebRequest.Result> GetAwaiter(this UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<UnityWebRequest.Result>();
            request.SendWebRequest().completed += _ => tcs.SetResult(request.result);
            return tcs.Task.GetAwaiter();
        }

        // Helper to make SendWebRequest awaitable directly
        public static System.Runtime.CompilerServices.TaskAwaiter<UnityWebRequestAsyncOperation> CuandoComplete(this UnityWebRequestAsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<UnityWebRequestAsyncOperation>();
            asyncOp.completed += operation => tcs.SetResult(operation as UnityWebRequestAsyncOperation);
            return tcs.Task.GetAwaiter();
        }
    }
}