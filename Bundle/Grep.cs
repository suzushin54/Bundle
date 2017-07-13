using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bundle
{
    class Grep
    {
        public string Extension { get; set; }
        public string ProgFileEncode { get; set; }
        public bool IgnoreBinary { get; set; }
        public string OutputDir { get; set; }
        public string RegexOptions { get; set; }

        public Grep()
        {
            Extension = ConfigurationManager.AppSettings["targetExtension"];
            ProgFileEncode = ConfigurationManager.AppSettings["programFileEncode"];
            IgnoreBinary = Boolean.Parse(ConfigurationManager.AppSettings["ignoreBinary"]);
            OutputDir = ConfigurationManager.AppSettings["outputDir"];
            RegexOptions = ConfigurationManager.AppSettings["regexOptions"];
        }

        /// <summary>
        /// 検索を実行します。
        /// </summary>
        /// <param name="word">検索キーワード</param> 
        /// <param name="fileList">検索対象ファイルリスト</param>
        /// <param name="subDir">AppConfigから読み込んだKeywordListの種類（Java等）</param>
        public bool doGrep(string word, string[] fileList, string subDir)
        {
            // ここで使用しているRegexOptionsはAppConfigに設定され、Programクラスで読み込んでいます
            var reg = new Regex(word, (RegexOptions)Enum.Parse(typeof(RegexOptions), RegexOptions));
            var sb = new StringBuilder();

            try
            {
                foreach (string f in fileList)
                {
                    // 設定のignoreBinaryがTrueなら、バイナリファイルは検索しない
                    if (IgnoreBinary && IsBinaryFile(f))
                            continue;

                    // すべての行を読み込んで、マッチする行を選択する
                    var result = File.ReadAllLines(@f, Encoding.GetEncoding(ProgFileEncode))
                               .Select((s, i) => new { Index = i, Value = s })
                               .Where(s => reg.IsMatch(s.Value));

                    // マッチした行だけをStringBuilderで結合していく
                    foreach (var r in result)
                        sb.AppendLine(f + " | " + (r.Index + 1) + "：" + r.Value);
                }

            }
            catch (DirectoryNotFoundException e)
            {
                throw new DirectoryNotFoundException ("ディレクトリが見つかりません。", e);
            }
            catch (System.Exception e)
            {
                // あまりよろしくないが全部拾うの面倒なので
                throw new System.Exception("エラーが発生しました。", e);
            }

            // マッチした全ての行を、キーワードをファイル名としてテキストに書き出す
            File.WriteAllText((OutputDir + "\\" + subDir) + "\\" + ValidFileName(word) + ".txt", sb.ToString());
            sb.Clear();

            return true;
        }

        /// <summary>
        /// ファイル名として無効な文字を「_」に置き換える
        /// ref: http://www.atmarkit.co.jp/fdotnet/dotnettips/551invalidchars/invalidchars.html
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ValidFileName(string s)
        {
            string valid = s;
            char[] invalidch = Path.GetInvalidFileNameChars();

            foreach (char c in invalidch)
            {
                valid = valid.Replace(c, '_');
            }
            return valid;
        }


        /// <summary>
        /// ファイルはバイナリファイルであるかどうか
        /// ref: http://yellow.ribbon.to/~tuotehhou/index.php?%2BC＃%2Bバイナリ・テキストファイルの判断
        /// </summary>
        /// <param name="filePath">パス</param>
        /// <returns>バイナリファイルの場合trueを返す</returns>
        public bool IsBinaryFile(string filePath)
        {
            FileStream fs = File.OpenRead(filePath);
            int len = (int)fs.Length;
            int count = 0;
            byte[] content = new byte[len];
            int size = fs.Read(content, 0, len);

            for (int i = 0; i < size; i++)
            {
                if (content[i] == 0)
                {
                    count++;
                    if (count == 4)
                    {
                        return true;
                    }
                }
                else
                {
                    count = 0;
                }
            }
            return false;
        }
        
    }
}
