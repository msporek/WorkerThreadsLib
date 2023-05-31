using System;

namespace WorkerThreadsLib;

/// <summary>
/// Generic implementation of <see cref="EventArgs"/> that can carry data of any type <typeparamref name="T"/>. 
/// </summary>
/// 
/// <typeparam name="T">Type of data to be carried with the <see cref="GenericEventArgs{T}"/>.</typeparam>
public class GenericEventArgs<T> : EventArgs
{
    /// <summary>
    /// Data that the instance of event args carries. 
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Constructor creates a new instance of <see cref="GenericEventArgs{T}"/> with the data passed as an argument.
    /// </summary>
    /// 
    /// <param name="data">Data to be carried with the event args. 
    /// This argument can be null / default for <typeparamref name="T"/> type.</param>
    public GenericEventArgs(T data = default(T))
    {
        this.Data = data;
    }
}
