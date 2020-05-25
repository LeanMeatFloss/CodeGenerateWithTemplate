# CodeGenerateWithTemplate

A C# based code generator with template input.
This project is for using C# net core runtime to export code files from template.
In some scenario, some code is boring when you are trying to copy those code several times only with different times. And when you are trying to fix bugs in the code, those duplicate codes also need to be revised.
e.g.
In some embedded software, you need to deal with CAN signal, and transfer is to another component:

```C
Uint32 RawSignal=ReadSignalFromName("Signal1");
float32 TransferSignal=RawSignal*0.1+1000;
TransferSignalToComponent(TransferSignal);

```

Maybe there is more than 40 signals need to be dealt, and those codes may need to copy 40 times. And once there is a bug or revise need to be fixed, the code need to deal 40 times either. This program is used to help this scenario with net core like xaml and mvvm.

```xml
<$Content$>
    <$Resources$>
        <$Instance Name="DBCNodes" Type="DBCFileParser" Function="ParserNodesFromFile" InputArgs="*.dbc"  Path="*" /$$$>
    <$/Resources$>
Uint32 RawSignal;
float32 TransferSignal;
    <$Array DataContext="DBCNodes" Index="ECU" Property="TransferSignals" $>
RawSignal=ReadSignalFromName("<$Value Property="SignalName"/$>");
TransferSignal=RawSignal*<$Value Property="Scale"/$>+<$Value Property="Offset"/$>;
TransferSignalToComponent(TransferSignal);
    <$/Array$>
<$/Content$>
```

In the example upper, this project will use a dll in path and call the function with input args to get the return object as "DBCNodes". The signals can be located in the index DBCNodes["ECU"].TransferSignals. TransferSignals is an  IENUMERABLE object, the <$Array> is used to expand the object using IENUMERABLE interface. Any way, those following code is the project supposed to do:

```csharp

var DBCNodes=DBCFileParser.ParserNodesFromFile("*");
StringBuilder content=new StringBuilder();
content.Append("Uint32 RawSignal;\nfloat32 TransferSignal;“);
foreach(var signal in DBCNodes["ECU"].TransferSignals)
{
    content.AppendFormat("RawSignal=ReadSignalFromName(\"{0}\");\nTransferSignal=RawSignal*{1}+{2};\nTransferSignalToComponent(TransferSignal);",signal.SignalName,signal.Scale,signal.Offset);
}
return content.ToString();
```



## Descriptions

Try to use $$$<MARK>$$$ to bracket the content. Like xaml files.
e.g.

$$$CONTENT$$$
......
$$$/CONTENT$$$ 

$$$VALUE/$$$