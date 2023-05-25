﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace WorkerThreadsLib;

public class WorkerThread
{
    public const int DefaultMaxQueueSize = 10000;

    #region Public properties and methods

    public string ThreadName
    {
        get { return thread.Name; }
    }

    public int MaxQueueSize
    {
        get { return queueMaxSize; }
    }

    public int CurrentQueueSize
    {
        get
        {
            lock (queueOfOperations)
            {
                return queueOfOperations.Count;
            }
        }
    }

    public bool IsActive
    {
        get { return isActive; }
    }

    public bool IsBusy
    {
        get { return isBusy; }
    }

    public virtual bool TryEnqueue(WorkerOperation request)
    {
        lock (queueOfOperations)
        {
            if (queueOfOperations.Count < queueMaxSize)
            {
                queueOfOperations.AddLast(request);
                actionEnqueued.Set();

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
        thread.Start();
    }

    public void Stop()
    {
        threadStopped.Set();
    }

    #endregion

    #region Constructors

    public WorkerThread(string threadName, int queueMaxSize = DefaultMaxQueueSize)
    {
        if (queueMaxSize <= 0)
        {
            throw new ArgumentException("\"queueMaxSize\" is supposed to be a positive value.");
        }

        this.queueMaxSize = queueMaxSize;

        thread = new Thread(Run);
        thread.Name = threadName;
    }

    public event EventHandler<GenericEventArgs<WorkerOperation>> OperationCompleted;

    protected virtual void OnOperationCompleted(WorkerOperation workerOperation)
    {
        EventHandler<GenericEventArgs<WorkerOperation>> handler = OperationCompleted;
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
        lock (queueOfOperations)
        {
            // If the queue is not empty - try to get its first item and remove it from the queue
            if (queueOfOperations.Count > 0)
            {
                request = queueOfOperations.First.Value;
                queueOfOperations.RemoveFirst();
            }

            // If the queue is empty after removing first item - we need to reset methodEnqueued_
            if (queueOfOperations.Count == 0)
            {
                actionEnqueued.Reset();
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
            isActive = true;

            WaitHandle[] eventHandles = new WaitHandle[] { threadStopped, actionEnqueued };
            while (isActive)
            {
                int index = WaitHandle.WaitAny(eventHandles);
                switch (index)
                {
                    case 0:
                        {
                            // Thread was stopped, change IsActive to false and terminate it
                            isActive = false;
                            break;
                        }
                    case 1:
                        {
                            // There is a new method to be executed - taking it from the queue
                            WorkerOperation nextRequest = TakeNextRequestFromQueue();

                            // If method was successfully got from the queue - invoking it
                            if (nextRequest != null)
                            {
                                isBusy = true;

                                RunOperation(nextRequest);
                                OnOperationCompleted(nextRequest);

                                isBusy = false;
                            }

                            break;
                        }
                }
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
            isBusy = false;
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
            // TODO: Pass this information up by an event. 
            // ACLogger.Instance.Error($"Error occured on running WorkerOperation of key: {operation.OperationKey}.", ex);
        }
    }

    private TimeSpan minTimeSpanToWait = TimeSpan.FromSeconds(5);

    #endregion

    #region Private fields

    // Max queue size. 
    protected int queueMaxSize = 1;

    // Thread object. 
    protected Thread thread = null;

    // Is this thread active and busy. 
    protected bool isActive = false;
    protected bool isBusy = false;

    // Some thread synchronization objects. 
    protected ManualResetEvent actionEnqueued = new ManualResetEvent(false);
    protected ManualResetEvent threadStopped = new ManualResetEvent(false);

    // Queue of methods to be invoked. 
    protected LinkedList<WorkerOperation> queueOfOperations = new LinkedList<WorkerOperation>();

    #endregion
}