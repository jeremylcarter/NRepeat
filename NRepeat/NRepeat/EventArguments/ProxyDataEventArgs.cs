using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NRepeat
{
    public class ProxyDataEventArgs: EventArgs
    {
        public int Bytes;

        public ProxyDataEventArgs(int bytes)
        {
            Bytes = bytes;
        }
    }

    public class ProxyByteDataEventArgs : EventArgs
    {
        public byte[] Bytes;
        public string Source { get; set; }
        public ProxyByteDataEventArgs(byte[] bytes, string source)
        {
            Bytes = bytes;
            Source = source;
        }
    }
}
