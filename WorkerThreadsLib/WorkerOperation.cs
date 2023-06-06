using System;

namespace WorkerThreadsLib;

public abstract class WorkerOperation
{
    public WorkerOperation()
    {
    }

    public abstract void Run();

    public abstract Exception OperationException { get; set; }

    public abstract string OperationKey { get; }
}
