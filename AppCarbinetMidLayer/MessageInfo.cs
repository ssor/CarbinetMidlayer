using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppCarbinetMidLayer
{
    public class MessageInfo
    {
        public static string GET_ALL_TAGS = "alltags";
        public static string SUBSCRIBE_READER = "subscribe";


        public string command = string.Empty;
        public string content = string.Empty;
    }
}
