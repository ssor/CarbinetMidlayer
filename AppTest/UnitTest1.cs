using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AppCarbinetMidLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AppTest
{
    [TestClass]
    public class UnitTest1
    {
        int common_port_9601 = 9601;
        int common_port_9602 = 9602;

        [TestMethod]
        public void TagEventTest()
        {
            TagPool.ClearTagPool();
            TagPool.SetMinReadedCount(2);


            //新读取标签测试，由不存在到存在，事件变为  TagEvent_TagNew
            string epc = "300833B2DDD906C001010101";
            TagInfo ti01 = new TagInfo(epc, "01");
            TagPool.AddTag(ti01);
            TagPool.AddTag(ti01);
            TagPool.ResetExistsState();
            TagInfo tiTemp = TagPool.GetSpecifiedTag(epc);
            Assert.IsTrue(tiTemp.bThisTagExists == true);
            Assert.IsTrue(tiTemp.Event == TagEvent.TagEvent_TagNew);
            Assert.IsTrue(tiTemp.antennaID == "01");

            //标签消失测试，由存在变为不存在，事件变为 TagEvent_TagDeleted
            TagPool.ResetExistsState();
            TagPool.UpdateTagInfo(tiTemp, null);
            tiTemp = TagPool.GetSpecifiedTag(epc);
            Assert.IsTrue(tiTemp.bThisTagExists == false);
            Assert.IsTrue(tiTemp.Event == TagEvent.TagEvent_TagDeleted);
            Assert.IsTrue(tiTemp.antennaID == "01");

            //标签由消失变化默认状态，不存在状态延续，事件变为 TagEvent_Normal
            TagPool.ResetExistsState();
            tiTemp = TagPool.GetSpecifiedTag(epc);
            Assert.IsTrue(tiTemp.Event == TagEvent.TagEvent_Normal);
            Assert.IsTrue(tiTemp.antennaID == "01");

            //标签切换天线
            TagPool.AddTag(ti01);
            TagPool.AddTag(ti01);
            TagPool.ResetExistsState();
            tiTemp = TagPool.GetSpecifiedTag(epc);
            Assert.IsTrue(tiTemp.bThisTagExists == true);
            Assert.IsTrue(tiTemp.Event == TagEvent.TagEvent_TagNew);
            Assert.IsTrue(tiTemp.antennaID == "01");
            TagInfo ti02 = new TagInfo(epc, "02");
            TagPool.AddTag(ti02);
            TagPool.AddTag(ti02);
            TagPool.AddTag(ti02);
            TagPool.ResetExistsState();
            Assert.IsTrue(tiTemp.bThisTagExists == true);
            Assert.IsTrue(tiTemp.Event == TagEvent.TagEvent_SwitchAnt);
            Assert.IsTrue(tiTemp.antennaID == "02");

            //存在状态延续，事件由 TagEvent_SwitchAnt 变为 TagEvent_Normal
            TagPool.AddTag(ti02);
            TagPool.AddTag(ti02);
            TagPool.AddTag(ti02);
            TagPool.ResetExistsState();
            Assert.IsTrue(tiTemp.bThisTagExists == true);
            Assert.IsTrue(tiTemp.Event == TagEvent.TagEvent_Normal);
            Assert.IsTrue(tiTemp.antennaID == "02");

            //由消失变存在，事件由 
            //TagPool.AddTag(ti01);
            //TagPool.AddTag(ti01);
            //TagPool.ResetExistsState();
            //Assert.IsTrue(tiTemp.bThisTagExists == true);
            //Assert.IsTrue(tiTemp.Event == TagEvent.TagEvent_SwitchAnt);
            //Assert.IsTrue(tiTemp.antennaID == "01");


        }

        //基础的原始数据解析
        [TestMethod]
        public void ParseRawDataTest()
        {
            TagPool.AddParser(common_port_9601, TagInfo.GetParseRawTagDataFunc(common_port_9601, null));
            TagPool.AddParser(common_port_9602, TagInfo.GetParseRawTagDataFunc(common_port_9602, null));
            string data1 = "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010101   ";
            string data2 = "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010102   ";

            List<TagInfo> tags = TagPool.GetRawDataParser(common_port_9601)(data1);
            List<TagInfo> tags2 = TagPool.GetRawDataParser(common_port_9602)(data2);
            Assert.IsTrue(tags.Count == 1);
            Assert.IsTrue(tags[0].epc == "300833B2DDD906C001010101");
            Assert.IsTrue(tags[0].port == common_port_9601);
        }


        //测试不完整原始数据解析
        [TestMethod]
        public void ParseRawDataTest2()
        {
            string data = "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010101" +
                            "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010102 " +
                            "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:0 " +
                            "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010103 " +
                            "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Tag:300833B2DDD906C001010103  ";

            TagPool.AddParser(common_port_9601, TagInfo.GetParseRawTagDataFunc(common_port_9601, null));
            List<TagInfo> tags = TagPool.GetRawDataParser(common_port_9601)(data);
            Assert.IsTrue(tags.Count == 3);
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010101"; }));
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010102"; }));
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010103"; }));
        }


        [TestMethod]
        public void TagPoolTest()
        {
            TagPool.ClearTagPool();
            TagPool.SetMinReadedCount(2);

            List<TagInfo> tagList = TagPool.GetAllExistsTags();
            TagPool.ResetExistsState();
            Assert.IsTrue(tagList.Count == 0);

            TagInfo ti01 = new TagInfo("300833B2DDD906C001010101", "01");
            ti01.port = common_port_9601;
            TagPool.AddTag(ti01);
            TagPool.ResetExistsState();
            tagList = TagPool.GetAllExistsTags();
            Assert.IsTrue(tagList.Count == 0);
            tagList = TagPool.GetAllExistsTags(true);
            string str = JsonConvert.SerializeObject(tagList);
            Assert.IsTrue(tagList.Count == 1);
            //Assert.IsTrue(tagList[0].epc == "300833B2DDD906C001010101");

            TagInfo ti02 = new TagInfo("300833B2DDD906C001010102", "01");
            ti02.port = common_port_9602;
            TagPool.AddTag(ti01);
            TagPool.AddTag(ti02);
            TagPool.AddTag(ti02);
            TagPool.ResetExistsState();
            tagList = TagPool.GetAllExistsTags();
            Assert.IsTrue(tagList.Count == 1);
            Assert.IsTrue(tagList.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010102" && _tag.port == common_port_9602; }));

        }

        //解析完的数据直接通过回掉添加到pool中
        [TestMethod]
        public void TagPoolParseAndAddTagsTest()
        {
            string data = "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010101" +
                     "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010102 " +
                     "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:0 " +
                     "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010103 " +
                     "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Tag:300833B2DDD906C001010103  ";

            TagPool.AddParser(common_port_9601, TagInfo.GetParseRawTagDataFunc(common_port_9601, TagPool.AddTagRange), true);
            List<TagInfo> tags = TagPool.GetRawDataParser(common_port_9601)(data);
            //TagPool.IntervalResetState();
            tags = TagPool.GetAllExistsTags(true);
            Assert.IsTrue(tags.Count == 3);
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010101"; }));
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010102"; }));
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010103"; }));
        }

        [TestMethod]
        public void TagPoolResetTagStateTest()
        {
            TagPool.ClearTagPool();

            string epc = "300833B2DDD906C001010101";
            TagInfo ti01 = new TagInfo(epc, "01");
            ti01.port = common_port_9601;
            TagPool.AddTag(ti01);
            TagPool.AddTag(ti01);
            TagInfo tiTemp = TagPool.GetSpecifiedTag(epc);
            Assert.IsTrue(TagPool.GetMaxReadCountTag(tiTemp) == 2);
            TagPool.UpdateTagInfo(tiTemp, ti01);
            Assert.IsTrue(TagPool.GetMaxReadCountTag(tiTemp) == 3);
            TagPool.ResetReadCountDefault(tiTemp);
            TagPool.UpdateTagInfo(tiTemp, null);
            Assert.IsTrue(TagPool.GetMaxReadCountTag(tiTemp) == 0);
        }

        [TestMethod]
        public void ListReferenceTest()
        {
            TagPool.ClearTagPool();
            List<TagInfo> listSrc = new List<TagInfo> 
            { 
                new TagInfo("111","01"),
                new TagInfo("222","01")
            };
            TagPool.AddTagRange(listSrc);
            Assert.IsTrue(TagPool.GetMaxReadCountTag(listSrc[0]) == 1);

            List<TagInfo> listDest = new List<TagInfo>(listSrc);
            Assert.IsTrue(TagPool.GetMaxReadCountTag(listSrc[0]) == 1);

            //listSrc[0].ReadCount++;
            Assert.IsTrue(TagPool.GetMaxReadCountTag(listSrc[0]) == 1);

            listDest = listSrc.GetRange(0, listSrc.Count - 1);
            Assert.IsTrue(listDest.Count == 1);
            listDest = listDest.GetRange(0, listDest.Count - 1);
            Assert.IsTrue(listDest.Count == 0);

        }

        //测试忽略误读状态功能
        [TestMethod]
        public void SetIgnoreStateTest()
        {
            TagPool.SetMinReadedCount(2);
            TagPool.ClearTagPool();

            List<TagInfo> tagList = TagPool.GetAllExistsTags();
            Assert.IsTrue(tagList.Count == 0);


            TagInfo ti1 = new TagInfo("111", "01");
            TagInfo ti2 = new TagInfo("111", "03");
            TagPool.AddTag(ti1);
            TagPool.AddTag(ti2);
            TagPool.ResetExistsState();
            tagList = TagPool.GetAllExistsTags();
            Assert.IsTrue(tagList.Count == 0);
            TagPool.AddTag(ti1);
            TagPool.AddTag(ti2);
            TagPool.SetIgnoreMisreading(true);
            TagPool.ResetExistsState();
            tagList = TagPool.GetAllExistsTags();
            Assert.IsTrue(tagList.Count == 1);

        }

    }
}
