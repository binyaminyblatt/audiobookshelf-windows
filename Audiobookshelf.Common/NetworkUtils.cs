using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Audiobookshelf.Common
{
    public class LocalIPInfo
    {
        public string IPAddress { get; set; }
        public string InterfaceName { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(InterfaceName))
                return IPAddress;
            return $"{IPAddress} ({InterfaceName})";
        }
    }

    public static class NetworkUtils
    {
        public static List<LocalIPInfo> GetLocalIPv4Addresses()
        {
            var results = new List<LocalIPInfo>();
            var seenIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ipStr = addr.Address.ToString();
                            // Exclude loopback (127.x), APIPA (169.254.x), and duplicate addresses
                            if (!seenIps.Contains(ipStr) && !ipStr.StartsWith("127.") && !ipStr.StartsWith("169.254."))
                            {
                                seenIps.Add(ipStr);
                                results.Add(new LocalIPInfo
                                {
                                    IPAddress = ipStr,
                                    InterfaceName = ni.Name,
                                    Description = ni.Description
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback using Dns.GetHostEntry
                try
                {
                    string hostName = Dns.GetHostName();
                    IPHostEntry host = Dns.GetHostEntry(hostName);
                    foreach (IPAddress ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ipStr = ip.ToString();
                            if (!seenIps.Contains(ipStr) && !ipStr.StartsWith("127.") && !ipStr.StartsWith("169.254."))
                            {
                                seenIps.Add(ipStr);
                                results.Add(new LocalIPInfo
                                {
                                    IPAddress = ipStr,
                                    InterfaceName = "Local Adapter",
                                    Description = "Local IPv4 Address"
                                });
                            }
                        }
                    }
                }
                catch { }
            }

            return results;
        }
    }
}
