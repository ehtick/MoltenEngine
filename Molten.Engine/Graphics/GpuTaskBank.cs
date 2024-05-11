using Molten.Collections;

namespace Molten.Graphics;
internal abstract class GpuTaskBank
{
    internal abstract void Process(GpuCommandList cmd, uint taskIndex);

    internal int BankIndex { get; init; }
}

internal class GpuTaskBank<T> : GpuTaskBank
    where T : struct, IGpuTask<T>
{
    ValueFreeRefList<T> _tasks = new();

    internal GpuTaskBank(int bankIndex)
    {
        BankIndex = bankIndex;
    }

    internal override void Process(GpuCommandList cmd, uint taskIndex)
    {
        ref T task = ref _tasks[taskIndex];
        bool success = T.Process(cmd, ref task);
        task.Complete(success);
        _tasks.RemoveAt(taskIndex);
    }

    internal uint Enqueue(ref T task)
    {
        return _tasks.Add(ref task);
    }
}