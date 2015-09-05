using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.Data;
using System.Text.RegularExpressions;

namespace Phaeton
{
    public class RegexHelper
    {
        /// <summary>
        /// 普通模式匹配，返回匹配成功的字符串集合
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Pattern"></param>
        /// <returns></returns>
        public static List<string> MultiNormalPatternMatch(string Source, string Pattern)
        {
            // 根据模式串匹配，返回多个匹配串
            List<string> matchResult = new List<string>();
            if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Pattern)) return matchResult;

            MatchCollection match = Regex.Matches(Source, Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            for (int i = 0; i < match.Count; i++)
            {
                if (!string.IsNullOrEmpty(match[i].ToString().Trim()))
                    matchResult.Add(match[i].ToString().Trim());
            }

            return matchResult;
        }

        /// <summary>
        /// 有捕获组的匹配，返回通过首个捕获组获得的字符串集合
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Pattern"></param>
        /// <returns></returns>
        public static List<string> MultiSpecialPatternMatch(string Source, string Pattern)
        {
            List<string> matchResult = new List<string>();
            if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Pattern)) return matchResult;

            // 根据模式串匹配，返回多个匹配串
            MatchCollection match = Regex.Matches(Source, Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            for (int i = 0; i < match.Count; i++)
            {
                if (match[i].Groups.Count > 1 && !string.IsNullOrEmpty(match[i].Groups[1].Value.Trim()))
                    matchResult.Add(match[i].Groups[1].Value.Trim());
            }
            return matchResult;
        }

        /// <summary>
        /// 普通模式匹配，返回匹配成功的首个字符串
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Pattern"></param>
        /// <returns></returns>
        public static string NormalPatternMatch(string Source, string Pattern)
        {
            if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Pattern)) return string.Empty;

            Match match = Regex.Match(Source, Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            return match.Success ? match.ToString() : string.Empty;
        }

        /// <summary>
        /// 有捕获组的匹配，返回通过首个捕获组获得的首个字符串
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Pattern"></param>
        /// <returns></returns>
        public static string SpecialPatternMatch(string Source, string Pattern)
        {
            if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Pattern)) return string.Empty;

            Match match = Regex.Match(Source, Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            return match.Groups.Count > 1 ? match.Groups[1].Value : string.Empty;
        }

        /// <summary>
        /// 有捕获组的匹配，返回所有捕获组获得的字符串集合
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Pattern"></param>
        /// <returns></returns>
        public static List<GroupCollection> MultiGroupPatternMatch(string Source, string Pattern)
        {
            List<GroupCollection> matchResult = new List<GroupCollection>();
            if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Pattern)) return matchResult;

            // 根据模式串匹配，返回多个匹配串
            MatchCollection match = Regex.Matches(Source, Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            for (int i = 0; i < match.Count; i++)
            {
                matchResult.Add(match[i].Groups);   
            }
            return matchResult;
        }
    }
}
