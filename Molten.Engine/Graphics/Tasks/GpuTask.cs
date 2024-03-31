namespace Molten.Graphics;

public delegate void GpuTaskHandler(bool success);

public delegate void GpuTaskHandler<T>(ref readonly T task, bool success)
    where T : struct, IGpuTask<T>;

public interface IGpuTask<T>
    where T : struct, IGpuTask<T>
{
    /// <summary>
    /// Invoked when the task is completed.
    /// </summary>
    /// <param name="success"></param>
    void Complete(bool success);

    /// <summary>
    /// Invoked when a task should be processed immediately instead of being queued.
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="t">The task to be immediately completed</param>
    static abstract bool Process(GpuCommandList cmd, ref T t);
}
