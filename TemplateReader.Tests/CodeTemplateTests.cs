using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NetCoreSystemEnvHelper;
using Xunit;
namespace TemplateReader.Tests
{
    public class UnitTest1
    {
        string resourcesFilePath = FileSysHelper.GetCurrentAppLocationPath () + "\\Resources\\TemplateReader.Tests.Resources\\";
        [Fact]
        public void Test1 ()
        {
            string file = resourcesFilePath + "TemplateTest1.test";
            CodeTemplate template = (new CodeTemplate (file)).ReadTemplate ();
            Assert.Equal (1, template.Root.ChildNodeList.Count);
        }

        [Fact]
        public void Test2 ()
        {
            string file = resourcesFilePath + "TemplateTest2.test";
            CodeTemplate template = (new CodeTemplate (file)).ReadTemplate ();
            Assert.Equal (1, template.Root.ChildNodeList.Count);
            Assert.Equal (3, template.Root.ChildNodeList[0].ChildNodeList.Count);

        }

        [Fact]
        public void Test3 ()
        {
            var str = string.Format ("{0}", 23.0);
            string file = resourcesFilePath + "TemplateTest3.test";
            CodeTemplate template = (new CodeTemplate (file)).ReadTemplate ();
            TreeParser parser = new TreeParser ();
            var res = parser.PraseTemplate (template);
            string outputFilePath = @"C:/Users/ITQ2CS/rtc_sandbox/rbd_briBk10_gly_sw_Development_RWS_Hansong_Dev/rbd/briBk10/gly/sw/01_ApplLyr/SysCtrl/ComWrp/src/ComWrp_ComWrapper_cc.c";
            File.WriteAllText (outputFilePath, res.ToString ());
            Assert.Equal (1, template.Root.ChildNodeList.Count);
            Assert.Equal (3, template.Root.ChildNodeList[0].ChildNodeList.Count);
        }

        [Fact]
        public void Test4 ()
        {
            string file = resourcesFilePath + "TemplateTest4.test";

            try
            {
                CodeTemplate template = (new CodeTemplate (file)).ReadTemplate ();
                TreeParser parser = new TreeParser ();
                var res = parser.PraseTemplate (template);
                string outputFilePath = @"C:/Users/ITQ2CS/rtc_sandbox/rbd_briBk10_gly_sw_Development_RWS_Hansong_Dev/rbd/briBk10/gly/sw/01_ApplLyr/SysCtrl/ADtRp/src/ADtRp_Report_cc.c";
                File.WriteAllText (outputFilePath, res.ToString ());
                Assert.Equal (1, template.Root.ChildNodeList.Count);
                Assert.Equal (3, template.Root.ChildNodeList[0].ChildNodeList.Count);
            }
            catch (Exception e)
            {

            }

        }
    }
}