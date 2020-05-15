using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetCoreSystemEnvHelper;
namespace TemplateReader
{
    public class TreeParser
    {
        public struct DataTreeProperties
        {
            public object Context { get; set; }
            public Dictionary<string, object> ObjectDict { get; set; }
            public string LineSpace { get; set; }
        }
        public string PraseTemplate (CodeTemplate template)
        {
            StringBuilder codeGenerate = new StringBuilder ();
            RootDataProperties = new DataTreeProperties () { ObjectDict = new Dictionary<string, object> () };
            ParseMarkItem (codeGenerate, template.Root, RootDataProperties);
            return codeGenerate.ToString ();
        }
        void ParseMarkItem (StringBuilder codeGenerate, CodeTemplate.MarkItem item, DataTreeProperties Context)
        {
            ReadBasicAttributes (item.AttributesDict, ref Context);
            switch (item.Type)
            {
                case CodeTemplate.MarkItem.NodeTypeEnum.Content:
                    //append list
                    {
                        for (int i = 0; i < item.ChildNodeList.Count + item.ContentPartsList.Count; i++)
                        {

                            if (i % 2 == 0)
                            {
                                if (item.ContentPartsList[i / 2].Equals ("\r\n"))
                                {

                                }
                                else
                                {

                                    string newLine = item.ContentPartsList[i / 2];
                                    newLine = newLine.Replace ("\n", "\n" + Context.LineSpace);
                                    codeGenerate.Append (newLine);
                                }

                            }
                            else
                            {
                                ParseMarkItem (codeGenerate, item.ChildNodeList[i / 2], Context);
                            }
                        }
                    }
                    break;
                case CodeTemplate.MarkItem.NodeTypeEnum.Array:
                    {
                        object context = Context.Context;
                        if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.DataSource))
                        {

                            //Searching for the path of context
                            context = SearchingPropertyFromPath (context, item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.DataSource]);
                        }
                        foreach (var arrayItem in ((IEnumerable) (context)))
                        {
                            DataTreeProperties arrayContext = new DataTreeProperties ()
                            {
                                //
                                Context = arrayItem,
                                //
                                ObjectDict = Context.ObjectDict,
                            };
                            var arrayInstance = CodeTemplate.MarkItem.GetInstance ();
                            arrayInstance.Type = CodeTemplate.MarkItem.NodeTypeEnum.Content;
                            arrayInstance.AttributesDict = new Dictionary<CodeTemplate.MarkItem.NodeAttributeEnum, string> (item.AttributesDict);
                            arrayInstance.AttributesDict.Remove (CodeTemplate.MarkItem.NodeAttributeEnum.Context);
                            arrayInstance.ChildNodeList = item.ChildNodeList;
                            arrayInstance.ContentPartsList = item.ContentPartsList;
                            ParseMarkItem (codeGenerate, arrayInstance, arrayContext);
                        }
                    }
                    break;
                case CodeTemplate.MarkItem.NodeTypeEnum.Value:
                    //
                    //Searching for path from Context or Dict
                    {
                        string valueAppend = GetValueBasic (item, Context)?.ToString () ?? "";
                        //string newLine = item.ContentPartsList[i / 2];
                        valueAppend = valueAppend.Replace ("\n", "\n" + Context.LineSpace);
                        codeGenerate.Append (valueAppend);
                    }
                    break;
                case CodeTemplate.MarkItem.NodeTypeEnum.Resources:
                    //遍历内部的instances
                    foreach (var node in item.ChildNodeList)
                    {
                        switch (node.Type)
                        {
                            case CodeTemplate.MarkItem.NodeTypeEnum.Instance:
                                SetupInstance (node.AttributesDict, Context);
                                break;
                        }
                    }
                    break;
                case CodeTemplate.MarkItem.NodeTypeEnum.TemplateSelector:
                    //获取对应的template属性
                    {
                        object compareValue = GetValueBasic (item, Context);
                        //查找template中符合compareValue的类
                        foreach (var childNode in item.ChildNodeList)
                        {
                            object templateValue = GetValueBasic (childNode, Context);
                            if (compareValue.Equals (templateValue))
                            {
                                //successful searched, try to use it 

                                var templateInstance = CodeTemplate.MarkItem.GetInstance ();
                                // templateInstance.Type = childNode.Content;
                                templateInstance.AttributesDict = childNode.AttributesDict;
                                templateInstance.ChildNodeList = childNode.ChildNodeList;
                                templateInstance.ContentPartsList = childNode.ContentPartsList;
                                ParseMarkItem (codeGenerate, templateInstance, Context);
                            }
                        }
                    }
                    break;
            }
        }
        object GetValueBasic (CodeTemplate.MarkItem item, DataTreeProperties Context)
        {

            object providerObject = Context.Context;
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Name))
            {
                //redirect to new obj
                providerObject = Context.ObjectDict[item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Name]];
            }
            object valueReturn = null;
            //input args get
            object[] inputArgs = null;
            if (item.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgs)).Count () != 0)
            {
                //Need to get the value
                inputArgs = item.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgs)).FirstOrDefault ().ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgValue)).Select (subArg => GetValueBasic (subArg, Context)).ToArray ();
            }
            else if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs))
            {
                inputArgs = item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs].Split (",");
                for (int i = 0; i < inputArgs.Length; i++)
                {
                    if (inputArgs[i].Equals ("null"))
                    {
                        inputArgs[i] = null;
                    }

                }
            }
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Path))
            {
                providerObject = SearchingPropertyFromPath (providerObject, item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Path]);
            }
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Index))
            {
                string index = item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Index];
                object[] indexParams = null;

                var prop = providerObject
                    .GetType ()
                    .GetProperties ()
                    .Where (prop => prop.GetIndexParameters ().Count () > 0).First ();
                if (int.TryParse (index, out int indexNum))
                {
                    indexParams = new object[] { indexNum };
                }
                else
                {
                    indexParams = new object[] { index };
                }
                providerObject = prop.GetValue (providerObject, indexParams);
            }
            //check for function call or property call
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Function))
            {
                string function = item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Function];
                valueReturn = providerObject.GetType ().GetMethod (function).Invoke (providerObject, inputArgs);
            }
            else if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Value))
            {
                //直接赋值
                valueReturn = item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Value];

            }
            else if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Property))
            {
                string property = item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Property];
                valueReturn = (providerObject.GetType ().GetProperty (property).GetValue (providerObject));
            }
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Type))
            {
                switch (item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Type])
                {
                    case "float":
                        valueReturn = float.Parse (valueReturn.ToString ());
                        break;
                    case "bool":
                        valueReturn = bool.Parse (valueReturn.ToString ());
                        break;
                    case "int":
                        valueReturn = int.Parse (valueReturn.ToString ());
                        break;
                    case "null":
                        valueReturn = null;
                        break;
                }
            }
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Format))
            {
                object[] formatArgs = new object[] { valueReturn };
                if (item.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.FormatArgs)).Count () != 0)
                {
                    //Need to get the value
                    formatArgs = item.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.FormatArgs)).FirstOrDefault ().ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgValue)).Select (subArg => GetValueBasic (subArg, Context)).ToArray ();
                }
                valueReturn = string.Format (item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Format], formatArgs);
            }
            return valueReturn;
        }
        object SearchingPropertyFromPath (object input, string path)
        {
            var routes = path.Split ('.');
            object output = input;
            foreach (var route in routes)
            {
                output = output.GetType ().GetProperty (route).GetValue (output);
            }
            return output;
        }
        public void ReadBasicAttributes (Dictionary<CodeTemplate.MarkItem.NodeAttributeEnum, string> attributes, ref DataTreeProperties Context)
        {
            foreach (var attribute in attributes)
            {
                switch (attribute.Key)
                {
                    case CodeTemplate.MarkItem.NodeAttributeEnum.Context:
                        Context.Context = Context.ObjectDict[attribute.Value];
                        break;
                    case CodeTemplate.MarkItem.NodeAttributeEnum.LineSpace:
                        Context.LineSpace = attribute.Value;
                        break;
                }
            }
        }

        void SetupInstance (Dictionary<CodeTemplate.MarkItem.NodeAttributeEnum, string> attributes, DataTreeProperties Context)
        {
            List<FileInfo> fileList = new List<FileInfo> ();
            string path = attributes[CodeTemplate.MarkItem.NodeAttributeEnum.Path];
            if (!path.Contains (":"))
            {
                path = FileSysHelper.GetCurrentAppLocationPath () + "\\" + path;
            }
            //searching dir or file
            if (File.Exists (path))
            {
                fileList.Add (new FileInfo (path));
            }
            else if (Directory.Exists (path))
            {
                fileList.AddRange (new DirectoryInfo (path).GetFiles ());
            }
            foreach (var file in fileList.Where (ele => ele.Extension.ToLower ().Equals (".dll")))
            {
                Assembly dllFromPath = Assembly.LoadFile (file.FullName);
                foreach (var dllModule in dllFromPath.GetLoadedModules ())
                {
                    foreach (var typeDefinedInModule in dllModule.GetTypes ())
                    {
                        if (typeDefinedInModule.Name.Equals (attributes[CodeTemplate.MarkItem.NodeAttributeEnum.Type]))
                        {

                            if (typeDefinedInModule.IsClass)
                            {
                                object itemGet = null;
                                if (attributes.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Function))
                                {
                                    itemGet = typeDefinedInModule.GetMethod (attributes[CodeTemplate.MarkItem.NodeAttributeEnum.Function]).Invoke (null, attributes.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs) ? attributes[CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs].Split (",") : null);
                                }
                                else if (attributes.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Property))
                                {
                                    itemGet = typeDefinedInModule.GetProperty (attributes[CodeTemplate.MarkItem.NodeAttributeEnum.Property]).GetValue (null);
                                }
                                else
                                {
                                    itemGet = System.Activator
                                        .CreateInstance (typeDefinedInModule, attributes.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs) ? attributes[CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs].Split (",") : null);
                                }

                                Context.ObjectDict[attributes[CodeTemplate.MarkItem.NodeAttributeEnum.Name]] = itemGet;
                                return;
                            }
                        }
                    }
                }

            }
            throw new Exception ("not finding instances");
        }
        public DataTreeProperties RootDataProperties { get; set; }
    }
}