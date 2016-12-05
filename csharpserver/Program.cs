
/*-----------------------------------------------------------------------
 *
 * Program: Http Server
 * Usage:   csharpserver <portnum>
 * Authors: Vladimir Georgiev, Josiah Burchard
 *-----------------------------------------------------------------------
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

class SimpleEchoServer
{

    public static string OTHER_PAGE = "<head></head><body><html><h1>Heyo.This is another page.</h1> <a style=\"color: red;\" href=\"/\">Take me back home with this red link.</a></html></body>";

    public static string HOME_PAGE = @"<head></head><body><html><h1>Welcome to the CS480 Demo Server</h1>
            <p>Why not visit: <ul>
                <li><a href=""http://www2.semo.edu/csdept/""> Computer Science Home Page</a></li>
                <li><a href=""http://cstl-csm.semo.edu/liu/cs480_fall2012/index.htm"">CS480 Home Page<a></li>
            </ul></html></body>";

    public static void Main(string[] args)
    {
        int recv;
        byte[] data = new byte[1024];

        if (args.Length > 1) // Test for correct # of args
            throw new ArgumentException("Parameters: [<Port>]");

        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, Int32.Parse(args[0]));
        Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(ipep);
        server.Listen(10);
        List<string> log = new List<string>();

        Parallel.For(0, 500, i =>
        {
            Socket socket = server.Accept();

            string request = "";

            try
            {

                IPEndPoint clientep = (IPEndPoint)socket.RemoteEndPoint;
                Console.WriteLine("Connected with {0} at port {1}", clientep.Address, clientep.Port);

                data = new byte[1024];
                recv = socket.Receive(data);
                request = Encoding.ASCII.GetString(data, 0, recv);
                List<string> requestList = request.Split(' ').ToList();

                if (requestList.Count < 2)
                {
                    throw new Exception("So sorry, I didn't catch that. ");
                }

                string cmd = requestList[0];
                string requestUrl = requestList[1];

                if (cmd != "GET")
                {
                    throw new Exception("This server only knows GET commands");
                }

                List<string> homePageUrls = new List<string>()
                {
                    "/",
                    "index.html",
                    "index.htm",
                };
                if (homePageUrls.Contains(requestUrl))
                {
                    SendText(HOME_PAGE, socket);
                }
                else if (requestUrl == "/other.html" || requestUrl == "/other.htm")
                {
                    SendText(OTHER_PAGE, socket);
                }
                else
                {
                    string extension = requestUrl.Substring(requestUrl.LastIndexOf('.') + 1);
                    if (extension == "html" || extension == "htm")
                    {
                        SendText(File.ReadAllText($"../../files{(requestUrl)}"), socket);
                    }
                    else
                    {
                        SendFile(requestUrl, socket);
                    }
                }

                Console.WriteLine("Disconnected from {0}", clientep.Address);
            }
            catch (Exception e)
            {
                Console.WriteLine($">> {request} Uh oh! Uno problema: {e.Message}");
            }

            socket.Close();
            socket = server.Accept();
        });

        server.Close();
    }

    public static void SendText(string text, Socket socket)
    {
        string response = $"HTTP/1.0 200 OK\r\nServer: CS480 Demo Web Server\r\nContent-Length: {text.Length}\r\nContent-Type: text/html\r\n\r\n{text}";
        socket.Send(Encoding.ASCII.GetBytes(response));
    }
    public static void SendFile(string fileUrl, Socket socket)
    {
        FileInfo info = new FileInfo($"../../files{fileUrl}");
        string headers = $"HTTP/1.0 200 OK\r\nServer: CS480 Demo Web Server\r\nContent-Length: {info.Length}\r\nContent-Disposition: inline;filename=\"{fileUrl.Substring(1)};\"\r\n\r\n";
        socket.Send(Encoding.ASCII.GetBytes(headers), headers.Count(), 0);
        socket.SendFile($"../../files{fileUrl}");
    }
}