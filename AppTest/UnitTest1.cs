using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AppCarbinetMidLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppTest
{
    [TestClass]
    public class UnitTest1
    {
        int common_port_9601 = 9601;
        int common_port_9602 = 9602;
        //基础的原始数据解析
        [TestMethod]
        public void ParseRawDataTest()
        {
            TagPool.AddParser(common_port_9601, TagInfo.GetParseRawTagDataFunc(common_port_9601, null));
            TagPool.AddParser(common_port_9602, TagInfo.GetParseRawTagDataFunc(common_port_9602, null));
            string data1 = "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010101   ";
            string data2 = "Disc:2000/02/28 20:01:51, Last:2000/02/28 20:07:42, Count:00019, Ant:02, Type:04, Tag:300833B2DDD906C001010102   ";

            List<TagInfo> tags = TagPool.GetParser(common_port_9601)(data1);
            List<TagInfo> tags2 = TagPool.GetParser(common_port_9602)(data2);
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
            List<TagInfo> tags = TagPool.GetParser(common_port_9601)(data);
            Assert.IsTrue(tags.Count == 3);
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010101"; }));
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010102"; }));
            Assert.IsTrue(tags.Exists((_tag) => { return _tag.epc == "300833B2DDD906C001010103"; }));
        }


        [TestMethod]
        public void TagPoolTest()
        {
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
            Assert.IsTrue(tagList.Count == 1);
            //Assert.IsTrue(tagList[0].epc == "300833B2DDD906C001010101");

            TagInfo ti02 = new TagInfo("300833B2DDD906C001010102", "01");
            ti02.port = common_port_9602;
            TagPool.AddTag(ti01);
            TagPool.AddTag(ti02);
            TagPool.AddTag(ti02);
            TagPool.ResetExistsState();
            tagList = TagPool.GetAllExistsTags();
            Assert.IsTrue(tagList.Count == 2);
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
            List<TagInfo> tags = TagPool.GetParser(common_port_9601)(data);
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
            string epc = "300833B2DDD906C001010101";
            TagInfo ti01 = new TagInfo(epc, "01");
            ti01.port = common_port_9601;
            TagPool.AddTag(ti01);
            TagPool.AddTag(ti01);
            TagInfo tiTemp = TagPool.GetSpecifiedTag(epc);
            Assert.IsTrue(tiTemp.ReadCount == 2);
            TagPool.IncreaseReadCount(tiTemp);
            Assert.IsTrue(tiTemp.ReadCount == 3);
            TagPool.ResetReadCountDefault(tiTemp);
            Assert.IsTrue(tiTemp.ReadCount == 1);
        }

        [TestMethod]
        public void ListReferenceTest()
        {
            List<TagInfo> listSrc = new List<TagInfo> 
            { 
                new TagInfo("111","01"),
                new TagInfo("222","01")
            };
            Assert.IsTrue(listSrc[0].ReadCount == 1);

            List<TagInfo> listDest = new List<TagInfo>(listSrc);
            Assert.IsTrue(listDest[0].ReadCount == 1);

            listSrc[0].ReadCount++;
            Assert.IsTrue(listDest[0].ReadCount == 2);

            listDest = listSrc.GetRange(0, listSrc.Count - 1);
            Assert.IsTrue(listDest.Count == 1);
            listDest = listDest.GetRange(0, listDest.Count - 1);
            Assert.IsTrue(listDest.Count == 0);

        }

    }
}
