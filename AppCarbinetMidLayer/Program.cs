using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fleck;
using Newtonsoft.Json;

namespace AppCarbinetMidLayer
{
    class Program
    {
        //static List<IWebSocketConnection> MonitorClientList = new List<IWebSocketConnection>();
        static List<IWebSocketConnection> ClientList = new List<IWebSocketConnection>();
        static void Main(string[] args)
        {
            int common_port_9601 = 9601;
            int common_port_9602 = 9602;

            Action<int> act = (port) =>
            {

                TagPool.AddParser(port, TagInfo.GetParseRawTagDataFunc(port, TagPool.AddTagRange), true);
                UDPServer.StartUDPServer(port,
                    (_port, _data) =>
                    {
                        TagPool.GetParser(_port)(_data);
                    });
            };

            act(common_port_9601);
            act(common_port_9602);

            StartWebSocketServer(9701);

            StartIntervalCheck(15000);



            string line;

        READ_LINE: line = Console.ReadLine();
            if (line == "ignoreMisreading")
            {
                Console.WriteLine("Input State:  y   or   n ?");
                string state = Console.ReadLine();
                if (state.ToLower() == "y")
                {
                    TagPool.SetIgnoreMisreading(true);
                    Console.WriteLine("Current Ignore Misreading State:  true");
                }
                if (state.ToLower() == "n")
                {
                    TagPool.SetIgnoreMisreading(false);
                    Console.WriteLine("Current Ignore Misreading State:  false");
                }
                goto READ_LINE;
            }
            else
            {
                goto READ_LINE;
            }

            //****************************************
            //Console.ReadLine();
        }


        #region WebSocket Server
        static void StartWebSocketServer(int _websocketPort)
        {
            string url = "ws://localhost:" + _websocketPort.ToString();
            WebSocketServer server = new WebSocketServer(url);
            server.Start(socket =>
            {
                string originurl = socket.ConnectionInfo.Host + socket.ConnectionInfo.Path;
                socket.OnOpen = () =>
                {
                    Console.WriteLine(originurl + " connected");

                    if (socket.ConnectionInfo.Path == "/Client")
                    {
                        addClient(socket, ClientList);
                        Debug.WriteLine("Client ++  => " + ClientList.Count.ToString());
                    }
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine(originurl + " closed");
                    removeClient(socket, ClientList);

                };
                socket.OnMessage = message =>
                {
                    Debug.WriteLine("OnMessage => " + message);

                    //MessageInfo mi = JsonConvert.DeserializeObject(message) as MessageInfo;
                    //if (mi != null)
                    //{
                    //    if (mi.command == "allTags")
                    //    {
                    //        List<TagInfo> tags = TagPool.GetAllExistsTags();
                    //        string json = JsonConvert.SerializeObject(tags);
                    //        socket.Send(json);
                    //    }

                    //}

                    List<TagInfo> tags = TagPool.GetAllExistsTags();
                    string json = JsonConvert.SerializeObject(tags);
                    socket.Send(json);

                    /*
                     [{"port":9601,"bThisTagExists":true,"antennaID":"04","tagType":"","epc":"300833B2DDD906C001010101","antReadCountList":[{"antID":"04","count":0}],"milliSecond":0,"Event":"tagNew"},{"port":9601,"bThisTagExists":true,"antennaID":"02","tagType":"","epc":"300833B2DDD906C001010102","antReadCountList":[{"antID":"02","count":0}],"milliSecond":0,"Event":"tagNew"}]
                     * */

                };
                socket.OnError = (error) =>
                {
                    Debug.WriteLine("OnError => " + error.Data);
                    removeClient(socket, ClientList);

                };
            });
        }


        static void removeClient(IWebSocketConnection client, List<IWebSocketConnection> list)
        {
            IWebSocketConnection c = list.Find((_client) =>
            {
                return _client.ConnectionInfo.Id == client.ConnectionInfo.Id;
            });
            if (c != null)
            {
                list.Remove(client);
                //Debug.WriteLine("Client --  => " + list.Count.ToString());
            }


        }


        static void addClient(IWebSocketConnection client, List<IWebSocketConnection> list)
        {
            IWebSocketConnection c = list.Find((_client) =>
            {
                return _client.ConnectionInfo.Origin == client.ConnectionInfo.Origin;
            });
            if (c == null)
            {
                list.Add(client);
                //Debug.WriteLine("Client ++  => " + list.Count.ToString());
            }
        }

        #endregion

        static void StartIntervalCheck(int interval, Func<bool> predictor = null)
        {
            System.Timers.Timer timer = new System.Timers.Timer(interval);
            timer.Elapsed += (sender, e) =>
            {
                TagPool.ResetExistsState();
                ReportEventChangedTag();
#if APP_DEBUG
                //Console.WriteLine("***************************************");
                //List<TagInfo> tags = TagPool.GetAllEventChangedTags();
                //List<TagInfo> tags = TagPool.GetAllExistsTags();
                //PrintTagInfo(tags);
                //Console.WriteLine("***************************************");
                //Console.WriteLine();
#endif
            };

            timer.Enabled = true;
        }
        static void ReportEventChangedTag()
        {
            List<TagInfo> tags = TagPool.GetAllEventChangedTags();
            string json = JsonConvert.SerializeObject(tags);
            List<IWebSocketConnection> list = new List<IWebSocketConnection>(ClientList);
            foreach (IWebSocketConnection socket in list)
            {
                socket.Send(json);
            }
        }
        static void PrintTagInfo(List<TagInfo> _tagList)
        {
            foreach (TagInfo ti in _tagList)
            {
                Console.WriteLine(ti.toString());
            }
        }
    }


}
