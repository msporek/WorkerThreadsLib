using System;

namespace WorkerThreadsLib;

/// <summary>
/// Base class for all operations to be scheduled for execution on instances of <see cref="WorkerThread"/> class.
/// </summary>
public abstract class WorkerOperation
{
    /// <summary>
    /// Public parameterless constructor. 
    /// </summary>
    /// 
    /// <remarks>It is is only possible to call it from derived classes, because <see cref="WorkerThread"/> class is abstract.</remarks>
    public WorkerOperation()
    {
    }

    /// <summary>
    /// Operation to be executed. 
    /// </summary>
    public abstract void Run();

    /// <summary>
    /// Should carry results of operation execution in the case of an error - exception that occured making the operation fail. 
    /// </summary>
    public abstract Exception OperationException { get; set; }

    /// <summary>
    /// Gets or sets value indicating a unique operation key. 
    /// </summary>
    public abstract string OperationKey { get; }
}
