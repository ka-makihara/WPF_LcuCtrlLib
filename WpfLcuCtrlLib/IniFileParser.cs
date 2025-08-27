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
                return "";
            }
            return member.Member.Name;
        }
        public static string GetIniValue(string path, string section, string key)
        {
            StringBuilder sb = new(1024);
			_ = GetPrivateProfileString(section, key, "", sb, sb.Capacity, path);
            return sb.ToString();
        }

		/// <summary>
		///  コンストラクタ
		/// </summary>
		/// <param name="path">iniファイルのパス</param>
        public IniFileParser(string path)
        {
			iniContent = [];

			if(System.IO.File.Exists(path) == false)
			{
				return;
			}

            string[] lines = System.IO.File.ReadAllLines(path);
            string currentSection = "";

            foreach (string line in lines) {
                string lineTrimmed = line.Trim();

                if( string.IsNullOrEmpty(lineTrimmed) || lineTrimmed.StartsWith(';') || lineTrimmed.StartsWith('#') ) {
                    // 空行またはコメント行の場合はスキップ
                    continue;
                }

                if (line.StartsWith('[') && line.EndsWith(']')) {
                    currentSection = line.Substring(1, line.Length - 2);
                    iniContent[currentSection] = new Dictionary<string, string>();
                }
                else {
                    string[] keyValuePair = line.Split('=');
                    if (keyValuePair.Length == 2) {
                        if (keyValuePair[1].Contains(':') == false)
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

		/// <summary>
		///  iniFile のセクション、キー、値を設定します。
		/// </summary>
		/// <param name="section">セクション名</param>
		/// <param name="key">キー名</param>
		/// <param name="value">値</param>
		public void SetValue(string section, string key, string value)
		{
			if (iniContent.ContainsKey(section))
			{
				iniContent[section][key] = value;
			}
			else
			{
				iniContent[section] = new Dictionary<string, string> { { key, value } };
			}
		}

		/// <summary>
		///  iniFile のセクション、キーから値を取得します。
		/// </summary>
		/// <param name="section">セクション名</param>
		/// <param name="key">キー名</param>
		/// <returns>キーに設定された値</returns>
		public string GetValue(string section, string key)
        {
            if (iniContent.TryGetValue(section, out Dictionary<string, string>? value)) {
                if (value.ContainsKey(key)) {
                    return value[key];
                }
            }
            return "";
        }

		/// <summary>
		///  キーと値からセクション名を取得します。
		/// </summary>
		/// <param name="key">キー名</param>
		/// <param name="value">値</param>
		/// <returns>セクション名のリスト</returns>
		public List<string> GetSectionByValue(string key, string value)
		{
			// key, value に一致するセクションが複数ある場合を考慮して List<string> を返すように変更
			//  (例: Copla_FW_FPGA, Copla_FW_FPGA_VER3 => Path=が同値)

			return [.. iniContent
				.Where(section => section.Value.TryGetValue(key, out var v) && v.Equals(value, StringComparison.OrdinalIgnoreCase))
				.Select(section => section.Key)];
		}

		/// <summary>
		///  iniFile のセクション数を取得します。
		/// </summary>
		/// <returns></returns>
		public List<string> SectionCount() => [.. iniContent.Keys];

		/// <summary>
		///  キー値に Path, FuserPath を持つセクション名、Path、FuserPath のリストを取得します。
		/// </summary>
		/// <returns></returns>
		public List<(string name, string path, string fuserPath)> GetPathList()
		{
			return [.. iniContent
				.Select(section => (
					name: section.Key,
					path: section.Value.TryGetValue("Path", out var p) ? p : "",
					fuserPath: section.Value.TryGetValue("FuserPath", out var f) ? f : ""
				))];
		}

		/// <summary>
		///  Path または FuserPath に指定されたパスを含むセクション名を取得します。
		/// </summary>
		/// <param name="path">検索したいパス</param>
		/// <returns>セクション名またはnull</returns>
		public string? GetSectionContainsPath(string path)
		{
			List<string> sec = SectionCount();
			foreach (string s in sec)
			{
				string? pathValue = GetValue(s, "Path").Split("Peripheral")[1];

				if ( pathValue.Equals(path, StringComparison.OrdinalIgnoreCase))
				{
					return s;
				}
				string? fuser = GetValue(s, "FuserPath");
				if ( fuser != "" )
				{
					string? pathValue2 = fuser.Split("Peripheral")[1];
					if (pathValue2.Equals(path, StringComparison.OrdinalIgnoreCase))
					{
						return s;
					}
				}
			}
			return null;
		}
    }
}
