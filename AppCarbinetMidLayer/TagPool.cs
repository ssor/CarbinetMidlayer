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
        static bool bIgnoreMisreading = false;//是否忽略误读数据
        static List<string> UnmisreadingAnt = new List<string> { "01", "02", "04", "08" };

        static Dictionary<int, Func<string, List<TagInfo>>> ParserList = new Dictionary<int, Func<string, List<TagInfo>>>();
        static Dictionary<string, TagInfo> pool = new Dictionary<string, TagInfo>();

        public static void ClearTagPool()
        {
            pool.Clear();
        }
        public static void SetIgnoreMisreading(bool _b)
        {
            bIgnoreMisreading = _b;
        }

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
                };
            }
        }


        public static void AddTagRange(List<TagInfo> _tags)
        {
            List<TagInfo> tags = new List<TagInfo>(_tags);
            foreach (TagInfo ti in tags)
            {
                AddTag(ti);
            }
        }

        public static void AddTag(TagInfo _ti)
        {
            if (!pool.ContainsKey(_ti.epc))
            {

                //if (bIgnoreMisreading == false)//不允许误读数据通过
                //{
                //    if (!UnmisreadingAnt.Contains(_ti.antennaID))
                //    {
                //        return;
                //    }
                //}
                pool.Add(_ti.epc, _ti);
                UpdateTagInfo(_ti, _ti);

            }
            else
            {
                TagInfo ti = pool[_ti.epc];
                UpdateTagInfo(ti, _ti);
            }
#if TagPool_Debug
            Debug.WriteLine("AddTag ...");
            TagInfo temp = GetSpecifiedTag(_ti.epc);
            Debug.WriteLine(string.Format("ReadCount => {0} EPC => {1}", GetMaxReadCountTag(temp).ToString(), temp.epc));
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
            Debug.WriteLine(string.Format("ReadCount => {0} EPC => {1}", GetMaxReadCountTag(temp).ToString(), temp.epc));
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

        public static List<TagInfo> GetAllEventChangedTags()
        {
            return pool.Where((_kvp) =>
            {
                return _kvp.Value.Event != TagEvent.TagEvent_Normal;
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
            int maxCount = 0;
            if (bIgnoreMisreading == false)
            {
                maxCount = GetMaxReadCountTag(_ti);
            }
            else
            {
                maxCount = GetTotalReadCountTag(_ti);
            }
            Debug.WriteLine(string.Format("read_count => {0}   MIN => {1}  epc => {2}", maxCount.ToString(), MIN_READED_COUNT.ToString(), _ti.epc));
            return maxCount >= MIN_READED_COUNT;
        }


        static void SetTagExistsState(TagInfo _ti, bool _bState)
        {
            if (_ti.bThisTagExists == false && _bState == true)
            {
                _ti.Event = TagEvent.TagEvent_TagNew;
#if TagPool_Debug
                Debug.WriteLine("New Readed => " + _ti.epc + "                +++++");
#endif
                goto END;
            }
            if (_ti.bThisTagExists == false && _bState == false)
            {
                _ti.Event = TagEvent.TagEvent_Normal;
#if TagPool_Debug
                Debug.WriteLine("New unReaded => " + _ti.epc + "              -----");
#endif
                goto END;
            }

            if (_ti.bThisTagExists == true && _bState == false)
            {
                _ti.Event = TagEvent.TagEvent_TagDeleted;
                goto END;
            }
            if (_ti.bThisTagExists == true && _bState == true)
            {
                _ti.Event = TagEvent.TagEvent_Normal;
                int crtCount = GetCurrentTagReadCount(_ti);
                int maxCount = GetMaxReadCountTag(_ti);
                if (maxCount > crtCount)
                {
                    _ti.antennaID = GetMaxReadCountAnt(_ti);
                    _ti.Event = TagEvent.TagEvent_SwitchAnt;
                }
                goto END;
            }
        END:
            _ti.bThisTagExists = _bState;

        }


        public static string GetMaxReadCountAnt(TagInfo _ti)
        {
            List<TagReadRecord> list = new List<TagReadRecord>(_ti.antReadCountList);
            if (list.Count > 0)
            {
                return list.OrderByDescending(_trr => _trr.count).First().antID;
            }
            else
                return string.Empty;
        }


        public static int GetTotalReadCountTag(TagInfo _ti)
        {
            List<TagReadRecord> list = new List<TagReadRecord>(_ti.antReadCountList);
            if (list.Count > 0)
            {
                return list.Sum(trr => trr.count);
            }
            else return 0;
        }
        public static int GetMaxReadCountTag(TagInfo _ti)
        {
            List<TagReadRecord> list = new List<TagReadRecord>(_ti.antReadCountList);
            if (list.Count > 0)
            {
                return list.Where(_trr => UnmisreadingAnt.Contains(_trr.antID)).Max(trr => trr.count);
            }
            else return 0;
        }


        public static int GetCurrentTagReadCount(TagInfo _ti)
        {
            List<TagReadRecord> list = new List<TagReadRecord>(_ti.antReadCountList);
            TagReadRecord trr = list.FirstOrDefault(_trr => _trr.antID == _ti.antennaID);
            if (trr == null) return 0;
            else return trr.count;
        }


        public static void ResetReadCountDefault(TagInfo _ti)
        {
            Debug.WriteLine("Epc => " + _ti.epc + " ResetReadCountDefault => " + GetMaxReadCountTag(_ti).ToString());
            //_ti.ReadCount = 0;
            //_ti.bSetTagDefaultState = true;
            List<TagReadRecord> list = new List<TagReadRecord>(_ti.antReadCountList);
            _ti.antReadCountList = _ti.antReadCountList.Select((_trr) =>
            {
                _trr.count = 0;
                return _trr;
            }).ToList<TagReadRecord>();
        }


        public static void UpdateTagInfo(TagInfo _dest, TagInfo _src)
        {
            if (_dest == null) return;
            if (_src == null) return;
            Action<TagInfo, string> act = (_tag, _antID) =>
            {
                if (_tag.antReadCountList.Exists(_record => { return _antID == _record.antID; }))
                {
                    _tag.antReadCountList.Find(_record => { return _antID == _record.antID; }).count++;
                }
                else
                {
                    _tag.antReadCountList.Add(new TagReadRecord(_antID, 1));
                }
            };
            act(_dest, _src.antennaID);

#if TagPool_Debug
            Debug.WriteLine("IncreaseReadCount ...");
            TagInfo temp = GetSpecifiedTag(_dest.epc);
            Debug.WriteLine(string.Format("ReadCount => {0} EPC => {1}", GetMaxReadCountTag(temp).ToString(), temp.epc));
#endif
        }


        static bool GetTagExistsState(TagInfo _ti)
        {
            return _ti.bThisTagExists;
        }
        #endregion
    }
}
