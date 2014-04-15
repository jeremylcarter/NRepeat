using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NRepeat
{
    public interface IProxy
    {
        IPEndPoint Server { get; set; }
        int Buffer { get; set; }
        bool Running { get; set; }
        void Start();
        void Stop();
    }
}
