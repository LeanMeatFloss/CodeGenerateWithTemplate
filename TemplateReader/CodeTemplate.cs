using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TemplateReader
{
    public class CodeTemplate
    {
        public CodeTemplate (string Path)
        {
            CodeTemplateFile = new FileInfo (Path);

        }
        FileInfo CodeTemplateFile { get; set; }
        public CodeTemplate ReadTemplate ()
        {
            Root = MarkItem.GetInstance ();
            ProcessReadingMarkTree (File.ReadAllText (CodeTemplateFile.FullName), Root);
            return this;
        }
        public MarkItem Root { get; set; }
        internal void ProcessReadingMarkTree (string template, MarkItem Parent)
        {

            string content = template;
            //TryToReadMark
            if (!Regex.IsMatch (template, MarkMatchPattern))
            {
                //if there is no match,adding the template to the parent.ContentPartsList
                Parent.ContentPartsList.Add (template);
                return;
            }
            Match headMatch = Regex.Match (template, MarkMatchPattern);
            //Searching the index
            string markHeadContent = headMatch.Groups[1].Value;
            //Setup a new instance
            MarkItem node = MarkItem.GetInstance ().ReadHeadMark (markHeadContent.Last ().Equals ('/') ? markHeadContent.Remove (markHeadContent.Length - 1) : markHeadContent);
            //Judging whether the close mark is itself or need to find?
            if (markHeadContent[markHeadContent.Length - 1] == '/')
            {
                //self close mark
                //split in to several parts
                Parent.ContentPartsList.Add (template.Substring (0, headMatch.Index - 0));
                string afterTemplate = template.Substring (headMatch.Index + headMatch.Length);
                Parent.ChildNodeList.Add (node);
                //afterTemplate has the same parent,
                ProcessReadingMarkTree (afterTemplate, Parent);
            }
            else
            {
                //nether in the end or begin, try to search the close mark
                string headCloseString = string.Format (EndMarkHeadFormatString, node.Type.ToString ());
                int closePosition = LocateToTheCloseMark (node.Type.ToString (), template, headMatch.Index);
                //Split in to parts.
                Parent.ContentPartsList.Add (template.Substring (0, headMatch.Index - 0));
                //the afterTemplate means the template last after the mark,
                string afterTemplate = template.Substring (closePosition + headCloseString.Length);

                Parent.ChildNodeList.Add (node);
                string subTemplate = template.Substring (headMatch.Index + headMatch.Length, closePosition - (headMatch.Index + headMatch.Length));
                ProcessReadingMarkTree (subTemplate, node);
                //afterTemplate has the same parent,
                ProcessReadingMarkTree (afterTemplate, Parent);
            }
        }
        static int LocateToTheCloseMark (string markHead, string content, int startIndex)
        {
            string headBeginPattern = string.Format (MarkHeadMatchPatternFormat, markHead);
            string headEnd = "$$$/" + markHead;
            int startPosition = startIndex;
            int endPosition = content.IndexOf (headEnd, startIndex);

            while (true)
            {
                Match headMatch = Regex.Match (content.Substring (startPosition + 1), headBeginPattern);
                if (headMatch.Success && !headMatch.Groups[1].Value.EndsWith ('/'))
                {
                    startPosition = headMatch.Index + startPosition + 1;
                }
                else if (headMatch.Groups[1].Value.EndsWith ('/'))
                {
                    startPosition = headMatch.Index + startPosition + 1;
                    continue;
                }
                else
                {
                    break;
                }
                if (startPosition < endPosition)
                {
                    endPosition = content.IndexOf (headEnd, endPosition + 1);
                }
                else
                {
                    //there is no startmark,
                    break;
                }
            }
            if (endPosition == -1)
            {
                throw new Exception ("not find close mark");
            }
            else
            {
                return endPosition;
            }

        }

        static string MarkMatchPattern = @"\$\$\$([\s\S]+?)\$\$\$";
        static string MarkHeadMatchPatternFormat = @"\$\$\$({0}[\s\S]*?)\$\$\$";
        static string EndMarkHeadFormatString = @"$$$/{0}$$$";
        public class MarkItem
        {
            public enum NodeTypeEnum
            {

                Content,
                Array,
                Code,
                Resources,
                TemplateSelector,
                Template,
                Instance,
                Value,
                InputArgs,
                FormatArgs,
                InputArgValue,
            }
            public enum NodeAttributeEnum
            {
                Context,
                Name,
                Path,
                Type,
                InputArgs,
                Property,
                Function,
                DataSource,
                Value,
                Format,
                Index,
                LineSpace,

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
                string markHead = Regex.Match (headMark, MarkHeadPattern).Groups[1].Value;
                Type = (MarkItem.NodeTypeEnum) Enum.Parse (typeof (MarkItem.NodeTypeEnum), markHead);
                foreach (Match match in (Regex.Matches (headMark.Remove (0, markHead.Length), AttributeMatchPatternStringFormat)))
                {
                    string[] keyValuePair = match.Groups[1].Value.Split (new char[] { '=', '"' }, StringSplitOptions.None);
                    AttributesDict[(MarkItem.NodeAttributeEnum) Enum.Parse (typeof (MarkItem.NodeAttributeEnum), keyValuePair[0])] = keyValuePair.Length > 1 ? keyValuePair[2] : "";
                }
                return this;
            }
            static string AttributeMatchPatternStringFormat = @"[\s]+([\S]*=" + "\"" + @"[\S\s]*?" + "\"" + @")";
            static string MarkHeadPattern = @"[\s/]*([\S^/]*)";
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