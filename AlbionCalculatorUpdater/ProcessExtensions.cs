using System.Diagnostics;
using System.Threading.Tasks;

namespace AlbionCalculatorUpdater;

public static class ProcessExtensions
{
    public static Task WaitForExitAsync(this Process process)
    {
        // 이미 종료된 경우 즉시 완료 반환
        if (process.HasExited) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<object>();
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(null);

        // 이벤트 등록 직후 프로세스가 종료되었을 경우를 대비한 방어 코드
        if (process.HasExited) tcs.TrySetResult(null);

        return tcs.Task;
    }
}