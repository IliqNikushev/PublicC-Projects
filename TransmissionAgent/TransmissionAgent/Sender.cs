using System;
using System.Net;
using System.Net.Sockets;

namespace TransmissionAgent
{
    public class Sender : TransmissionAgent
    {
        private TcpClient client;
        protected override Socket TargetSocket
        {
            get { return client == null ? null : client.Client; }
        }

        public override bool IsConnected
        {
            get { return client.Connected; }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            base.StopReceiving();
            this.client.Close();
        }

        public void ConnectTo(IPAddress address, int port)
        {
            this.client = new TcpClient();
            try
            {
                this.client.Connect(address, port);
            }
            catch
            {
                Console.WriteLine("Unable to connect to " + address + ":"+port);
                return;
            }
            base.StartListeningForMessages();
        }
    }
}
