using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace TransmissionAgent
{
    public abstract class TransmissionAgent
    {
        public static bool DebugIsEnabled = false;
        public const int SIZE_MESSAGE_MAX_MB = 1;
        public static int SIZE_MESSAGE_MAX_KB { get { return SIZE_MESSAGE_MAX_MB * 1024; } }
        public static int SIZE_MESSAGE_MAX_B { get { return SIZE_MESSAGE_MAX_KB * 8; } }

        public event Action<Message> OnMessageReceived = (x) => { };
        public event Action<Message> OnMessageSent = (x) => { };
        public abstract bool IsConnected { get; }

        protected abstract Socket TargetSocket { get; }
        private System.Threading.Thread messageListenerThread;

        private object messageSendLock = new object();

        private LinkedList<Message> notReceivedByTargetMessages = new LinkedList<Message>();
        private HashSet<uint> respondedMessages = new HashSet<uint>();

        private System.Threading.Thread keepSendingThread;
        private System.Threading.AutoResetEvent[] waitHandles = new System.Threading.AutoResetEvent[] { new System.Threading.AutoResetEvent(false) };

        private bool awaiting = false;

        protected void StartListeningForMessages()
        {
            if (messageListenerThread != null)
                return;
            if (TargetSocket == null)
                throw new ArgumentNullException("SOCKET NOT YET INITIALIZED");

            messageListenerThread = new System.Threading.Thread(ListenForMessage);
            messageListenerThread.Start();
            keepSendingThread = new System.Threading.Thread(KeepSending);
            keepSendingThread.Start();
        }

        protected virtual void OnCatchSocketException(SocketException ex)
        {

        }

        protected void StopReceiving()
        {
            if (messageListenerThread != null)
                messageListenerThread.Abort();
            messageListenerThread = null;
            if (keepSendingThread != null)
                keepSendingThread.Abort();
            keepSendingThread = null;
        }

        private void ListenForMessage()
        {
            try
            {
                while (TargetSocket.Connected)
                {
                    try
                    {
                        Message message = TargetSocket.Receive();
                        lock (messageSendLock)
                        {
                            OnMessageReceived(message);
                            if (message is AwaitingMessage)
                            {
                                waitHandles[0].Set();
                                continue;
                            }

                            if (TransmissionAgent.DebugIsEnabled)
                            {
                                Console.WriteLine(string.Join(", ", notReceivedByTargetMessages.Select(x => x.ID + "").ToArray()));
                                Console.WriteLine("RECEIVED:" + message.ID + " " + (message is Response ? (message as Response).TargetMessageID + "" : ""));
                            }
                            if (message is Response)
                                notReceivedByTargetMessages.Remove(notReceivedByTargetMessages.First(x => x.ID == (message as Response).TargetMessageID));
                            if (!(message is MessageReceivedResponse))
                                if (!respondedMessages.Contains(message.ID))
                                    SendMessage(new MessageReceivedResponse(message.ID));
                                else
                                    respondedMessages.Remove(message.ID);
                            else
                                if (notReceivedByTargetMessages.Count == 0 && !awaiting)
                                    SendMessage(new AwaitingMessage());
                            waitHandles[0].Set();
                        }
                    }
                    catch (SocketException) { throw; }
                    catch (Exception ex) { Console.WriteLine("Unable to receive message, " + ex.Message + " " + ex.StackTrace); }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Lost connection");
                OnCatchSocketException(ex);
            }
            catch(System.Threading.ThreadAbortException) { }
        }

        private void KeepSending()
        {
            while (true)
            {
                System.Threading.WaitHandle.WaitAll(waitHandles);
                lock(messageSendLock)
                    if (notReceivedByTargetMessages.Count > 0)
                    {
                        var l = notReceivedByTargetMessages.First;
                        if (l.Value is AwaitingMessage || l.Value is MessageReceivedResponse)
                            notReceivedByTargetMessages.RemoveFirst();
                        ProcessSend(l.Value);
                        if (TransmissionAgent.DebugIsEnabled)
                            Console.WriteLine("SENDING " + l.Value.ID + " " + l.Value.GetType());
                    }
            }
        }

        public void SendMessage(Message message)
        {
            lock(messageSendLock)
                if (TargetSocket != null)
                    if (TargetSocket.Connected)
                    {
                        if (message is MessageReceivedResponse)
                            notReceivedByTargetMessages.AddFirst(message);
                        else
                            notReceivedByTargetMessages.AddLast(message);
                        if (message is Response)
                            respondedMessages.Add((message as Response).TargetMessageID);
                        waitHandles[0].Reset();
                        
                        if (notReceivedByTargetMessages.Count == 1)
                            waitHandles[0].Set();
                    }
        }

        private void ProcessSend(Message message)
        {
            TargetSocket.Send(message);
            OnMessageSent(message);
            waitHandles[0].Reset();
        }
    }
}
