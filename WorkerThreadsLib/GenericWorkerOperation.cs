using System;

namespace WorkerThreadsLib;

/// <summary>
/// Generic worker operation to be used for scheduling particular types action on the <see cref="WorkerThread"/> instance. 
/// </summary>
public class GenericWorkerOperation : WorkerOperation
{
    protected Action actionToRun;

    /// <summary>
    /// Constructor creates a new instance of <see cref="GenericWorkerOperation"/> class with the <paramref name="actionToRun"/> provided 
    /// to it as an argument. 
    /// </summary>
    /// 
    /// <param name="actionToRun">Action to be run.</param>
    /// 
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actionToRun"/> is null.</exception>
    public GenericWorkerOperation(Action actionToRun)
        : base()
    {
        ArgumentNullException.ThrowIfNull(actionToRun);

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

    public override Exception OperationException { get; set; }
}
