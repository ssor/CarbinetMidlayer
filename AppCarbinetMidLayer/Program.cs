﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AppCarbinetMidLayer
{
    class Program
    {
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

            StartIntervalCheck(20000);

            Console.Read();
        }
        static void StartIntervalCheck(int interval, Func<bool> predictor = null)
        {
            System.Timers.Timer timer = new System.Timers.Timer(interval);
            timer.Elapsed += (sender, e) =>
            {
                TagPool.ResetExistsState();
                Console.WriteLine("***************************************");
                List<TagInfo> tags = TagPool.GetAllExistsTags();
                PrintTagInfo(tags);
                Console.WriteLine("***************************************");
                Console.WriteLine();
            };

            timer.Enabled = true;
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
