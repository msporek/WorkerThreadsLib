using System;

namespace WorkerThreadsLib;

/// <summary>
/// Generic implementation of <see cref="EventArgs"/> that can carry data of any type <typeparamref name="T"/>. 
/// </summary>
/// 
/// <typeparam name="T">Type of data to be carried with the <see cref="GenericEventArgs{T}"/>.</typeparam>
public class GenericEventArgs<T> : EventArgs
{
    public T Data { get; set; }

    public GenericEventArgs(T data = default(T))
    {
        this.Data = data;
    }
}
