using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppCarbinetMidLayer
{
    public class ReaderInfo
    {
        public int port;
        public string name;

        public ReaderInfo(int _port, string _name)
        {
            this.port = _port;
            this.name = _name;
        }
    }

    public class ReaderManager
    {
        static List<ReaderInfo> readerList = new List<ReaderInfo>();
        public static void addReader(ReaderInfo ri)
        {
            readerList.Add(ri);
        }
        public static void addReader(int _port, string _name)
        {
            ReaderInfo ri = readerList.Find((_reader) =>
            {
                return _reader.port == _port;
            });
            if (ri == null)
            {
                addReader(new ReaderInfo(_port, _name));
            }
        }

        public static ReaderInfo getReaderByPort(int _port)
        {
            return readerList.Find((_reader) =>
            {
                return _reader.port == _port;
            });
        }
        public static ReaderInfo getReaderByName(string _name)
        {
            return readerList.Find((_reader) =>
            {
                return _reader.name == _name;
            });
        }
        public static List<int> getPortsByName(List<string> _filter)
        {
            return readerList.Where((_ri) => { return _filter.Contains(_ri.name); })
                             .Select((_ri) => { return _ri.port; }).ToList<int>();
        }
    }
}
