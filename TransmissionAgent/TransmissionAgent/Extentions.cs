using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Net.Sockets;

namespace TransmissionAgent
{
    public static class Extentions
    {
        static BinaryFormatter binaryFormatter = new BinaryFormatter();
        public static Message ToMessage(this byte[] buffer)
        {
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(buffer, 0, buffer.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return binaryFormatter.Deserialize(memoryStream) as Message;
        }

        public static byte[] ToBytes(this Message message)
        {
            MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, message);
            memoryStream.Seek(0, SeekOrigin.Begin);
            byte[] result = null;
            using (var r = new System.IO.BinaryReader(memoryStream))
                result = r.ReadBytes((int)memoryStream.Length);
            if (result.Length > TransmissionAgent.SIZE_MESSAGE_MAX_B)
                throw new InvalidDataException("CANNOT HAVE MESSAGE > " + TransmissionAgent.SIZE_MESSAGE_MAX_B +"BYTES (real:"+result.Length+")");
            return result;
        }

        public static void Send(this Socket socket, Message message)
        {
            byte[] bytes = message.ToBytes();
            socket.Send(bytes);
            if(TransmissionAgent.DebugIsEnabled)
                Console.WriteLine("Sent " + bytes.Length + "bytes");
        }

        public static Message Receive(this Socket socket)
        {
            byte[] messageBuffer = new byte[TransmissionAgent.SIZE_MESSAGE_MAX_B];
            int received = socket.Receive(messageBuffer);
            if (TransmissionAgent.DebugIsEnabled)
                Console.WriteLine("Received : " + received + "bytes");
            return messageBuffer.ToMessage();
        }
    }
}
