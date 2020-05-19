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
            string file = resourcesFilePath + "TemplateTest3.test";
            CodeTemplate template = (new CodeTemplate (file)).ReadTemplate ();
            TreeParser parser = new TreeParser ();
            var res = parser.PraseTemplate (template);
            string outputFilePath = @"C:\Users\ITQ2CS\Desktop\temp\template.c";
            File.WriteAllText (outputFilePath, res.ToString ());
            Assert.Equal (1, template.Root.ChildNodeList.Count);
            Assert.Equal (3, template.Root.ChildNodeList[0].ChildNodeList.Count);
        }
    }
}