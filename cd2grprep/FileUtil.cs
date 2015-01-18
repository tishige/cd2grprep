using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace cd2grprep
{

	/// <summary>
	/// ファイル出力失敗例外
	/// </summary>
	class FileOutputException : Exception
	{
		public FileOutputException(string msg)
			: base(msg)
		{
		}
	}

	/// <summary>
	/// ファイル出力関連のクラス
	/// </summary>
	class FileUtil
	{
		// ファイルに書き込むときに使うEncoding
		private static System.Text.Encoding FILE_ENCODING = System.Text.Encoding.GetEncoding("Shift_JIS");
		// CSV出力用のヘッダ
        private const string CSV_HEADER = "案件,ACD着信呼総数,ACD着信数,応答呼数,保留数,放棄呼数,転送呼数,フローアウト呼数,TM着呼出,TM着通話,内TM着保留,TM放棄平均,ACD応答率,ACD放棄率";

		/// <summary>
		/// CSVファイルを出力する。
		/// </summary>
		/// <param name="outputPath">パス</param>
		/// <param name="dataList">データ</param>
        public static void CreateCsv(string outputPath, List<wrkgrpreport> dataList)
		{
			try
			{
				using (System.IO.StreamWriter sr = new System.IO.StreamWriter(outputPath, false, FILE_ENCODING))
				{
					// header
					sr.WriteLine(CSV_HEADER);

					// data


                    foreach (wrkgrpreport data in dataList)
                    {
                        sr.WriteLine(data.workgroupname + "," + data.nEnteredacd + "," + data.ACDChakushin + "," + data.nAnsweredacd + "," + data.nHoldacd + "," +
                            data.nAbandonedacd + "," + data.nTransferedacd + "," + data.nFlowoutacd + "," + data.Chakuyobidashi + "," + data.Chakutsuuwa + "," +
                            data.Chakuhoryuu +","+data.Heikinhouki + "," + data.Acdoutouritsu + "," + data.Acdhoukiritsu);
                        
                    }
				}
			}
			catch (IOException e)
			{
				throw new FileOutputException(string.Format(Properties.Resources.LMSG_E_201_FILE_WRITE, e.Message));
			}
		}


		/// <summary>
		/// SQLファイルを出力する。
		/// </summary>
		/// <param name="outputPath">パス</param>
		/// <param name="dataList">データ</param>
		public static void CreateSql(string outputPath, List<UpdateData> dataList)
		{
			const string DB_DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";
			// DB名取得
			Regex reg = new Regex("Initial Catalog=(?<db>[^;]*);",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
			String db = reg.Match(Properties.Settings.Default.DBConnectString).Groups["db"].Value;

			try
			{
				using (System.IO.StreamWriter sr = new System.IO.StreamWriter(outputPath, false, FILE_ENCODING))
				{
					int count = 0;
					sr.WriteLine("USE [" + db + "]");
					sr.WriteLine("BEGIN TRANSACTION;");
					sr.WriteLine("GO");
					// data
					foreach (UpdateData data in dataList)
					{
						count++;
						sr.WriteLine("PRINT N'保留回数登録中(" + count + "/" + dataList.Count + ")'");
						sr.WriteLine("GO");

						string strInterval = data.dIntervalStart.ToString(DB_DATE_FORMAT);
						if (data.isAllWorkgroup)
						{
							string template = Properties.Settings.Default.UpdateIAgentQueueStatsAll;
							sr.WriteLine(string.Format(template, new object[] { strInterval, data.cName, "*", data.CustomValue1 }));
						}
						else
						{
							string template = Properties.Settings.Default.UpdateIAgentQueueStatsNormal;
							sr.WriteLine(string.Format(template, new object[] { strInterval, data.cName, data.cReportGroup, data.CustomValue1 }));
						}
						sr.WriteLine("GO");

						if (0 == count % Properties.Settings.Default.UpdateCommitCount)
						{
							sr.WriteLine("COMMIT TRANSACTION;");
							sr.WriteLine("GO");
							sr.WriteLine("PRINT N'コミットしました'");
							sr.WriteLine("GO");
							sr.WriteLine("BEGIN TRANSACTION;");
							sr.WriteLine("GO");
						}
					}
					sr.WriteLine("COMMIT TRANSACTION;");
					sr.WriteLine("GO");
				}
			}
			catch (IOException e)
			{
				throw new FileOutputException(string.Format(Properties.Resources.LMSG_E_201_FILE_WRITE, e.Message));
			}
		}

	}
}
