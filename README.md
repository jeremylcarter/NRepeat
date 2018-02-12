NRepeat
--------

NRepeat is a simple C# TCP Proxy and VNC Repeater. You decide!

NRepeat can work as an Ultra VNC repeater.

The server will be improved with a GUI, backend and REST API.

What Next?
-----------------
We need to improve the stability. Perhaps move out of the two tasks running per proxy connection and into the IAsyncResult options available in the TcpListener. Have a database of connected clients or thread safe list that allows the modification and deletion of clients. Logging of clients, data used and the time spent. REST API so that you can control the server via Javascript/NodeJS or Web front end.


Technical Details
-----------------

NRepeat is released under the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

If you use NRepeat in commercial software you sell, you MUST also open source your software.
