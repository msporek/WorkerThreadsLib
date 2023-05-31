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
    private string poolName;

    private int count = 0;

    private List<WorkerThread> threads;

    private int lastPushedToWorkerIndex = -1;

    private HashSet<string> enqueuedOperationsKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private object locker = new object();

    public WorkerThreadPool(string poolName, int count)
    {
        this.poolName = poolName;
        this.count = count;

        threads = new List<WorkerThread>();
        for (int i = 0; i < this.count; i++)
        {
            WorkerThread workerThread = new WorkerThread($"WorkerThreadPool{this.poolName} {i + 1}", 10000000);
            workerThread.OperationCompleted += WorkerThread_OperationCompleted;
            workerThread.Start();

            threads.Add(workerThread);
        }
    }

    private void WorkerThread_OperationCompleted(object sender, GenericEventArgs<WorkerOperation> operation)
    {
        if (operation == null)
        {
            return;
        }

        lock (locker)
        {
            enqueuedOperationsKeys.Remove(operation.Data.OperationKey);
        }
    }

    public bool CheckIsAllProcessingCompleted()
    {
        lock (locker)
        {
            WorkerThread thisThread = threads.FirstOrDefault();
            return thisThread.CurrentQueueSize == 0 && !thisThread.IsBusy;
        }
    }

    public void Schedule(WorkerOperation operation)
    {
        lock (locker)
        {
            if (enqueuedOperationsKeys.Contains(operation.OperationKey))
            {
                return;
            }

            int currentIndex = lastPushedToWorkerIndex + 1;
            while (true)
            {
                if (currentIndex >= count)
                {
                    currentIndex = 0;
                }

                if (threads[currentIndex].TryEnqueue(operation))
                {
                    enqueuedOperationsKeys.Add(operation.OperationKey);
                    break;
                }

                currentIndex++;
            }

            lastPushedToWorkerIndex = currentIndex;
        }
    }
}