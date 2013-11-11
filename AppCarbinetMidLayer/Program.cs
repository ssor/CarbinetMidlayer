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
        static List<SubscriberClient> ClientList = new List<SubscriberClient>();
        static void Main(string[] args)
        {

            int common_port_9601 = 9601;
            int common_port_9602 = 9602;


            ReaderManager.addReader(common_port_9601, "9601");
            ReaderManager.addReader(common_port_9602, "9602");



            Action<int> StartUDPListener = (port) =>
            {

                TagPool.AddParser(port, TagInfo.GetParseRawTagDataFunc(port, TagPool.AddTagRange), true);
                UDPServer.StartUDPServer(port,
                    (_port, _data) =>
                    {
                        TagPool.GetRawDataParser(_port)(_data);
                    });
            };

            StartUDPListener(common_port_9601);
            StartUDPListener(common_port_9602);

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
                    try
                    {
                        MessageInfo mi = JsonConvert.DeserializeObject(message) as MessageInfo;
                        if (mi != null)
                        {
                            if (mi.command == MessageInfo.GET_ALL_TAGS)
                            {
                                List<TagInfo> tags = TagPool.GetAllExistsTags();
                                string json = JsonConvert.SerializeObject(tags);
                                socket.Send(json);
                            }
                            else if (mi.command == MessageInfo.SUBSCRIBE_READER)
                            {
                                Debug.WriteLine("subscribe => " + mi.content);

                                string[] reader = mi.content.Split(',');
                                if (reader.Length > 0)
                                {
                                    updateClientSubscribedReaderList(socket, ClientList, reader.ToList<string>());
                                }
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }


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

        static void updateClientSubscribedReaderList(IWebSocketConnection client, List<SubscriberClient> list, List<string> _readerList)
        {
            SubscriberClient c = list.Find((_client) =>
            {
                return _client.client.ConnectionInfo.Id == client.ConnectionInfo.Id;
            });
            if (c != null)
            {
                c.subscribedReaderList.AddRange(_readerList);
            }
        }
        static void removeClient(IWebSocketConnection client, List<SubscriberClient> list)
        {
            SubscriberClient c = list.Find((_client) =>
            {
                return _client.client.ConnectionInfo.Id == client.ConnectionInfo.Id;
            });
            if (c != null)
            {
                list.Remove(c);
                //Debug.WriteLine("Client --  => " + list.Count.ToString());
            }


        }


        static void addClient(IWebSocketConnection client, List<SubscriberClient> list)
        {
            SubscriberClient c = list.Find((_client) =>
            {
                return _client.client.ConnectionInfo.Origin == client.ConnectionInfo.Origin;
            });
            if (c == null)
            {
                list.Add(new SubscriberClient(client));
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
            List<SubscriberClient> list = new List<SubscriberClient>(ClientList);
            foreach (SubscriberClient socket in list)
            {
                socket.client.Send(json);
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
