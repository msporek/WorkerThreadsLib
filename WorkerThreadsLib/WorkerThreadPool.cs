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

    private int _count = 0;

    private List<WorkerThread> _threads;

    private int _lastPushedToWorkerIndex = -1;

    private HashSet<string> _enqueuedOperationsKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private object _locker = new object();

    public WorkerThreadPool(string poolName, int count)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(poolName, nameof(poolName));

        if (count <= 0)
        {
            throw new ArgumentException($"The \"{count}\" value should be a positive integer.", nameof(count));
        }

        this._poolName = poolName;
        this._count = count;

        this._threads = new List<WorkerThread>();
        for (int i = 0; i < this._count; i++)
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

    public bool CheckIsAllProcessingCompleted()
    {
        lock (this._locker)
        {
            WorkerThread thisThread = this._threads.FirstOrDefault();
            return thisThread.CurrentQueueSize == 0 && !thisThread.IsBusy;
        }
    }

    public void Schedule(WorkerOperation operation)
    {
        lock (this._locker)
        {
            if (this._enqueuedOperationsKeys.Contains(operation.OperationKey))
            {
                return;
            }

            int currentIndex = this._lastPushedToWorkerIndex + 1;
            while (true)
            {
                if (currentIndex >= this._count)
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