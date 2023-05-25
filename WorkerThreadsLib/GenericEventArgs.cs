using System;

namespace WorkerThreadsLib;

public class GenericEventArgs<T> : EventArgs
{
    public T Data { get; set; }

    public GenericEventArgs(T data)
    {
        this.Data = data;
    }
}
