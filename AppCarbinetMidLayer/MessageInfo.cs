using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppCarbinetMidLayer
{
    /// <summary>
    /// 用于与客户端交互时使用
    /// </summary>
    public class MessageInfo
    {
        public const string GET_ALL_TAGS = "alltags";//客户端要求返回所有标签
        public const string GET_SUBSCRIBE_READER_TAGS = "subscribedtags";//订阅的读写器的所有读到的标签
        public const string SUBSCRIBE_READER = "subscribe";//客户端要求订阅读写器


        public string command = string.Empty;
        public string content = string.Empty;

        public MessageInfo()
        {}

        //{"command":"subscribe","content":"9602"}
    }
}
