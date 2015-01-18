using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Globalization;

namespace cd2grprep
{
	/// <summary>
	/// 保留数カウントプログラム本体
	/// </summary>
	class Program
	{
		// ロガー
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
								System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private const string DATE_FORMAT_ARG = "yyyyMMddHHmmss";
		/// <summary>
		/// メイン処理
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			logger.Info(Properties.Resources.LMSG_I_001_START);

			try
			{
				// 引数チェック
				if (args.Length != 2)// && args.Length != 4)
				{
					// 引数は2のみ
					logger.Error(Properties.Resources.LMSG_E_001_INV_ARG);
					printUsage();
					return;
				}
				DateTime start;
				if (!DateTime.TryParseExact(args[0], DATE_FORMAT_ARG, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out start))
				{
					logger.Error(Properties.Resources.LMSG_E_002_INV_DATE);
					printUsage();
					return;
				}
				DateTime end;
				if (!DateTime.TryParseExact(args[1], DATE_FORMAT_ARG, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out end))
				{
					logger.Error(Properties.Resources.LMSG_E_002_INV_DATE);
					printUsage();
					return;
				}
				String csvPath = "AnalysisResult_" + args[0] + "-" + args[1] + "_" + DateTime.Now.ToString(DATE_FORMAT_ARG) + ".csv";
                //String sqlPath = "UpdateAgentQueueStats_" + args[0] + "-" + args[1] + "_" + DateTime.Now.ToString(DATE_FORMAT_ARG) + ".sql";
                //if (args.Length == 4)
                //{
                //    csvPath = args[2];
                //    sqlPath = args[3];
                //}

				// 解析実行
				HoldCountAnalyzer exe = new HoldCountAnalyzer();
				exe.Analyze(start, end);

				// 結果取得
                List<cdIwrkgrp> dataList = exe.GetLineData();

                //Workgroup名でソート
                dataList = dataList.OrderBy(n => n.workgroupname).ToList();

                List<wrkgrpreport> wrkgrpreport = exe.SumLineData(dataList);

				// 日時順にソート
				//dataList = dataList.OrderBy(n => n.dIntervalStart).ThenBy(n => n.cName).ThenBy(n => n.cReportGroup).ToList();

				// CSV出力
				logger.Info(Properties.Resources.LMSG_I_201_CSV);
                FileUtil.CreateCsv(csvPath, wrkgrpreport);


				// SQL出力
				//logger.Info(Properties.Resources.LMSG_I_202_SQL);
				//FileUtil.CreateSql(sqlPath, dataList);

			}
			catch (AnalyzeException e)
			{
				logger.Error(e.Message, e);
			}
			catch (FileOutputException e)
			{
				logger.Error(e.Message, e);
			}
			catch (Exception e)
			{
				logger.Error(String.Format(Properties.Resources.LMSG_E_999_ACCIDENTAL, e.Message), e);
			}
			finally
			{
				logger.Info(Properties.Resources.LMSG_I_002_END);
			}
		}

		/// <summary>
		/// USAGEを出力する
		/// </summary>
		private static void printUsage()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Usage:");
			sb.AppendLine("cd2grprep.exe 読取開始日時 読取終了日時(範囲は1時間以内を推奨)");
			sb.AppendLine("");
			sb.AppendLine("読取開始日時　　　：CallDetailテーブルを読み取る範囲指定（開始日時）");
			sb.AppendLine("　　　　　　　　　　yyyyMMddHHmmss形式で指定");
			sb.AppendLine("読取終了日時　　　：CallDetailテーブルを読み取る範囲指定（終了日時） ");
			sb.AppendLine("　　　　　　　　　　yyyyMMddHHmmss形式で指定");
			Console.Out.Write(sb.ToString());
		}
	}
}
