using System;

namespace WorkerThreadsLib;

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
