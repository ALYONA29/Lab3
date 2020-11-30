using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ClassLibrary2
{
    public class ParserJson
    {
        public static class Parser
        {
            public static List<string> ParseJson(string json)
            {
                json = json.Trim(new char[] { ' ', '{', '}' });
                List<string> objects = new List<string>();
                int count = 0;
                StringBuilder obj = new StringBuilder();
                foreach (char ch in json)
                {
                    if (char.IsLetterOrDigit(ch) || char.IsPunctuation(ch))
                    {
                        if (ch == ',' && count == 0)
                        {
                            objects.Add(obj.ToString());
                            obj.Clear();
                        }
                        else if (ch == '{')
                        {
                            obj.Append(ch);
                            count++;
                        }
                        else if (ch == '}')
                        {
                            obj.Append(ch);
                            count--;
                        }
                        else
                        {
                            obj.Append(ch);
                        }
                    }
                }
                if (obj.Length != 0)
                {
                    objects.Add(obj.ToString());
                }
                return objects;
            }
            public static T DeserializeJson<T>(string json) where T : new()
            {
                List<string> objects = ParseJson(json);
                T ans = new T();
                Type type = typeof(T);
                Regex complexType = new Regex(@"(\w+)\s*:\s*{(.*)}", RegexOptions.Singleline);
                Regex simpleType = new Regex(@"(\w+)\s*:\s*(.*)", RegexOptions.Singleline);
                Match match;
                foreach (string obj in objects)
                {
                    string index = "";
                    string value = "";
                    if (simpleType.IsMatch(obj))
                    {
                        match = simpleType.Match(obj);
                        index = match.Groups[1].Value;
                        value = match.Groups[2].Value;
                        FieldInfo info = type.GetField(index);
                        info.SetValue(ans, Convert.ChangeType(value, info.FieldType));
                    }
                    else if (complexType.IsMatch(obj))
                    {
                        match = complexType.Match(obj);
                        index = match.Groups[1].Value;
                        value = match.Groups[2].Value;
                        FieldInfo info = type.GetField(index);
                        info.SetValue(ans, typeof(ParserJson).GetMethod("DeserializeJson")
                            .MakeGenericMethod(new Type[] { info.FieldType })
                            .Invoke(null, new object[] { value }));
                    }
                }
                return ans;
            }
        }
    }
}
