using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NRepeat
{
    public class VNCRepeaterDefinition
    {
        public DateTime FirstRequest { get; set; }
        public string Version { get; set; }
        public bool Authenticated { get; set; }

        public IPEndPoint ServerEndPoint { get; set; }
        public IPEndPoint ClientEndPoint { get; set; }

    }
}
