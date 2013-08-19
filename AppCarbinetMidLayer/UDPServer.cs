//#define DEBUG_UDP
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace AppCarbinetMidLayer
{
    public class UDPServer
    {
        static Dictionary<int, UDPServer> udpServerList = new Dictionary<int, UDPServer>();
        public bool bRunning = false;
        //public ManualResetEvent Manualstate = new ManualResetEvent(true);
        //public StringBuilder sbuilder = new StringBuilder();
        public Socket serverSocket;
        public int port = 5000;
        byte[] byteData = new byte[1024];
        Action<int, string> callBack = null;


        public static UDPServer StartUDPServer(int _port, Action<int, string> _callback = null)
        {
            if (udpServerList.ContainsKey(_port))
            {
                return udpServerList[_port];
            }
            else
            {
                UDPServer server = new UDPServer(_port);
                bool state = server.startUDPListening(_callback);
                if (state)
                {
                    udpServerList.Add(_port, server);
                    return server;
                }
                else
                {
                    return null;
                }
            }
        }
        public UDPServer(int _port)
        {
            this.port = _port;
        }
        public void stop_listening()
        {
            try
            {
                if (serverSocket != null)
                {
                    serverSocket.Shutdown(SocketShutdown.Both);
                    serverSocket.Close();
                    serverSocket = null;
                    this.bRunning = false;
                    Console.WriteLine("Stop UDP Listening on " + this.port.ToString());
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("UDPServer -> " + ex.Message);
            }
        }
        public bool startUDPListening(Action<int, string> _callback = null)
        {
            if (bRunning == true)
            {
                return true;
            }
            try
            {
                bRunning = true;
                this.callBack = _callback;

                //We are using UDP sockets
                serverSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);
                IPAddress ip = IPAddress.Parse(GetLocalIP4());
                IPEndPoint ipEndPoint = new IPEndPoint(ip, port);
                //                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);

                //Bind this address to the server
                serverSocket.Bind(ipEndPoint);
                //防止客户端强行中断造成的异常
                long IOC_IN = 0x80000000;
                long IOC_VENDOR = 0x18000000;
                long SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

                byte[] optionInValue = { Convert.ToByte(false) };
                byte[] optionOutValue = new byte[4];
                serverSocket.IOControl((int)SIO_UDP_CONNRESET, optionInValue, optionOutValue);

                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                //The epSender identifies the incoming clients
                EndPoint epSender = (EndPoint)ipeSender;

                //Start receiving data
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length,
                    SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);


                Console.WriteLine("Start UDP Listening on " + this.port.ToString()+ " ...");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    string.Format("UDPServer.startUDPListening  -> error = {0}"
                    , ex.Message));
                return false;
            }
        }
        public void OnReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint epSender = (EndPoint)ipeSender;

                serverSocket.EndReceiveFrom(ar, ref epSender);

                string strReceived = Encoding.UTF8.GetString(byteData);
                strReceived = strReceived.Substring(0, strReceived.IndexOf("\0"));
                Array.Clear(byteData, 0, byteData.Length);
                if (this.callBack != null)
                {
                    this.callBack(this.port, strReceived);
                }

#if DEBUG_UDP
                Debug.WriteLine(
                    string.Format("UDPServer {0} received = {1}",
                  this.port.ToString(), strReceived));
#endif
                //int i = strReceived.IndexOf("\0");
                //Manualstate.WaitOne();
                //Manualstate.Reset();
                ////todo here should deal with the received string
                //sbuilder.Append(strReceived.Substring(0, i));
                //if (sbuilder.Length > 19600)//内容太多清楚掉
                //{
                //    sbuilder.Remove(0, sbuilder.Length);
                //}
                //Manualstate.Set();

                //Start listening to the message send by the user
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epSender,
                    new AsyncCallback(OnReceive), epSender);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    string.Format("UDPServer.OnReceive  -> error = {0}"
                    , ex.Message));
            }
        }
        static string GetLocalIP4()
        {
            IPAddress ipAddress = null;
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            for (int i = 0; i < ipHostInfo.AddressList.Length; i++)
            {
                ipAddress = ipHostInfo.AddressList[i];
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    break;
                }
                else
                {
                    ipAddress = null;
                }
            }
            if (null == ipAddress)
            {
                return null;
            }
            return ipAddress.ToString();
        }
    }
}
