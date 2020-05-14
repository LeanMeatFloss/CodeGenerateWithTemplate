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
            public int Index { get; set; }
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
                                if (item.ContentPartsList[i / 2].Split (new string[] { "\r", "\n", " " }, StringSplitOptions.RemoveEmptyEntries).Length == 0)
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

                        context = GetValueBasic (item, Context);
                        if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.DataSource))
                        {

                            //Searching for the path of context
                            context = SearchingPropertyFromPath (context, item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.DataSource]);
                        }
                        int i = 0;
                        if (context is int)
                        {

                        }
                        foreach (var arrayItem in ((IEnumerable) (context)))
                        {
                            DataTreeProperties arrayContext = new DataTreeProperties ()
                            {
                                //
                                Context = arrayItem,
                                //
                                ObjectDict = Context.ObjectDict,
                                Index = i,
                            };
                            var arrayInstance = CodeTemplate.MarkItem.GetInstance ();
                            arrayInstance.Type = CodeTemplate.MarkItem.NodeTypeEnum.Content;
                            arrayInstance.AttributesDict = new Dictionary<CodeTemplate.MarkItem.NodeAttributeEnum, string> (item.AttributesDict);
                            arrayInstance.AttributesDict.Remove (CodeTemplate.MarkItem.NodeAttributeEnum.Context);
                            arrayInstance.ChildNodeList = item.ChildNodeList;
                            arrayInstance.ContentPartsList = item.ContentPartsList;
                            arrayContext.ObjectDict["Parent"] = Context.Context;
                            ParseMarkItem (codeGenerate, arrayInstance, arrayContext);
                            arrayContext.ObjectDict.Remove ("Parent");
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
                                SetupInstance (node, Context);
                                break;
                            case CodeTemplate.MarkItem.NodeTypeEnum.Template:
                                Context.ObjectDict[node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Name]] = node;
                                break;
                            case CodeTemplate.MarkItem.NodeTypeEnum.Value:
                                Context.ObjectDict[node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Name]] = GetValueBasic (node, Context);
                                break;
                        }
                    }
                    break;
                case CodeTemplate.MarkItem.NodeTypeEnum.TemplateSelector:
                    //获取对应的template属性
                    {
                        object compareValue = GetValueBasic (item, Context);
                        if (compareValue == null)
                        {
                            //there is no template
                            break;
                        }
                        CodeTemplate.MarkItem defaultTemplate = null;
                        bool getTemplate = false;
                        //查找template中符合compareValue的类
                        foreach (var childNode in item.ChildNodeList.Where (ele => ele.Type == CodeTemplate.MarkItem.NodeTypeEnum.Template))
                        {
                            object templateValue = GetValueBasic (childNode, Context);
                            if (templateValue != null && templateValue.Equals ("default"))
                            {
                                //default template 
                                defaultTemplate = childNode;
                            }

                            if (compareValue.Equals (templateValue))
                            {
                                //successful searched, try to use it 
                                getTemplate = true;
                                var templateInstance = CodeTemplate.MarkItem.GetInstance ();
                                // templateInstance.Type = childNode.Content;
                                templateInstance.AttributesDict = childNode.AttributesDict;
                                templateInstance.ChildNodeList = childNode.ChildNodeList;
                                templateInstance.ContentPartsList = childNode.ContentPartsList;
                                ParseMarkItem (codeGenerate, templateInstance, Context);
                            }
                        }
                        if (defaultTemplate != null && !getTemplate)
                        {
                            var templateInstance = CodeTemplate.MarkItem.GetInstance ();
                            // templateInstance.Type = childNode.Content;
                            templateInstance.AttributesDict = defaultTemplate.AttributesDict;
                            templateInstance.ChildNodeList = defaultTemplate.ChildNodeList;
                            templateInstance.ContentPartsList = defaultTemplate.ContentPartsList;
                            ParseMarkItem (codeGenerate, templateInstance, Context);
                        }
                    }
                    break;
                case CodeTemplate.MarkItem.NodeTypeEnum.Template:
                    //refrence to the template name
                    {
                        var markItem = Context.ObjectDict[item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Name]] as CodeTemplate.MarkItem;
                        markItem.Type = CodeTemplate.MarkItem.NodeTypeEnum.Content;
                        if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Context))
                        {
                            Context.Context = Context.ObjectDict[item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Context]];
                        }
                        ParseMarkItem (codeGenerate, markItem, Context);
                    }
                    break;
            }
        }
        object GetValueBasic (CodeTemplate.MarkItem item, DataTreeProperties Context)
        {
            //path->index->property/function/
            object providerObject = Context.Context;
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Name))
            {
                //redirect to new obj
                providerObject = Context.ObjectDict[item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Name]];
            }
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Path))
            {
                providerObject = SearchingPropertyFromPath (providerObject, item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Path]);
            }

            object valueReturn = providerObject;
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

            //index deal
            {
                List<object[]> indexArgsList = new List<object[]> ();

                if (item.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.IndexArgs)).Count () != 0)
                {
                    foreach (var indexArgs in item.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.IndexArgs)))
                    {
                        var argArray = indexArgs.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgValue)).Select (subArg => GetValueBasic (subArg, Context)).ToArray ();
                        indexArgsList.Add (argArray);
                    }

                }
                if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Index))
                {
                    string index = item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Index];
                    string[] indexArgsString = index.Split (",", StringSplitOptions.RemoveEmptyEntries);
                    indexArgsList.Add (indexArgsString.Select (new Func<string, object> (ele =>
                    {
                        if (int.TryParse (ele, out int indexNum))
                        {
                            return indexNum;
                        }
                        else if (ele.Equals ("null"))
                        {
                            return null;
                        }
                        else
                        {
                            return ele;
                        }
                    })).ToArray ());
                }
                if (indexArgsList.Count != 0)
                {
                    foreach (var indexArgs in indexArgsList)
                    {
                        var prop = providerObject
                            .GetType ()
                            .GetProperties ()
                            .Where (prop => prop.GetIndexParameters ().Length > 0)
                            .Where (prop =>
                            {
                                int i = 0;
                                foreach (var para in prop.GetIndexParameters ())
                                {
                                    if (i < indexArgs.Length)
                                    {
                                        if (indexArgs[i].GetType ().Equals (para.ParameterType))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                return true;
                            })
                            .First ();

                        providerObject = prop.GetValue (providerObject, indexArgs);
                        if (providerObject == null)
                        {
                            return null;
                        }
                    }
                    valueReturn = providerObject;

                }

            }

            //check for function call or property call
            if (item.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Function))
            {
                string function = item.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Function];
                var type = providerObject.GetType ();
                if (providerObject is Delegate del)
                {
                    valueReturn = del.DynamicInvoke (new object[] { inputArgs });
                }
                else
                {
                    if (inputArgs.Contains (null))
                    {
                        valueReturn = providerObject.GetType ().GetMethod (function).Invoke (providerObject, inputArgs);
                    }
                    else
                    {
                        var temp = providerObject.GetType ();
                        var method = providerObject.GetType ().GetMethod (function, inputArgs.Select (ele => ele.GetType ()).ToArray ());
                        valueReturn = providerObject.GetType ().GetMethod (function, inputArgs.Select (ele => ele.GetType ()).ToArray ()).Invoke (providerObject, inputArgs);
                    }
                }

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

        void SetupInstance (CodeTemplate.MarkItem Node, DataTreeProperties Context)
        {
            object[] inputArgs = null;
            Assembly assemblyResolver (object? sender, ResolveEventArgs args)
            {
                string path = Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Path];
                string fileToLoad = args.Name.Remove (args.Name.IndexOf (','));
                if (path != null)
                {
                    string resolvePath = FileSysHelper.GetCurrentAppLocationPath () + "\\" + path + "\\" + fileToLoad + ".dll";
                    if (File.Exists (resolvePath))
                    {
                        return Assembly.LoadFile (resolvePath);
                    }
                }

                return Assembly.Load (fileToLoad);

            }
            AppDomain.CurrentDomain.AssemblyResolve += assemblyResolver;
            if (Node.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgs)).Count () != 0)
            {
                //Need to get the value
                inputArgs = Node.ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgs)).FirstOrDefault ().ChildNodeList.Where (ele => ele.Type.Equals (CodeTemplate.MarkItem.NodeTypeEnum.InputArgValue)).Select (subArg => GetValueBasic (subArg, Context)).ToArray ();
            }
            else if (Node.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs))
            {
                inputArgs = Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.InputArgs].Split (",");
                for (int i = 0; i < inputArgs.Length; i++)
                {
                    if (inputArgs[i].Equals ("null"))
                    {
                        inputArgs[i] = null;
                    }

                }
            }
            Assembly dllToLoad = null;
            if (Node.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Load))
            {
                dllToLoad = Assembly.Load (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Load]);
            }
            else
            {
                List<FileInfo> fileList = new List<FileInfo> ();
                string path = Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Path];
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
                            if (typeDefinedInModule.Name.Equals (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Type]))
                            {
                                dllToLoad = dllFromPath;
                                break;

                            }
                        }
                        if (dllToLoad != null)
                        {
                            break;
                        }
                    }
                    if (dllToLoad != null)
                    {
                        break;
                    }
                }

            }
            if (dllToLoad == null)
            {
                throw new Exception ("not finding instances");
            }
            foreach (var typeDefinedInModule in dllToLoad.GetTypes ())
            {
                if (typeDefinedInModule.Name.Equals (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Type]))
                {

                    if (typeDefinedInModule.IsClass)
                    {
                        object itemGet = null;
                        if (Node.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Function))
                        {
                            itemGet = typeDefinedInModule.GetMethod (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Function]).Invoke (null, inputArgs);
                        }
                        else if (Node.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Property))
                        {
                            itemGet = typeDefinedInModule.GetProperty (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Property]).GetValue (null);
                        }
                        else if (Node.AttributesDict.ContainsKey (CodeTemplate.MarkItem.NodeAttributeEnum.Method))
                        {
                            // if (inputArgs != null)
                            // {
                            //     var method = typeDefinedInModule.GetMethod (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Method]);
                            // }
                            // else
                            // {
                            //     var method = typeDefinedInModule.GetMethod (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Method]);
                            // }

                            var func = new Func<object[], object> ((object[] input) =>
                                {

                                    foreach (var method in typeDefinedInModule.GetMethods ())
                                    {
                                        bool isCorrect = false;
                                        if (method.Name.Equals (Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Method]))
                                        {
                                            int i = 0;
                                            isCorrect = true;
                                            var parameters = method.GetParameters ();
                                            if (parameters.Length != input.Length)
                                            {
                                                isCorrect = false;
                                            }
                                            else
                                            {
                                                foreach (var parameter in parameters)
                                                {
                                                    if (input[i] != null)
                                                    {
                                                        if (input[i].GetType ().Equals (parameter.ParameterType))
                                                        {

                                                        }
                                                        else
                                                        {
                                                            isCorrect = false;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {

                                                    }
                                                }
                                            }

                                        }
                                        if (isCorrect)
                                        {
                                            return method.Invoke (null, input);
                                        }
                                    }

                                    throw new Exception ("not finding instances");
                                });

                            itemGet = func;

                        }
                        else
                        {
                            itemGet = System.Activator
                                .CreateInstance (typeDefinedInModule, inputArgs);
                        }

                        Context.ObjectDict[Node.AttributesDict[CodeTemplate.MarkItem.NodeAttributeEnum.Name]] = itemGet;
                        AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolver;
                        return;
                    }
                }
            }
            throw new Exception ("not finding instances");
        }
        public DataTreeProperties RootDataProperties { get; set; }
    }
}