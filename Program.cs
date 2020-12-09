using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RoutingTableUpdater
{
    class Program
    {
        private enum MIB_IPFORWARD_TYPE : uint
        {
            MIB_IPROUTE_TYPE_OTHER = 1,
            MIB_IPROUTE_TYPE_INVALID = 2,
            MIB_IPROUTE_TYPE_DIRECT = 3,
            MIB_IPROUTE_TYPE_INDIRECT = 4
        }

        private enum MIB_IPPROTO : uint
        {
            MIB_IPPROTO_OTHER = 1,
            MIB_IPPROTO_LOCAL = 2,
            MIB_IPPROTO_NETMGMT = 3,
            MIB_IPPROTO_ICMP = 4,
            MIB_IPPROTO_EGP = 5,
            MIB_IPPROTO_GGP = 6,
            MIB_IPPROTO_HELLO = 7,
            MIB_IPPROTO_RIP = 8,
            MIB_IPPROTO_IS_IS = 9,
            MIB_IPPROTO_ES_IS = 10,
            MIB_IPPROTO_CISCO = 11,
            MIB_IPPROTO_BBN = 12,
            MIB_IPPROTO_OSPF = 13,
            MIB_IPPROTO_BGP = 14,
            MIB_IPPROTO_NT_AUTOSTATIC = 10002,
            MIB_IPPROTO_NT_STATIC = 10006,
            MIB_IPPROTO_NT_STATIC_NON_DOD = 10007
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_IPFORWARDROW
        {
            public uint dwForwardDest;
            public uint dwForwardMask;
            public uint dwForwardPolicy;
            public uint dwForwardNextHop;
            public uint dwForwardIfIndex;
            public MIB_IPFORWARD_TYPE dwForwardType;
            public MIB_IPPROTO dwForwardProto;
            public uint dwForwardAge;
            public uint dwForwardNextHopAS;
            public int dwForwardMetric1;
            public int dwForwardMetric2;
            public int dwForwardMetric3;
            public int dwForwardMetric4;
            public int dwForwardMetric5;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct MIB_IPFORWARDTABLE
        {
            public int dwNumEntries;
            public MIB_IPFORWARDROW table;
        }

        [DllImport("Iphlpapi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int CreateIpForwardEntry(ref MIB_IPFORWARDROW pRoute);

        [DllImport("Iphlpapi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int DeleteIpForwardEntry(ref MIB_IPFORWARDROW pRoute);

        [DllImport("Iphlpapi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int SetIpForwardEntry(MIB_IPFORWARDROW pRoute);

        [DllImport("Iphlpapi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private unsafe static extern int GetIpForwardTable(MIB_IPFORWARDTABLE* pIpForwardTable, ref int pdwSize, bool bOrder);

        private unsafe static List<MIB_IPFORWARDROW> GetRoutingTable()
        {
            var retVal = new List<MIB_IPFORWARDROW>();

            var size = Marshal.SizeOf(typeof(MIB_IPFORWARDROW));
            MIB_IPFORWARDTABLE* fwdTable = (MIB_IPFORWARDTABLE*)Marshal.AllocHGlobal(size);

            var result = GetIpForwardTable(fwdTable, ref size, true);
            if (result == 0x7A)
            {
                fwdTable = (MIB_IPFORWARDTABLE*)Marshal.ReAllocHGlobal((IntPtr)fwdTable, (IntPtr)size);
                result = GetIpForwardTable(fwdTable, ref size, true);
            }
            if (result == 0x00)
                retVal = Enumerable.Range(0, fwdTable->dwNumEntries)
                    .Select(i => new MIB_IPFORWARDROW
                    {
                        dwForwardDest = (&fwdTable->table)[i].dwForwardDest,
                        dwForwardMask = (&fwdTable->table)[i].dwForwardMask,
                        dwForwardPolicy = (&fwdTable->table)[i].dwForwardPolicy,
                        dwForwardNextHop = (&fwdTable->table)[i].dwForwardNextHop,
                        dwForwardIfIndex = (&fwdTable->table)[i].dwForwardIfIndex,
                        dwForwardType = (&fwdTable->table)[i].dwForwardType,
                        dwForwardProto = (&fwdTable->table)[i].dwForwardProto,
                        dwForwardAge = (&fwdTable->table)[i].dwForwardAge,
                        dwForwardNextHopAS = (&fwdTable->table)[i].dwForwardNextHopAS,
                        dwForwardMetric1 = (&fwdTable->table)[i].dwForwardMetric1,
                        dwForwardMetric2 = (&fwdTable->table)[i].dwForwardMetric2,
                        dwForwardMetric3 = (&fwdTable->table)[i].dwForwardMetric3,
                        dwForwardMetric4 = (&fwdTable->table)[i].dwForwardMetric4,
                        dwForwardMetric5 = (&fwdTable->table)[i].dwForwardMetric5
                    })
                    .ToList();

            Marshal.FreeHGlobal((IntPtr)fwdTable);
            return retVal;
        }

        private static string ToIpString(uint x) => string.Join(".", BitConverter.GetBytes(x));

        static void Main()
        {
            var interfaceName = "WLAN";
            var routingRules = new string[][] {
                new string[] { "10.0.0.0", "255.0.0.0" },
                new string[] { "49.52.96.0", "255.255.240.0" },
                new string[] { "58.198.64.0", "255.255.192.0" },
                new string[] { "202.120.112.0", "255.255.240.0" },
                new string[] { "202.121.192.0", "255.255.248.0" }
            }
                .Select(rr => rr
                    .Select(s => BitConverter.ToUInt32(IPAddress.Parse(s).GetAddressBytes()))
                    .ToList())
                .ToList();
            var wlanInterface = NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.Name == interfaceName);
            if (wlanInterface == null)
                Environment.Exit(1);
            var gatewayIp = BitConverter.ToUInt32(wlanInterface
                .GetIPProperties()
                .GatewayAddresses
                .First(ga => ga.Address.AddressFamily == AddressFamily.InterNetwork)
                .Address
                .GetAddressBytes());
            Console.WriteLine($"Gateway IP: {ToIpString(gatewayIp)}");
            var routingTable = GetRoutingTable();
            if (!routingTable.Any(rt => rt.dwForwardNextHop == gatewayIp))
                Environment.Exit(1);
            routingTable
                .Where(rtr => routingRules.Any(rr => rr[0] == rtr.dwForwardDest))
                .ToList()
                .ForEach(rtr =>
                {
                    var result = DeleteIpForwardEntry(ref rtr);
                    if (result == 0x00)
                        Console.WriteLine($"Successfully deleted a routing rule: {ToIpString(rtr.dwForwardDest)} MASK {ToIpString(rtr.dwForwardMask)} {ToIpString(rtr.dwForwardNextHop)}");
                    else
                        Console.WriteLine($"Unable to remove a routing rule ({result}): {ToIpString(rtr.dwForwardDest)} MASK {ToIpString(rtr.dwForwardMask)} {ToIpString(rtr.dwForwardNextHop)}");
                });
            routingRules
                .Select(rr => new MIB_IPFORWARDROW
                {
                    dwForwardDest = rr[0],
                    dwForwardMask = rr[1],
                    dwForwardNextHop = gatewayIp,
                    dwForwardIfIndex = Convert.ToUInt32(wlanInterface.GetIPProperties().GetIPv4Properties().Index),
                    dwForwardProto = MIB_IPPROTO.MIB_IPPROTO_NETMGMT,
                    dwForwardMetric1 = routingTable.First(rt => rt.dwForwardNextHop == gatewayIp).dwForwardMetric1
                })
                .ToList()
                .ForEach(rtr =>
                {
                    var result = CreateIpForwardEntry(ref rtr);
                    if (result == 0x00)
                        Console.WriteLine($"Successfully created a routing rule: {ToIpString(rtr.dwForwardDest)} MASK {ToIpString(rtr.dwForwardMask)} {ToIpString(rtr.dwForwardNextHop)}");
                    else
                        Console.WriteLine($"Unable to create a routing rule ({result}): {ToIpString(rtr.dwForwardDest)} MASK {ToIpString(rtr.dwForwardMask)} {ToIpString(rtr.dwForwardNextHop)}");
                });
        }
    }
}
