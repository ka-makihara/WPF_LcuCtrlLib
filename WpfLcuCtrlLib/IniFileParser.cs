using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfLcuCtrlLib
{
    public class IniFileParser
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            private static extern int GetPrivateProfileString(
            string lpApplicationName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedstring,
            int nSize,
            string lpFileName);

        private Dictionary<string, Dictionary<string, string>> iniContent;

        public static string GetName<T>(Expression<Func<T>> e)
        {
            var member = e.Body as MemberExpression;
            if (member == null) {
                //throw new ArgumentException();
                return "";
            }
            return member.Member.Name;
        }
        public static string GetIniValue(string path, string section, string key)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetPrivateProfileString(section, key, "", sb, sb.Capacity, path);
            return sb.ToString();
        }
        public IniFileParser(string path)
        {
			if(System.IO.File.Exists(path) == false)
			{
				return;
			}

			iniContent = new Dictionary<string, Dictionary<string, string>>();
            string[] lines = System.IO.File.ReadAllLines(path);
            string currentSection = "";

            foreach (string line in lines) {
                string lineTrimmed = line.Trim();

                if( string.IsNullOrEmpty(lineTrimmed) || lineTrimmed.StartsWith(";") || lineTrimmed.StartsWith("#") ) {
                    // 空行またはコメント行の場合はスキップ
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]")) {
                    currentSection = line.Substring(1, line.Length - 2);
                    iniContent[currentSection] = new Dictionary<string, string>();
                }
                else {
                    string[] keyValuePair = line.Split('=');
                    if (keyValuePair.Length == 2) {
                        if (keyValuePair[1].Contains(":") == false)
                        {
                            iniContent[currentSection][keyValuePair[0]] = keyValuePair[1];
                        }
                        else
                        {
                            iniContent[currentSection][keyValuePair[0]] = keyValuePair[1].Split(":")[1];
                        }
                    }
                }
            }
        }
        public string GetValue(string section, string key)
        {
            if (iniContent.TryGetValue(section, out Dictionary<string, string>? value)) {
                if (value.ContainsKey(key)) {
                    return value[key];
                }
            }
            return "";
        }
        public List<string> SectionCount() => [.. iniContent.Keys];
    }
    /*
    public class UpdateInfo
    {
        public string? Name { get; set; }
        public string? Attribute { get; set; }
        public string? Version { get; set; }
        public string? Path { get; set; }
    }
    */
}
