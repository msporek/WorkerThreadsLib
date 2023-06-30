using System;
using System.Collections.Generic;
using System.Threading;

namespace WorkerThreadsLib;

/// <summary>
/// Basic class used for scheduling operations (represented by <see cref="WorkerOperation"/>) on a thread 
/// that executes these operations one by one. If no scheduled operations to run, the thread waits for another operation. 
/// Once scheduled, the thread gets activated and handles the operation immediately. If multiple operations are scheduled, 
/// then they get queued and will be handled one by one. 
/// </summary>
public class WorkerThread
{
    /// <summary>
    /// Represents the default value for the maximum length of the queue with scheduled operations. 
    /// </summary>
    public const int DefaultMaxQueueSize = 10000;

    #region Public properties and methods

    /// <summary>
    /// Gets name of this <see cref="WorkerThread"/> instance. 
    /// </summary>
    public string ThreadName => this._thread.Name;

    /// <summary>
    /// Gets value indicating the max number of queue lenght that this thread supports. 
    /// </summary>
    public int MaxQueueSize => this._queueMaxSize;

    /// <summary>
    /// Gets the current length of queue of scheduled operations. 
    /// </summary>
    public int CurrentQueueSize
    {
        get
        {
            lock (this._queueOfOperations)
            {
                return this._queueOfOperations.Count;
            }
        }
    }

    /// <summary>
    /// Gets value indicating whether the thread is active (i.e. true if the thread was started and was not stopped, otherwise false). 
    /// </summary>
    public bool IsActive => this._isActive;

    /// <summary>
    /// Gets value indicating if the thread is currently running any operation. 
    /// </summary>
    public bool IsBusy => this._isBusy;

    /// <summary>
    /// Method attempts to enqueue the operation by adding it to the queue of scheduled operations. It succeeds if the current length of the 
    /// queue of operations is less than <see cref="WorkerThread.MaxQueueSize"/>. 
    /// </summary>
    /// 
    /// <param name="request">Operation to be scheduled for execution.</param>
    /// 
    /// <returns>True if the <paramref name="request"/> operation was scheduled successfully. Otherwise false. </returns>
    /// 
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
    public virtual bool TryEnqueue(WorkerOperation request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (this._queueOfOperations)
        {
            if (this._queueOfOperations.Count < this._queueMaxSize)
            {
                this._queueOfOperations.AddLast(request);
                
                this._actionEnqueued.Set();

                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Method starts the thread and makes it active. 
    /// </summary>
    public void Start()
    {
        this._thread.Start();
    }

    /// <summary>
    /// Method stops the thread if it is running. 
    /// </summary>
    public void Stop()
    {
        this._threadStopped.Set();
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor creates a new instance of <see cref="WorkerThread"/> class initializing its properties with the provided arguments. 
    /// </summary>
    /// 
    /// <param name="threadName">Name to assign to the thread.</param>
    /// <param name="queueMaxSize">Maximum number of operations that can be scheduled on the thread queue.</param>
    /// 
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="threadName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="threadName"/> is empty string, or if <paramref name="queueMaxSize"/> 
    /// equals zero or is less than zero.</exception>
    public WorkerThread(string threadName, int queueMaxSize = DefaultMaxQueueSize)
    {
        ArgumentException.ThrowIfNullOrEmpty(threadName, nameof(threadName));

        if (queueMaxSize <= 0)
        {
            throw new ArgumentException("\"queueMaxSize\" is supposed to be a positive value.");
        }

        this._queueMaxSize = queueMaxSize;

        this._thread = new Thread(this.Run) { Name = threadName };
    }

    /// <summary>
    /// Event fired when a particular operation has been completed. It carries details of the operation with it (represented as 
    /// an instance of the <see cref="WorkerOperation"/> class). 
    /// </summary>
    public event EventHandler<GenericEventArgs<WorkerOperation>> OperationCompleted;

    /// <summary>
    /// Method raises <see cref="WorkerThread.OperationCompleted"/> event for the provided <paramref name="workerOperation"/>. 
    /// </summary>
    /// 
    /// <param name="workerOperation">Operation that has been completed.</param>
    /// 
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workerOperation"/> is null.</exception>
    protected virtual void OnOperationCompleted(WorkerOperation workerOperation)
    {
        ArgumentNullException.ThrowIfNull(workerOperation);

        EventHandler<GenericEventArgs<WorkerOperation>> handler = this.OperationCompleted;
        if (handler != null)
        {
            handler(this, new GenericEventArgs<WorkerOperation>(workerOperation));
        }
    }

    #endregion

    #region Protected and private methods

    protected virtual WorkerOperation TakeNextRequestFromQueue()
    {
        WorkerOperation request = null;
        lock (this._queueOfOperations)
        {
            // If the queue is not empty - try to get its first item and remove it from the queue
            if (this._queueOfOperations.Count > 0)
            {
                request = this._queueOfOperations.First.Value;
                this._queueOfOperations.RemoveFirst();
            }

            // If the queue is empty after removing first item - we need to reset methodEnqueued_
            if (this._queueOfOperations.Count == 0)
            {
                this._actionEnqueued.Reset();
            }
        }

        return request;
    }

    /// <summary>
    /// Main method of the thread. 
    /// </summary>
    protected virtual void Run()
    {
        try
        {
            this._isActive = true;

            WaitHandle[] eventHandles = new WaitHandle[] { this._threadStopped, this._actionEnqueued };
            while (this._isActive)
            {
                int index = WaitHandle.WaitAny(eventHandles);
                switch (index)
                {
                    case 0:
                        {
                            // Thread was stopped, change IsActive to false and terminate it
                            this._isActive = false;
                            break;
                        }
                    case 1:
                        {
                            // There is a new method to be executed - taking it from the queue
                            WorkerOperation nextRequest = this.TakeNextRequestFromQueue();

                            // If method was successfully got from the queue - invoking it
                            if (nextRequest != null)
                            {
                                this._isBusy = true;

                                this.RunOperation(nextRequest);
                                this.OnOperationCompleted(nextRequest);

                                this._isBusy = false;
                            }

                            break;
                        }
                }
            }
        }
        catch (Exception)
        {
            // TODO: Handle exceptions gracefully. 
        }
        finally
        {
            this._isBusy = false;
        }
    }

    protected virtual void RunOperation(WorkerOperation operation)
    {
        try
        {
            operation.Run();
        }
        catch (Exception ex)
        {
            operation.OperationException = ex;
        }
    }

    #endregion

    #region Private fields

    // Max queue size. 
    protected int _queueMaxSize = 1;

    // Thread object. 
    protected Thread _thread = null;

    // Is this thread active and busy. 
    protected bool _isActive = false;
    protected bool _isBusy = false;

    // Thread synchronization object that indicates when a new action has been queued to wake up the thread and have it run it. 
    protected ManualResetEvent _actionEnqueued = new ManualResetEvent(false);

    // Thread synchronization object that indicates when the thread stop has been requested. 
    protected ManualResetEvent _threadStopped = new ManualResetEvent(false);

    // Queue of methods to be invoked. 
    protected LinkedList<WorkerOperation> _queueOfOperations = new LinkedList<WorkerOperation>();

    #endregion
}