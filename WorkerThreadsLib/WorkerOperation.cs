namespace WorkerThreadsLib;

public abstract class WorkerOperation
{
    public WorkerOperation()
    {
    }

    public abstract void Run();

    public abstract string OperationKey { get; }
}
