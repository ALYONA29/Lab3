using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ClassLibrary2
{
    public class ParserXml
    {
        public static class Parser
        {
            static List<string> ParseXml(string xml, bool trim)
            {
                xml = xml.Trim(new char[] { '\n', '\t', '\r', ' ' });
                string name;
                Match match;
                name = Next(xml, 0);
                if (trim)
                {
                    Regex clean = new Regex($"^<{name}>(.*)</{name}>$", RegexOptions.Singleline);
                    match = clean.Match(xml);
                    if (match.Success)
                    {
                        xml = match.Groups[1].Value;
                    }
                }
                List<string> objects = new List<string>();
                bool isTag = false;
                StringBuilder obj = new StringBuilder();
                string tag = "";
                string value = "";
                int count = 0;
                foreach (char ch in xml)
                {
                    if (ch != '\t' && ch != '\r' && ch != '\n')
                    {
                        if (ch == '<')
                        {
                            isTag = true;
                            count++;
                            continue;
                        }
                        if (ch == '>')
                        {
                            isTag = false;
                            if (count == 2)
                            {
                                tag = tag.Remove(tag.IndexOf('/'), tag.Length - tag.IndexOf('/'));
                                obj.Append(tag + ':' + value);
                                count = 0;
                                tag = "";
                                value = "";
                                objects.Add(obj.ToString());
                                obj.Clear();
                            }
                            continue;
                        }
                        if (isTag)
                        {
                            tag += ch;
                        }
                        else
                        {
                            value += ch;
                        }
                    }
                }
                return objects;
            }

            public static T DeserializeXml<T>(string xml) where T : new()
            {
                List<string> objects = ParseXml(xml, true);
                T ans = new T();
                Type type = typeof(T);
                Regex complexType = new Regex(@"(\w+)\s*:\s*{(.*)}", RegexOptions.Singleline);
                Regex simpleType = new Regex(@"(\w+)\s*:\s*(.*)", RegexOptions.Singleline);
                Match match;
                foreach (string obj in objects)
                {
                    string index = "";
                    string value = "";
                    if (complexType.IsMatch(obj))
                    {
                        match = complexType.Match(obj);
                        index = match.Groups[1].Value;
                        value = match.Groups[2].Value;
                        FieldInfo info = type.GetField(index);
                        info.SetValue(ans, typeof(ParserXml).GetMethod("DeserializeXml")
                            .MakeGenericMethod(new Type[] { info.FieldType })
                            .Invoke(null, new object[] { value }));
                    }
                    else if (simpleType.IsMatch(obj))
                    {
                        match = simpleType.Match(obj);
                        index = match.Groups[1].Value;
                        value = match.Groups[2].Value;
                        FieldInfo info = type.GetField(index);
                        info.SetValue(ans, Convert.ChangeType(value, info.FieldType));
                    }
                }
                return ans;
            }
                      
            static string Next(string xml, int startPosition)
            {
                StringBuilder tag = new StringBuilder("");
                bool isTag = false;
                for (int i = startPosition; i < xml.Length; i++)
                {
                    if (xml[i] == '<')
                    {
                        isTag = true;
                        continue;
                    }
                    else if (xml[i] == '>')
                    {
                        return tag.ToString();
                    }
                    else if (isTag)
                    {
                        tag.Append(xml[i]);
                    }
                }
                throw new Exception("Cant't find tag");
            }
        }
    }
}
