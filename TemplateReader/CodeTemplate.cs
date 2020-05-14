using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TemplateReader
{
    public class CodeTemplate
    {
        public CodeTemplate (string Path)
        {
            CodeTemplateFile = new FileInfo ();
        }
        FileInfo CodeTemplateFile { get; set; }
        public void ProcessReadingMarkTree (string template, MarkItem Parent)
        {

            string content = template;
            //TryToReadMark
            if (!Regex.IsMatch (template, MarkMatchPattern))
            {
                //if there is no match,adding the template to the parent.ContentPartsList
                Parent.ContentPartsList.Add (template);
            }
            Match headMatch = Regex.Match (template, MarkMatchPattern);
            //Searching the index
            string markHeadContent = headMatch.Groups[1].Value;
            //Settingup a new instance
            MarkItem node = new MarkItem (markHeadContent);
            //Judging whether the close mark is itself or need to find?
            if (markHeadContent[markHeadContent.Length - 1] == '/')
            {
                //self close mark
                //split in to several parts
                Parent.ContentPartsList.Add (template.Substring (0, headMatch.Index - 0));
                string afterTemplate = template.Substring (headMatch.Index + headMatch.Length);
                //afterTemplate has the same parent,
                ProcessReadingMarkTree (afterTemplate, Parent);
            }
            else
            {
                //nether in the end or begin, try to search the close mark
                string headCloseString = string.Format (EndMarkHeadFormatString, markHead);
                int closePosition = CodeTemplateString.LastIndexOf (headCloseString);
                //Split in to parts.
                Parent.ContentPartsList.Add (template.Substring (0, headMatch.Index - 0));
                //the afterTemplate means the template last after the mark,
                string afterTemplate = template.Substring (closePosition + headCloseString.Length);
                //afterTemplate has the same parent,
                ProcessReadingMarkTree (afterTemplate, Parent);

                Parent.ChildNodeList.Add (node);
                string subTemplate = template.Substring (headMatch.Index + headMatch.Length, closePosition - (headMatch.Index + headMatch));
                ProcessReadingMarkTree (subTemplate, node);
            }
        }

        string MarkMatchPattern = @"$$$([\s\S]*?)$$$";

        string EndMarkHeadFormatString = @"$$$/{0}$$$";
        class MarkItem
        {
            public enum NodeTypeEnum
            {
                Root,
                Content,
                Array,
                Resources,
            }
            public enum NodeAttributeEnum
            {
                Context,

            }
            private MarkItem ()
            {

            }
            public static MarkItem GetInstance ()
            {
                return new MarkItem ();
            }
            public MarkItem ReadHeadMark (string headMark)
            {
                string markHead = Regex.Match (markHeadContent, headMark).Groups[1].Value;
                NodeType = Enum.Parse (typeof (MarkItem.NodeTypeEnum), markHead) as MarkItem.NodeTypeEnum;
                foreach (Group group in Regex.Match (headMark, string.Format (AttributeMatchPatternStringFormat, markHead)).Groups.Skip (1))
                {
                    string[] keyValuePair = group.Value.Split ('=');
                    AttributesDict[Enum.Parse (typeof (MarkItem.NodeAttributeEnum), keyValuePair[0]) as MarkItem.NodeAttributeEnum] = keyValuePair[1];
                }
                return this;
            }
            string AttributeMatchPatternStringFormat = @"{0}[\s]+([\S]*=[\S]*[\s]+)*";
            string MarkHeadPattern = @"[\s/]*([\S^/]*)";
            void ReadAttributes (string headMark)
            {

            }
            public NodeTypeEnum Type { get; set; }
            public List<string> ContentPartsList { get; set; } = new List<string> ();
            public List<MarkItem> ChildNodeList { get; set; } = new List<MarkItem> ();
            public Dictionary<NodeAttributeEnum, string> AttributesDict { get; set; } = new Dictionary<NodeAttributeEnum, string> ();

        }
    }
}