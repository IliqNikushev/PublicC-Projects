using System;
using System.Net.Sockets;

namespace TransmissionAgent
{
    public class Receiver : TransmissionAgent
    {
        public const int DEFAULT_PORT = 12345;

        private TcpListener listener;
        private Socket targetSocket;
        protected override Socket TargetSocket
        {
            get { return targetSocket; }
        }

        public override bool IsConnected
        {
            get { return targetSocket != null; }
        }

        public Receiver(int port)
        {
            this.listener = new TcpListener(Utils.LocalIPAddress, port);
            try
            {
                this.listener.Start();
            }
            catch 
            {
                Console.WriteLine("Unable to listen at " + port);
                return;
            }
            StartListening();
        }

        private void StartListening()
        {
            System.Threading.Thread waitForConnectionThread = new System.Threading.Thread(() => ListenForConnection());
            waitForConnectionThread.Start();
        }

        public void StopListening()
        {
            if (!IsConnected) return;

            base.StopReceiving();
            if (this.targetSocket != null)
            {
                this.targetSocket.Close();
                this.targetSocket = null;
            }
            this.listener.Stop();
        }

        private void ListenForConnection()
        {
            if(TransmissionAgent.DebugIsEnabled)
                Console.WriteLine("Awaiting client");
            Socket link = listener.AcceptSocket();
            if (targetSocket != null)
            {
                targetSocket.Close();
                base.StopReceiving();
            }
            targetSocket = link;
            base.StartListeningForMessages();
        }

        protected override void OnCatchSocketException(SocketException ex)
        {
            if (TransmissionAgent.DebugIsEnabled)
                Console.WriteLine("Connection lost to client");
            StartListening();
        }
    }
}
