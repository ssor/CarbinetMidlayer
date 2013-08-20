using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AppCarbinetMidLayer
{
    public class TagEvent
    {
        public const string TagEvent_Normal = "normal";
        public const string TagEvent_TagNew = "tagNew";
        public const string TagEvent_TagDeleted = "tagDeleted";
        public static string TagEvent_SwitchAnt = "tagSwitchAnt";
    }
    public class TagReadRecord
    {
        public string antID = string.Empty;
        public int count = 0;
        public TagReadRecord(string _id, int _count)
        {
            this.antID = _id;
            this.count = _count;
        }
    }
    public class TagInfo
    {

        #region 成员
        public int port;
        public bool bThisTagExists = false;
        //public int ReadCount = 1;//读取到的次数
        public string antennaID = string.Empty;
        //public string tagType = string.Empty;
        public string epc = string.Empty;
        //public Dictionary<string, int> antReadCountList = new Dictionary<string, int>();
        public List<TagReadRecord> antReadCountList = new List<TagReadRecord>();
        //public string getTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        //public int milliSecond = DateTime.Now.Millisecond;
        //StringBuilder stringBuilder = new StringBuilder();

        public long milliSecond = 0;//时间标记

        public string Event = TagEvent.TagEvent_Normal;

        #endregion

        #region 构建函数
        public TagInfo() { }
        public TagInfo(string _epc, string _ant)
        {
            this.epc = _epc;
            this.antennaID = _ant;
        }

        #endregion
        public string toString()
        {
            string str = string.Empty;
            str = string.Format("Port => {0}:ant -> {1} | epc -> {2}  |  {3}",
                                this.port, this.antennaID, this.epc, this.Event);

            return str;
        }

        public static Func<string, List<TagInfo>> GetParseRawTagDataFunc(int _port, Action<List<TagInfo>> _callback)
        {
            return (_data) =>
            {
                int port = _port;
                StringBuilder stringBuilder = new StringBuilder();

                List<TagInfo> listR = new List<TagInfo>();
                if (_data == null || _data.Length <= 0)
                {
                    return listR;
                }
                stringBuilder.Append(_data);
                string temp1 = stringBuilder.ToString();
                string match_string = @"Disc:\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}, Last:\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}, Count:(?<count>\d{5}), Ant:(?<ant>\d{2}), Type:\d{2}, Tag:(?<epc>[0-9A-F]{24})";
                MatchCollection mc = Regex.Matches(temp1, match_string);
                foreach (Match m in mc)
                {
                    string strCmd = m.ToString();
                    string epc = m.Groups["epc"].Value;
                    string ant = m.Groups["ant"].Value;
                    //string count = m.Groups["count"].Value;
                    TagInfo ti = new TagInfo(epc, ant);
                    ti.port = port;
                    listR.Add(ti);
                    stringBuilder.Replace(strCmd, "");
                }

                stringBuilder.Replace("\r\n", "");
                stringBuilder.Replace("    ", "");
                if (_callback != null) _callback(listR);
                return listR;
            };
        }
    }
}
