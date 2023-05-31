using System;

namespace WorkerThreadsLib;

/// <summary>
/// Generic worker operation to be used for scheduling particular types action on the <see cref="WorkerThread"/> instance. 
/// </summary>
public class GenericWorkerOperation : WorkerOperation
{
    protected Action actionToRun;

    public GenericWorkerOperation(Action actionToRun)
        : base()
    {
        OperationKey = Guid.NewGuid().ToString("D");
        this.actionToRun = actionToRun;
    }

    public override void Run()
    {
        if (actionToRun != null)
        {
            actionToRun();
        }
    }

    public override string OperationKey { get; }
}
