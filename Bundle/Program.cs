using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Bundle
{
    /// <summary>
    /// 指定したディレクトリに対して検索を行います。
    /// テキストエディタの検索と異なり、キーワードをリスト形式で複数指定できます。
    /// キーワードは正規表現で記述してください。
    /// 結果ファイルはキーワードごとにテキストファイルで出力します。
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entrypoint
        /// </summary>
        /// <param name="args">使用しません</param>
        static void Main(string[] args)
        {
            // 出力先フォルダ
            string output = ConfigurationManager.AppSettings["outputDir"];

            // 多段で検索するか、設定から取得して判断する
            int mFlg = Int16.Parse(ConfigurationManager.AppSettings["multipleFlg"]);
            if(mFlg == 1)
            {
                var psr = PrimarySearch(output);
            } else if(mFlg == 2)
            {
                var psr = PrimarySearch(output);
                var ssr = SecondarySearch(output);
            } else
            {
                // テスト用
                var ssr = SecondarySearch(output);
            }

        }

        /// <summary>
        /// 第一段階の検索を実行する。
        /// </summary>
        /// <returns></returns>
        private static  bool PrimarySearch(string output)
        {
            // AppConfigから設定値を読み込む
            var dic = new Dictionary<string, string>(); // key-value
            dic.Add("Java", ConfigurationManager.AppSettings["keywordList_java"]);
            dic.Add("Interstage", ConfigurationManager.AppSettings["keywordList_interstage"]);
            dic.Add("DB", ConfigurationManager.AppSettings["keywordList_db"]);
            
            // 対象フォルダを元に検索対象ファイル一覧を作成する
            string[] fileList = Directory.GetFiles(ConfigurationManager.AppSettings["targetDir"], 
                ConfigurationManager.AppSettings["targetExtension"], SearchOption.AllDirectories);

            // 出力予定のフォルダが無かったら作成する
            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);
            
            string keyword = "";
            var g = new Grep();

            // AppConfigから読み込んだKeywordListの数だけループする
            foreach (KeyValuePair<string, string> p in dic)
            {
                try
                {
                    using (var sr = new StreamReader(p.Value, Encoding.GetEncoding(ConfigurationManager.AppSettings["keywordListEncode"])))
                    {
                // 出力予定のサブフォルダが無かったら作成する
                if (!Directory.Exists(output + "\\" + p.Key))
                    Directory.CreateDirectory(output + "\\" + p.Key);

                        // キーワードリストの行数分だけ繰り返す
                        while ((keyword = sr.ReadLine()) != null)
                        {
                            // 改行の場合、または先頭が# の場合（コメント行）は検索を実行しない
                            if ((keyword.Length == 0) || ("#".Equals(keyword.Substring(0, 1))))
                                continue;

                            bool isSuccess = g.doGrep(keyword, fileList, p.Key);
                            // 処理に失敗した場合は抜ける
                            if (!isSuccess)
                            {
                                Console.WriteLine("検索に失敗しました。");
                                break;
                            }
                        }
                    }
                }
                catch (FileNotFoundException fe)
                {
                    Console.WriteLine("【エラー】ファイル: " + p.Value + " が見つかりませんでした。");
                    Console.WriteLine(p.Key + " は検索されません。");
                }
            }

            return true;
        }


        /// <summary>
        /// 第二段階の検索を実行する。
        /// </summary>
        /// <returns></returns>
        private static bool SecondarySearch(string output)
        {
            // Digフォルダを対象に、追加Grep用のファイルを検索する
            foreach (var f in Directory.GetFiles(ConfigurationManager.AppSettings["inputDig"], "*.txt", SearchOption.AllDirectories))
            {
                var pathList = new List<string>();

                // 取得したファイル名を元に、Grep結果ファイルを取得する
                // サブディレクトリが特定できないため、OutputDir+FileNameだけではPathが確定しないので検索している
                foreach (var outputFile in Directory.GetFiles(output, System.IO.Path.GetFileName(f), SearchOption.AllDirectories))
                {
                    // ファイルを繰り返し読み込み、Path部分のみ取得する
                    foreach (var line in File.ReadLines(outputFile))
                    {
                        // PrimarySearchでPathの後にセパレータとして半角Pipe（|）を出力しているため、それ以前を取得する
                        pathList.Add(line.Substring(0, line.IndexOf("|") - 1));
                    }
                    // 検索対象のファイルリストなので、重複は削除する
                    pathList = pathList.Distinct().ToList();
                }

                // TODO: 手動GrepのためにPath一覧を出力する
                //if(bool.Parse(ConfigurationManager.AppSettings["exFileListOutput"]))


                string keyword = "";
                var g = new Grep();
                // 作成した検索対象ファイルリストをGrep実行メソッドに渡して実行
                try
                {
                    using (var sr = new StreamReader(f, Encoding.GetEncoding(ConfigurationManager.AppSettings["keywordListEncode"])))
                    {
                        // キーワードリストの行数分だけ繰り返す
                        while ((keyword = sr.ReadLine()) != null)
                        {
                            // 改行の場合、または先頭が# の場合（コメント行）は検索を実行しない
                            if ((keyword.Length == 0) || ("#".Equals(keyword.Substring(0, 1))))
                                continue;

                            // 出力予定のサブフォルダが無かったら作成する
                            if (!Directory.Exists(output + "\\Dig\\" + System.IO.Path.GetFileName(f)))
                                Directory.CreateDirectory(output + "\\Dig\\" + System.IO.Path.GetFileName(f));

                            bool isSuccess = g.doGrep(keyword, pathList.ToArray(), "Dig\\" + System.IO.Path.GetFileName(f));
                            // 処理に失敗した場合は抜ける
                            if (!isSuccess)
                            {
                                Console.WriteLine("検索に失敗しました。");
                                break;
                            }
                        }
                    }
                }
                catch (FileNotFoundException fe)
                {
                    Console.WriteLine("【エラー】ファイル: " + f + " が見つかりませんでした。");
                }

            }

            return true;
        }
     }
}
