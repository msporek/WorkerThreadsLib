using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkerThreadsLib;

/// <summary>
/// Class to be used for creating a pool of <see cref="WorkerThread"/> instances. It deals with the logic of scheduling operations 
/// on the next <see cref="WorkerThread"/> that is available, or has got less work to handle in its queue. 
/// </summary>
public class WorkerThreadPool
{
    private string _poolName;

    private int _threadCount = 0;

    private List<WorkerThread> _threads;

    private int _lastPushedToWorkerIndex = -1;

    private HashSet<string> _enqueuedOperationsKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private object _locker = new object();

    /// <summary>
    /// Gets the name of this thread pool. 
    /// </summary>
    public string Name => this._poolName;

    /// <summary>
    /// Gets the number of threads initiated in this thread pool. 
    /// </summary>
    public int ThreadCount => this._threadCount;

    /// <summary>
    /// Constructor creates a new instance of <see cref="WorkerThreadPool"/> class of given <paramref name="poolName"/> and number of threads 
    /// given by <paramref name="threadCount"/> argument. 
    /// </summary>
    /// 
    /// <param name="poolName">Name of the pool.</param>
    /// <param name="threadCount">Number of threads to be spawned in the pool.</param>
    /// 
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="poolName"/> is null.</exception>
    /// 
    /// <exception cref="ArgumentException">Thrown if <paramref name="poolName"/> is empty string, or if <paramref name="threadCount"/> is 
    /// equal to zero, or is less than zero.</exception>
    public WorkerThreadPool(string poolName, int threadCount)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(poolName, nameof(poolName));

        if (threadCount <= 0)
        {
            throw new ArgumentException($"The \"{threadCount}\" value should be a positive integer.", nameof(threadCount));
        }

        this._poolName = poolName;
        this._threadCount = threadCount;

        this._threads = new List<WorkerThread>();
        for (int i = 0; i < this._threadCount; i++)
        {
            WorkerThread workerThread = new WorkerThread($"WorkerThreadPool{this._poolName} {i + 1}", 10000000);
            workerThread.OperationCompleted += this.WorkerThread_OperationCompleted;
            workerThread.Start();

            this._threads.Add(workerThread);
        }
    }

    private void WorkerThread_OperationCompleted(object sender, GenericEventArgs<WorkerOperation> operation)
    {
        if (operation == null)
        {
            return;
        }

        lock (this._locker)
        {
            this._enqueuedOperationsKeys.Remove(operation.Data.OperationKey);
        }
    }

    /// <summary>
    /// Method returns a value indicating whether all threads in the pool have finished all their scheduled, operations, are free and do not 
    /// have any operations in their queues. 
    /// </summary>
    /// 
    /// <returns>True if all processing has been finished, otherwise false.</returns>
    public bool CheckIsAllProcessingCompleted()
    {
        lock (this._locker)
        {
            WorkerThread thisThread = this._threads.FirstOrDefault();
            return thisThread.CurrentQueueSize == 0 && !thisThread.IsBusy;
        }
    }

    /// <summary>
    /// Method schedules <paramref name="operation"/> on an thread. 
    /// </summary>
    /// 
    /// <param name="operation">Operation to be queued for execution.</param>
    /// 
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="operation"/> is null.</exception>
    public void Schedule(WorkerOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (this._locker)
        {
            if (this._enqueuedOperationsKeys.Contains(operation.OperationKey))
            {
                return;
            }

            int currentIndex = this._lastPushedToWorkerIndex + 1;
            while (true)
            {
                if (currentIndex >= this._threadCount)
                {
                    currentIndex = 0;
                }

                if (this._threads[currentIndex].TryEnqueue(operation))
                {
                    this._enqueuedOperationsKeys.Add(operation.OperationKey);
                    break;
                }

                currentIndex++;
            }

            this._lastPushedToWorkerIndex = currentIndex;
        }
    }
}