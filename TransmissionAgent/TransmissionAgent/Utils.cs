using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TransmissionAgent
{
    public static class Utils
    {
        public static string ExternalIPAddress
        {
            get
            {
                try
                {
                    string url = "http://checkip.dyndns.org";
                    if (TransmissionAgent.DebugIsEnabled)
                        Console.WriteLine("Checking ip");
                    string response = new System.Net.WebClient().DownloadString(url);
                    if (TransmissionAgent.DebugIsEnabled)
                        Console.WriteLine("Found ip");
                    string[] a = response.Split(':');
                    string a2 = a[1].Substring(1);
                    string[] a3 = a2.Split('<');
                    string a4 = a3[0];
                    return a4;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static IPAddress LocalIPAddress
        {
            get
            {
                //check wireless
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && item.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
                //check ethernet
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if ((
                        item.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        item.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                        item.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                        item.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                        item.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                            && item.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
                //assume has internet
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip;
                    }
                }
                throw new Exception("Local IP Address Not Found!");
            }
        }
    }
}