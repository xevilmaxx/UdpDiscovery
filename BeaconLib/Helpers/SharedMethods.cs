using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace BeaconLib.Helpers
{
    public static class SharedMethods
    {
        internal static bool HasPrefix<T>(IEnumerable<T> haystack, IEnumerable<T> prefix)
        {
            return haystack.Count() >= prefix.Count() &&
                haystack.Zip(prefix, (a, b) => a.Equals(b)).All(_ => _);
        }

        /// <summary>
        /// Convert a string to network bytes
        /// </summary>
        internal static IEnumerable<byte> Encode(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var len = IPAddress.HostToNetworkOrder((short)bytes.Length);

            return BitConverter.GetBytes(len).Concat(bytes);
        }

        /// <summary>
        /// Convert network bytes to a string
        /// </summary>
        internal static string Decode(IEnumerable<byte> data)
        {
            var listData = data as IList<byte> ?? data.ToList();

            var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(listData.Take(2).ToArray(), 0));
            if (listData.Count() < 2 + len) throw new ArgumentException("Too few bytes in packet");

            return Encoding.UTF8.GetString(listData.Skip(2).Take(len).ToArray());
        }

        /// <summary>
        /// Return the machine's hostname (usually nice to mention in the beacon text)
        /// </summary>
        public static string HostName
        {
            get { return Dns.GetHostName(); }
        }
    }
}
