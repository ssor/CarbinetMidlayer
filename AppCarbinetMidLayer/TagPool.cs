#define TagPool_Debug
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AppCarbinetMidLayer
{
    public class TagPool
    {
        static int MIN_READED_COUNT = 2;

        static Dictionary<int, Func<string, List<TagInfo>>> ParserList = new Dictionary<int, Func<string, List<TagInfo>>>();
        static Dictionary<string, TagInfo> pool = new Dictionary<string, TagInfo>();


        public static void SetMinReadedCount(int _count)
        {
            if (_count > 0)
            {
                MIN_READED_COUNT = _count;
            }
        }

        /// <summary>
        /// 添加解析闭包
        /// </summary>
        /// <param name="_flag">闭包的存储标识，一般为端口号</param>
        /// <param name="_parser">闭包函数</param>
        /// <param name="_bReplace">是否用新闭包强制替换就闭包</param>
        public static void AddParser(int _flag, Func<string, List<TagInfo>> _parser, bool _bReplace = false)
        {
            if (!ParserList.ContainsKey(_flag))
            {
                ParserList.Add(_flag, _parser);
            }
            else if (_bReplace)
            {
                ParserList[_flag] = _parser;
            }
        }
        public static Func<string, List<TagInfo>> GetParser(int _flag)
        {
            if (ParserList.ContainsKey(_flag))
            {
                return ParserList[_flag];
            }
            else
            {
                //默认解析函数，提示异常
                return (_data) =>
                {
                    throw new Exception();
                    //int port = _flag;
                    //List<TagInfo> listR = new List<TagInfo>();
                    //return listR;
                };
            }
        }


        public static void AddTagRange(List<TagInfo> _tags)
        {
            //List<TagInfo> tags = new List<TagInfo>(_tags);
            foreach (TagInfo ti in _tags)
            {
                AddTag(ti);
            }
        }

        public static void AddTag(TagInfo _ti)
        {
            if (!pool.ContainsKey(_ti.epc))
            {
                pool.Add(_ti.epc, _ti);

            }
            else
            {
                TagInfo ti = pool[_ti.epc];
                IncreaseReadCount(ti);
            }
#if TagPool_Debug
            Debug.WriteLine("AddTag ...");
            TagInfo temp = GetSpecifiedTag(_ti.epc);
            Debug.WriteLine(string.Format("ReadCount => {0} EPC => {1}", temp.ReadCount.ToString(), temp.epc));
#endif
        }

        //每隔一定时间，鉴定标签是否满足确定为读取到的条件
        public static void ResetExistsState()
        {
            Debug.WriteLine("TagPool => ResetExistsState ...");

            List<TagInfo> list = pool.Select((_kvp) => _kvp.Value).ToList<TagInfo>();
            LoopTagForExistsState(list);
        }
        static void LoopTagForExistsState(List<TagInfo> _list)
        {
            if (_list.Count <= 0) return;

            int count = _list.Count;
            TagInfo ti = _list[count - 1];
            if (CheckExistsRequirement(ti))
            {
                SetTagExistsState(ti, true);
                ResetReadCountDefault(ti);
            }
            else
            {
                SetTagExistsState(ti, false);
                ResetReadCountDefault(ti);
            }

#if TagPool_Debug
            Debug.WriteLine("LoopTagForExistsState ...");
            TagInfo temp = GetSpecifiedTag(ti.epc);
            Debug.WriteLine(string.Format("ReadCount => {0} EPC => {1}", temp.ReadCount.ToString(), temp.epc));
#endif

            LoopTagForExistsState(_list.GetRange(0, count - 1));
        }


        /// <summary>
        /// 查找所有存在的标签
        /// </summary>
        /// <param name="_bExist">默认存在的标签才能返回，但是可以通过参数true获取所有标签</param>
        /// <returns></returns>
        public static List<TagInfo> GetAllExistsTags(bool _bExist = false)
        {
            return pool.Where((_kvp) =>
            {
                return GetTagExistsState(_kvp.Value) || _bExist;
            })
            .Select((_kvp) => { return _kvp.Value; }).ToList<TagInfo>();
        }
        public static List<TagInfo> GetSpecifiedExistsTags(int _port)
        {
            return pool.Where((_kvp) => { return GetTagExistsState(_kvp.Value) && _kvp.Value.port == _port; })
            .Select((_kvp) => { return _kvp.Value; }).ToList<TagInfo>();
        }
        public static TagInfo GetSpecifiedTag(string _epc)
        {
            if (!pool.ContainsKey(_epc))
            {
                return null;

            }
            else
            {
                return pool[_epc];
            }
        }
        #region Helper Func
        static bool CheckExistsRequirement(TagInfo _ti)
        {
            Debug.WriteLine(string.Format("read_count => {0}   MIN => {1}  epc => {2}", _ti.ReadCount.ToString(), MIN_READED_COUNT.ToString(), _ti.epc));
            return _ti.ReadCount >= MIN_READED_COUNT;
        }
        static void SetTagExistsState(TagInfo _ti, bool _bState)
        {
            _ti.bThisTagExists = _bState;
#if TagPool_Debug
            if (_bState)
            {
                Debug.WriteLine("New Readed => " + _ti.epc + "                +++++");
            }
            else
            {
                Debug.WriteLine("New unReaded => " + _ti.epc + "              -----");
            
            }
#endif
        }
        public static void ResetReadCountDefault(TagInfo _ti)
        {
            Debug.WriteLine("Epc => " + _ti.epc + " ResetReadCountDefault => " + _ti.ReadCount.ToString());
            _ti.ReadCount = 1;
        }
        public static void IncreaseReadCount(TagInfo _ti)
        {
            //Debug.WriteLine(string.Format("Before IncreaseReadCount => {0}  epc => {1}", _ti.ReadCount.ToString(), _ti.epc));
            _ti.ReadCount++;
            //Debug.WriteLine(string.Format("After IncreaseReadCount => {0}  epc => {1}", _ti.ReadCount.ToString(), _ti.epc));
#if TagPool_Debug
            Debug.WriteLine("IncreaseReadCount ...");
            TagInfo temp = GetSpecifiedTag(_ti.epc);
            Debug.WriteLine(string.Format("ReadCount => {0} EPC => {1}", temp.ReadCount.ToString(), temp.epc));
#endif
        }
        static bool GetTagExistsState(TagInfo _ti)
        {
            return _ti.bThisTagExists;
        }
        #endregion
    }
}
