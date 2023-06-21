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
    public const int DefaultMaxQueueSize = 10000;

    #region Public properties and methods

    public string ThreadName => this._thread.Name;

    public int MaxQueueSize => this._queueMaxSize;

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

    public bool IsActive => this._isActive;

    public bool IsBusy => this._isBusy;

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

    public void Start()
    {
        this._thread.Start();
    }

    public void Stop()
    {
        this._threadStopped.Set();
    }

    #endregion

    #region Constructors

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

    public event EventHandler<GenericEventArgs<WorkerOperation>> OperationCompleted;

    protected virtual void OnOperationCompleted(WorkerOperation workerOperation)
    {
        EventHandler<GenericEventArgs<WorkerOperation>> handler = this.OperationCompleted;
        if (handler != null)
        {
            handler(this, new GenericEventArgs<WorkerOperation>(workerOperation));
        }
    }

    #endregion

    #region Protected and private methods

    private WorkerOperation TakeNextRequestFromQueue()
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
    private void Run()
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