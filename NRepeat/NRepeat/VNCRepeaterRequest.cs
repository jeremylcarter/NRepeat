using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NRepeat
{
    public class VNCRepeaterDefinition
    {
        public DateTime FirstRequest { get; set; }
        public string Version { get; set; }
        public bool Authenticated { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public IPEndPoint ServerEndPoint { get; set; }
        public IPEndPoint ClientEndPoint { get; set; }

        public VNCRepeaterDefinition()
        {
            // Default the token source for 1 hour
            CancellationTokenSource = new CancellationTokenSource(new TimeSpan(1,0,0));
        }

        public void Cancel()
        {
            if (CancellationTokenSource !=null) CancellationTokenSource.Cancel();
        }
    }
}
