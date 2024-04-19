using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatSharp
{
    internal class RequestManager
    {
        internal Dictionary<string, RequestOperation> PendingOperations { get; } = new Dictionary<string, RequestOperation>();

        public void QueueOperation(string key, RequestOperation operation)
        {
            if (!PendingOperations.TryAdd(key, operation))
                throw new InvalidOperationException("Operation is already pending.");
        }

        public RequestOperation PeekOperation(string key)
        {
            var realKey =
                PendingOperations.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            return PendingOperations[realKey];
        }

        public RequestOperation DequeueOperation(string key)
        {
            var operation = PendingOperations[key];
            PendingOperations.Remove(key);
            return operation;
        }
    }

    internal class RequestOperation
    {
        public RequestOperation(object state, Action<RequestOperation> callback)
        {
            State = state;
            Callback = callback;
        }

        public object State { get; set; }
        public Action<RequestOperation> Callback { get; set; }
    }
}