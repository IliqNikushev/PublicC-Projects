using System;

namespace TransmissionAgent
{
    [Serializable]
    public class NULL
    {
        public override bool Equals(object obj)
        {
            if (obj == null) return true;
            return false;
        }
    }

    [Serializable]
    public abstract class Message
    {
        private static object currentIDLock = new object();
        private static uint currentID = 0;
        public uint ID { get; private set; }
        public object Data { get; private set; }
        public Message(object data) { this.Data = data; lock (currentIDLock)this.ID = currentID++; }

        public static implicit operator bool(Message m)
        {
            return m != null;
        }
    }

    [Serializable]
    public abstract class Message<T> : Message
    {
        public new T Data { get { return (T)base.Data; } }
        public Message(T data) : base(data) { }
    }

    [Serializable]
    public sealed class AwaitingMessage : Message
    {
        public AwaitingMessage() : base(null) { }
    }

    [Serializable]
    public abstract class MethodInvoke
    {
        public string MethodName { get; private set; }
        public string Service { get; private set; }

        public MethodInvoke(string methodName, string service)
        {
            this.MethodName = methodName;
            this.Service = service;
        }
    }

    [Serializable]
    public class MethodInvokeRequest : MethodInvoke
    {
        public object[] Parameters { get; private set; }

        public MethodInvokeRequest(string methodName, string service, params object[] parameters) : base(methodName, service)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] == null)
                    parameters[i] = new NULL();
                if (!parameters[i].GetType().IsSerializable)
                    throw new NotImplementedException("CANNOT SERIALIZE " + parameters[i].GetType().FullName);
            }
                
            this.Parameters = parameters;
        }
    }

    [Serializable]
    public class MethodInvokeResponse : MethodInvoke
    {
        public object Result { get; private set; }

        public MethodInvokeResponse(string methodName, string service, object result) : base(methodName, service)
        {
            if (result == null)
                result = new NULL();

            if(!result.GetType().IsSerializable)
                throw new NotImplementedException("CANNOT SERIALIZE " + result.GetType().FullName);
            this.Result = result;
        }
    }

    [Serializable]
    public class MethodCallbackRequest : MethodInvokeRequest
    {
        public MethodCallbackRequest(string methodName, string service, params object[] parameters) : base(methodName, service, parameters)
        {
        }
    }

    [Serializable]
    public abstract class Request : Message
    {
        public Request(object data) : base(data) { }
    }

    [Serializable]
    public abstract class Request<T> : Request
    {
        public new T Data { get { return (T)base.Data; } }
        public Request(T data) : base(data) { }
    }

    [Serializable]
    public abstract class Response : Message
    {
        public uint TargetMessageID { get; private set; }
        public Response(uint messageID, object data) : base(data) { this.TargetMessageID = messageID; }
    }

    [Serializable]
    public abstract class Response<T> : Response
    {
        public new T Data { get { return (T)base.Data; } }
        public Response(uint messageID, T data) : base(messageID, data) {}
    }

    [Serializable]
    public sealed class InvokeMethodMessage : Request<MethodInvokeRequest>
    {
        public InvokeMethodMessage(MethodInvokeRequest data) : base(data) { }
    }

    [Serializable]
    public sealed class InvokeCallbackMessage : Request<MethodCallbackRequest>
    {
        public InvokeCallbackMessage(MethodCallbackRequest data) : base(data) { }
    }

    [Serializable]
    public sealed class InvokeMethodResultMessage : Response<MethodInvokeResponse>
    {
        public InvokeMethodResultMessage(uint messageID, MethodInvokeResponse data) : base(messageID, data) { }
    }

    [Serializable]
    public sealed class MessageReceivedResponse : Response<uint>
    {
        public MessageReceivedResponse(uint messageID) : base(messageID, messageID) { }
    }
}
