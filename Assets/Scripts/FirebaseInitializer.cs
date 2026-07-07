#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using System.Threading.Tasks;

/// <summary>
/// Firebase Unity SDK chỉ cho phép gọi CheckAndFixDependenciesAsync một lần tại một thời điểm.
/// Dùng class này thay vì gọi trực tiếp từ nhiều MonoBehaviour.
/// </summary>
public static class FirebaseInitializer
{
    private static Task<DependencyStatus> initTask;

    public static Task<DependencyStatus> EnsureInitializedAsync()
    {
        if (initTask == null)
            initTask = FirebaseApp.CheckAndFixDependenciesAsync();

        return initTask;
    }

    public static bool IsAvailable
    {
        get
        {
            if (initTask == null || !initTask.IsCompleted || initTask.IsFaulted || initTask.IsCanceled)
                return false;
            return initTask.Result == DependencyStatus.Available;
        }
    }
}
#endif
