using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace WrapperGenerator
{
    class Program
    {
        static List<string> GlobalSystemFunction = new List<string>()
        {
            "FMOD_Memory_Initialize",
            "FMOD_Memory_GetStats",
            "FMOD_Debug_Initialize",
            "FMOD_File_SetDiskBusy",
            "FMOD_File_GetDiskBusy",
            "FMOD_Thread_SetAttributes",
        };
        static Dictionary<string,string> ParamTypeConvertion = new Dictionary<string,string>()
        {
            {"FMOD_SYSTEM","FMOD::System" },
            {"FMOD_SOUND", "FMOD::Sound" },
            {"FMOD_CHANNELCONTROL", "FMOD::ChannelControl" },
            {"FMOD_CHANNEL", "FMOD::Channel" },
            {"FMOD_CHANNELGROUP", "FMOD::ChannelGroup" },
            {"FMOD_SOUNDGROUP","FMOD::SoundGroup" },
            {"FMOD_REVERB3D", "FMOD::Reverb3D" },
            {"FMOD_DSP", "FMOD::DSP" },
            {"FMOD_DSPCONNECTION","FMOD::DSPConnection" },
            {"FMOD_POLYGON", "FMOD::Polygon" },
            {"FMOD_GEOMETRY", "FMOD::Geometry" },
            //{"FMOD_SYNCPOINT", "FMOD::SyncPoint" },
            {"FMOD_ASYNCREADINFO","FMOD::AsyncReadInfo" },
            {"FMOD_BOOL", "bool" }
        };
        static void Main(string[] args)
        {
            System.Console.Out.WriteLine("start...");
            var srcHeadPath = args.Length > 0 ? args[0] : string.Empty ;
            if (srcHeadPath == string.Empty)
            {
                srcHeadPath = "F:\\Projects\\FmodH\\app\\src\\main\\cpp\\fmodInc\\fmod.h";
            }

            StringBuilder strb = new StringBuilder();
            strb.AppendLine("#include \"fmodInc/fmod.h\"");
            strb.AppendLine("#include \"fmodInc/fmod.hpp\"");
            var headSrc = System.IO.File.ReadAllText(srcHeadPath);
            var regexFunc = new System.Text.RegularExpressions.Regex("FMOD_RESULT F_API FMOD_\\w+");
            var regexParamType = new System.Text.RegularExpressions.Regex("\\(*\\,*(unsigned\\s)*(const\\s)*(long\\s)*\\w+\\s");
            var regexParamName = new System.Text.RegularExpressions.Regex("\\**\\w+\\,*");
            var regexParam = new System.Text.RegularExpressions.Regex("\\((\\s*\\w+\\s\\**\\w+\\,*)*\\)\\;*");
            var regexPtr = new System.Text.RegularExpressions.Regex("\\*");
            var matchFunc = regexFunc.Match(headSrc);
            List<string> paramTypeStack = new List<string>();
            List<string> paramNameStack = new List<string>();
            while (matchFunc.Success)
            {
                strb.Append(matchFunc.Value);
                var funcNameStart = matchFunc.Value.IndexOf("FMOD_RESULT F_API ") + "FMOD_RESULT F_API ".Length;
                var funcNameC = matchFunc.Value.Substring(funcNameStart, matchFunc.Value.Length - funcNameStart);
                var funcNameSplit =funcNameC.Split('_');
                var namespaceName = funcNameSplit[0];
                var moduleName = funcNameSplit[1];
                var funcName = funcNameSplit[2];
                bool isGlobalFunc = GlobalSystemFunction.Contains(funcNameC);

                int start = matchFunc.Index + matchFunc.Length;
                int end = Math.Min(500, headSrc.Length - start);
                var matchParam = regexParam.Match(headSrc, start, end);
                if (matchParam.Success)
                {
                    paramTypeStack.Clear();
                    paramNameStack.Clear();
                    int paramIndex = 0;
                    strb.Append(matchParam.Value.TrimEnd(';'));
                    strb.Append("\r{\r");
                    //Console.WriteLine(matchParam.Value);
                    int matchTypeStart = Math.Min(matchParam.Index + matchParam.Length, matchParam.Index + matchParam.Length);
                    int matchTypeEnd = Math.Min(Math.Max(0, matchParam.Index + matchParam.Length - matchTypeStart), headSrc.Length - matchTypeStart);
                    var matchType = regexParamType.Match(headSrc, start, end);
                    System.Text.RegularExpressions.Match matchName = null;

                    bool isPtr = false;
                    System.Text.RegularExpressions.MatchCollection ptrCount = null;
                    while (matchType.Success)
                    {
                        //Console.Out.WriteLine(matchType.Value);
                        int matchNameStart = Math.Min(matchParam.Index + matchParam.Length, matchType.Index + matchType.Length);
                        int matchNameEnd = Math.Min(Math.Max(0, matchParam.Index + matchParam.Length - matchNameStart), headSrc.Length - matchNameStart);
                        matchName = regexParamName.Match(headSrc, matchNameStart, matchNameEnd);
                        if (matchName.Success)
                        {
                            Console.Out.WriteLine(matchName.Value);

                            if (paramIndex == 0 || matchType.Value.Contains("("))
                            {
                                isPtr = matchName.Value.Contains("*");
                                ptrCount = regexPtr.Matches(matchName.Value);
                            }
                            paramTypeStack.Add(matchType.Value.Trim(',', '(', ')'));
                            paramNameStack.Add(matchName.Value.Trim(',', ' ', ')'));
                            matchTypeStart = Math.Min(matchParam.Index + matchParam.Length, matchName.Index + matchName.Length);
                            matchTypeEnd = Math.Min(Math.Max(0, matchParam.Index + matchParam.Length - matchTypeStart), headSrc.Length - matchTypeStart);
                            matchType = regexParamType.Match(headSrc, matchTypeStart, matchTypeEnd);
                        }
                        else
                        {
                            break;
                        }
                        paramIndex++;
                    }


                    if (isGlobalFunc || ptrCount.Count == 2)
                    {
                        strb.Append("   return " + namespaceName + "::" + moduleName + "_" + funcName + "(");// + matchName.Value.TrimStart('(').TrimEnd(' ', ',').TrimStart('*') + ");");
                        for(int i=0;i<paramNameStack.Count;i++)
                        {
                            strb.Append(paramNameStack[i].Trim('(',' ' ,',','*'));
                            if (i != paramNameStack.Count - 1)
                            {
                                strb.Append(", ");
                            }
                        }
                        strb.AppendLine(");");
                    }
                    else if (ptrCount.Count == 1)
                    {
                        //strb.AppendLine("   return FMOD_OK;");
                        string funcNameLow = funcName;
                        string firstChar = string.Join("", funcNameLow[0]).ToLower();
                        funcNameLow = funcNameLow.TrimStart(funcNameLow[0]);
                        funcNameLow = funcNameLow.Insert(0, firstChar);
                        strb.Append("   return " + "((" + "FMOD::" + moduleName + "*)" + paramNameStack[0].TrimStart('(').TrimEnd(' ', ',').TrimStart('*') + ")" + (isPtr ? "->" : ".") + funcNameLow + "(");
                        for (int i = 1; i < paramNameStack.Count; i++)
                        {
                            var matchPtrCount = regexPtr.Matches(paramNameStack[i]);
                            var paramTypeStr = paramTypeStack[i].Trim('(', ' ', ',', '*');
                            var paramNameStr = paramNameStack[i].Trim('(', ' ', ',', '*');
                            if (ParamTypeConvertion.ContainsKey(paramTypeStr))
                            {
                                strb.Append("(" + ParamTypeConvertion[paramTypeStr]);
                                for (int ptrIdx = 0; ptrIdx < matchPtrCount.Count; ptrIdx++)
                                {
                                    strb.Append("*");
                                }
                                strb.Append(")");
                                strb.Append(paramNameStr);
                            }
                            else
                            {
                                strb.Append(paramNameStr);
                            }
                            if (i != paramNameStack.Count - 1)
                            {
                                strb.Append(", ");
                            }
                        }
                        strb.AppendLine(");");
                    }
                    


                }
                else
                {
                    Console.Out.Write("$$ matchParam failed..");
                }
                strb.Append("\r\r");
                strb.Append("}\r");
                matchFunc = matchFunc.NextMatch();
            }
            System.IO.File.WriteAllText(System.IO.Path.GetDirectoryName(srcHeadPath).Replace("fmodInc", "") + "/fmod.cpp", strb.ToString());

        }
    }
}
