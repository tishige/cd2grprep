using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace cd2grprep
{
	/// <summary>
	/// 解析失敗例外
	/// </summary>
	class AnalyzeException : Exception
	{
		public AnalyzeException(string msg):base(msg)
		{
		}
	}

	/// <summary>
	/// ワークグループレポート集計プログラムメイン処理
	/// </summary>
	class HoldCountAnalyzer
	{
		// ロガー
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
								System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //wrkgrpdataクラス
        private List<wrkgrpdata> analyzeData = new List<wrkgrpdata>();

        List<cdIwrkgrp> ListcdIwrkgrp = new List<cdIwrkgrp>();
		/// <summary>
		/// 解析します。
		/// </summary>
		/// <param name="start">検索開始日時</param>
		/// <param name="end">検索終了日時</param>
		public void Analyze(DateTime start, DateTime end)
		{
			// 日付をGMTに変換
			DateTime startGMT = start.AddHours(-9.0d);
			DateTime endGMT = end.AddHours(-9.0d);

			try
			{
				// DBからデータ取得
				using (SqlConnection con = new SqlConnection(Properties.Settings.Default.DBConnectString))
				{
					// DB接続
					con.Open();

					/*
					 * 先に件数だけ取得
					 */
					logger.Info(Properties.Resources.LMSG_I_111_READ_COUNT);
					// SQL文指定
					SqlCommand command = new SqlCommand(Properties.Settings.Default.SelectCallDetailCountSql, con);
					command.CommandTimeout = Properties.Settings.Default.DBTimeOut;
					SqlParameter startParam = new SqlParameter("@start", SqlDbType.DateTime);
					SqlParameter endParam = new SqlParameter("@end", SqlDbType.DateTime);
					startParam.Value = startGMT;
					endParam.Value = endGMT;
					command.Parameters.Add(startParam);
					command.Parameters.Add(endParam);

					// 実行
					command.Prepare();
					int count = (int)command.ExecuteScalar();
					logger.Info(string.Format(Properties.Resources.LMSG_I_112_READ_COUNT_FIN, count));

					/*
					 * データ取得
					 */
					logger.Info(Properties.Resources.LMSG_I_101_ANALYZE);
					// SQL文指定
					command = new SqlCommand(Properties.Settings.Default.SelectCallDetailSql, con);
					command.CommandTimeout = Properties.Settings.Default.DBTimeOut;
					startParam = new SqlParameter("@start", SqlDbType.DateTime);
					endParam = new SqlParameter("@end", SqlDbType.DateTime);
					startParam.Value = startGMT;
					endParam.Value = endGMT;
					command.Parameters.Add(startParam);
					command.Parameters.Add(endParam);

					// 実行
					command.Prepare();
					using (SqlDataReader reader = command.ExecuteReader())
					{
						int i = 1;
						while (reader.Read())
						{
							logger.Info(string.Format(Properties.Resources.LMSG_I_102_ANALYZE_COUNT, i, count));
							CallDetailInfo info = new CallDetailInfo();
							info.CallEventLog = reader.GetString(0);
							info.ConnectedDate = reader.GetDateTime(1);
							info.CallId = reader.GetString(2);
                            info.callduration = reader.GetInt32(3);
                            info.holdduration = reader.GetInt32(4);
                            info.customnum3 = reader.GetInt32(5);

							Analyze1Line(info);
							i++;
						}
					}
				}
			}
			catch (SqlException e)
			{
				throw new AnalyzeException(string.Format(Properties.Resources.LMSG_E_101_SQL_ERROR, e.Message));
			}
		}

        /// <summary>
        /// CallDetailを解析した結果がセットされたクラスのリストを返す
        /// </summary>
        /// <returns></returns>
        
        public List<cdIwrkgrp> GetLineData()
        
        {
            List<cdIwrkgrp> ret = new List<cdIwrkgrp>(ListcdIwrkgrp);

			return ret;
		}

        /// <summary>
        /// datalistを集計しテキスト形式で返す
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>

        public List<wrkgrpreport> SumLineData(List<cdIwrkgrp> dataList)
        {
           
            var userdata = from user in dataList.Where(n => n.workgroupname!=null)
                           group user by new {user.workgroupname} into gr
                           select new
                           {
                               gr.Key.workgroupname,
                               sum_nEnteredAcd = gr.Sum(user => user.nEnteredacd),
                               sum_ACDChakushin = gr.Sum(user => user.nAnsweredacd)+gr.Sum(user=>user.customnum3),
                               sum_nAnsweredAcd = gr.Sum(user => user.nAnsweredacd),
                               sum_nHoldAcd = gr.Sum(user => user.nHoldacd),
                               sum_nAbandonedAcd = gr.Sum(user => user.nAbandonedacd),
                               sum_nFlowoutAcd = gr.Sum(user => user.nFlowoutacd),
                               sum_Chakuyobidashi = gr.Sum(user => user.Answeredduration),
                               sum_Chakutsuuwa = gr.Sum(user => user.Callduration),
                               sum_Chakuhoryuu = gr.Sum(user => user.Holdduration),
                               sum_Abandonedduration = gr.Sum(user => user.Abandonedduration),
                               sum_nTransferdAcd = gr.Sum(user => user.nTransferedacd)
                                                              
                           };

            //List<string[]> tmp = new List<string[]>(); ***example
            List<wrkgrpreport> wrkgrpreport = new List<wrkgrpreport>();

            foreach(var val in userdata)
            {
                wrkgrpreport wrk = new wrkgrpreport();
                //tmp.Add(new string[]{val.workgroupname.ToString(),val.sum_nEnteredAcd.ToString()}); ***example

                wrk.workgroupname = val.workgroupname.ToString();
                wrk.nEnteredacd = val.sum_nEnteredAcd.ToString();
                wrk.ACDChakushin = val.sum_ACDChakushin.ToString();
                wrk.nAnsweredacd = val.sum_nAnsweredAcd.ToString();
                wrk.nHoldacd = val.sum_nHoldAcd.ToString();
                wrk.nAbandonedacd = val.sum_nAbandonedAcd.ToString();
                wrk.nFlowoutacd = val.sum_nFlowoutAcd.ToString();
                wrk.nTransferedacd = val.sum_nTransferdAcd.ToString();
                wrk.Chakuyobidashi = new TimeSpan(0, 0, int.Parse((val.sum_Chakuyobidashi).ToString())).ToString();
                wrk.Chakutsuuwa = new TimeSpan(0, 0, int.Parse((val.sum_Chakutsuuwa).ToString())).ToString();
                wrk.Chakuhoryuu = new TimeSpan(0, 0, int.Parse((val.sum_Chakuhoryuu).ToString())).ToString();
                wrk.abandonedduration = val.sum_Abandonedduration.ToString();

                if (int.Parse(wrk.nAbandonedacd)>0 && int.Parse(wrk.abandonedduration)>0)
                {
                    int heikinhouki = int.Parse(wrk.abandonedduration) / int.Parse(wrk.nAbandonedacd);
                    TimeSpan ts = new TimeSpan(0, 0, heikinhouki);
                    wrk.Heikinhouki = ts.ToString();
                }
                else
                {
                    wrk.Heikinhouki = "0:00:00";
                }

                if (int.Parse(wrk.ACDChakushin) > 0 && int.Parse(wrk.nAnsweredacd)>0)
                {
                    double acdoutou = (double.Parse(wrk.nAnsweredacd) / double.Parse(wrk.nEnteredacd)) * 100;
                    wrk.Acdoutouritsu = acdoutou.ToString() + "%";
                }
                else
                {
                    wrk.Acdoutouritsu = "0.0%";
                }

                if (int.Parse(wrk.nAbandonedacd)>0)
                {
                    double houki = (double.Parse(wrk.nAbandonedacd) / double.Parse(wrk.nEnteredacd)) * 100;
                    wrk.Acdhoukiritsu = houki.ToString() + "%";
                }
                else
                {
                    wrk.Acdhoukiritsu = "0.0%";
                }

                wrkgrpreport.Add(wrk);

            }

            return wrkgrpreport;
        }

		Regex regWg = new Regex("(?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}).*Entered Workgroup[ ]*(?<workgroup>[^ ]+)",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		Regex regSent = new Regex("(?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}).*Sent to user[ ]*(?<user>[^ ]+)",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		Regex regConnected = new Regex("(?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}). Connected",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		Regex regHeld = new Regex("(?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}).*Held",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        Regex regTransfer = new Regex("(?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}).*Transfer to user[ ]*(?<user>[^ ]+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        //Regex regRemoteDisconnected = new Regex("(?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}). Disconnected \\[Remote Disconnect\\]",
        //        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        Regex regRemoteDisconnected = new Regex("(?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}). Disconnected \\[.*\\]",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);



		/// <summary>
		/// CallDetail1行分の解析
		/// </summary>
		/// <param name="info">行情報</param>
		private void Analyze1Line(CallDetailInfo info)
		{
			logger.Info ("コールID：" + info.CallId + " のレコードを解析します。");

			string sentUserId = null;
			string workgroup = null;
            int customnum3 = info.customnum3;
            int nAnsweredacd = 0;

            string EnteredTime = null;
            string ConnectedTime = null;
            string DisconnectedTime = null;
            bool AddListcdIwrkgrp=false;
			
            string[] logs = info.CallEventLog.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            cdIwrkgrp cdIwrkgrp = new cdIwrkgrp();

			foreach (string log in logs)
			{
				// ワークグループ
				Match mWg = regWg.Match(log);
				if (mWg.Success)
				{
					workgroup = mWg.Groups["workgroup"].Value;
                    EnteredTime = mWg.Groups["time"].Value;

                    //1001667241V0141231 RMオリエナホットライン
                    //1001666691V0141231 その他→ランドリー

                    if(cdIwrkgrp.workgroupname==null)

                    {
                        //ワークグループ新規検出
                        logger.Info(info.CallId + "ワークグループ " + workgroup + " 着信 新規 検出");

                        cdIwrkgrp.workgroupname = workgroup;
                        cdIwrkgrp.nEnteredacd++;
                        cdIwrkgrp.Entereddate = CreateDateTime(EnteredTime, info);

                    }
                    else
                    {
                        if (cdIwrkgrp.workgroupname == workgroup)
                        {
                            logger.Info(info.CallId + " １コール内で同一ワークグループ " + workgroup + " に着信しました。");
                            cdIwrkgrp.nEnteredacd++;
                            cdIwrkgrp.Entereddate = CreateDateTime(EnteredTime, info);

                        }
                        else
                        {
                            logger.Info(info.CallId + " １コール内で別ワークグループ " + workgroup + " に着信しました。");
                            
                            //直前のユーザーがnullならフローアウト　そうでなければワークグループ転送
                            if (cdIwrkgrp.username == null)
                            {
                                logger.Info(info.CallId + " 直前のワークグループ " + cdIwrkgrp.workgroupname + " はユーザー未応答のためフローアウトを+1します。");
                                cdIwrkgrp.nFlowoutacd++;
                            }
                            else
                            {
                                logger.Info(info.CallId + " 直前のワークグループ " + cdIwrkgrp.workgroupname + " はユーザー" + cdIwrkgrp.username +" が応答していたため転送発生を+1します。");
                                cdIwrkgrp.nTransferedacd++;
                            }

                            //これまでのワークグループ着信データをリストに登録
                            ListcdIwrkgrp.Add(cdIwrkgrp);
                            AddListcdIwrkgrp=true;
                            //新しいワークグループ着信情報を追加
                            logger.Info(info.CallId + "ワークグループ " + workgroup + " 着信 追加 検出");

                            cdIwrkgrp = new cdIwrkgrp();
                            cdIwrkgrp.workgroupname = workgroup;
                            cdIwrkgrp.nEnteredacd++;
                            cdIwrkgrp.Entereddate = CreateDateTime(EnteredTime, info);

                        }

                    }

				}
				// ユーザ
				Match mSent = regSent.Match(log);
				if (mSent.Success)
				{
					sentUserId = mSent.Groups["user"].Value;
				}
                // ユーザに接続
                Match mConnected = regConnected.Match(log);

                if (mConnected.Success && sentUserId != null && workgroup!=null)
                {
                    ConnectedTime = mConnected.Groups["time"].Value;
                    cdIwrkgrp.username = sentUserId;
                    cdIwrkgrp.Connecteddate = CreateDateTime(ConnectedTime, info);
                    cdIwrkgrp.nAnsweredacd++;
                    nAnsweredacd++;

                    //    //Station転送した後のConnectedを拾ってダブルカウントしてしまうためsentUserIdを初期化
                    //    //09:55:35: Sent to user t-sugawara 
                    //    //09:55:36: Connected 
                    //    //09:55:36: Sent to station 79034 
                    //    //09:56:50: Held 
                    //    //09:57:29: Connected 

                    sentUserId = null;

                    TimeSpan answeredTime = cdIwrkgrp.Connecteddate - cdIwrkgrp.Entereddate;
                    cdIwrkgrp.Answeredduration = answeredTime.TotalSeconds;
                    cdIwrkgrp.Callduration = info.callduration;
   
                }

                // 保留
                Match mHeld = regHeld.Match(log);
                if (mHeld.Success && workgroup!=null)
                {
                    cdIwrkgrp.nHoldacd++;
                    cdIwrkgrp.Holdduration = info.holdduration;
                }

                // 転送
                Match mTransfer = regTransfer.Match(log);
                if(mTransfer.Success && workgroup!=null)
                {
                    cdIwrkgrp.nTransferedacd++;
                }
                
                //Remote Disconnect
                Match mRemote = regRemoteDisconnected.Match(log);
                if ((mRemote.Success && workgroup != null && (nAnsweredacd == 0) | info.customnum3 == 1))
                {

                    DisconnectedTime = mRemote.Groups["time"].Value;
                    cdIwrkgrp.Disconnecteddate = CreateDateTime(DisconnectedTime, info);

                    if (info.customnum3==1)
                    {
                        logger.Info(info.CallId + " はcustomnum3=1のため " + workgroup + " の放棄呼を+1します。");
                        TimeSpan abandonedTime = cdIwrkgrp.Disconnecteddate - cdIwrkgrp.Entereddate;
                        cdIwrkgrp.Abandonedduration = abandonedTime.TotalSeconds;
                        cdIwrkgrp.nAbandonedacd++;

                    }

                    //ワークグループ着信が複数回だったらCallDetailLogのDisconnect発生時点でリストに登録
                    if (AddListcdIwrkgrp)
                    {
                        ListcdIwrkgrp.Add(cdIwrkgrp);
                    }

                }

                //    if (info.ConnectedDate.ToString("HH:mm:ss").CompareTo(connectDateTime.ToString("HH:mm:ss")) > 0)
                //    {
                //        // 日付またぎの場合1日追加
                //        connectDateTime = connectDateTime + TimeSpan.FromDays(1.0d);

                //    }
                //    // 単位時間に変換
                //    connectDateTime = RoundUp(connectDateTime, TimeSpan.FromMinutes(15));


            }
            //ワークグループ着信が１回だけだったらCallDetailLogの解析終了時点でリストに登録
            if (AddListcdIwrkgrp==false)
            {
                ListcdIwrkgrp.Add(cdIwrkgrp);
            }
           
        }

        public DateTime CreateDateTime(string condate,CallDetailInfo cdinfo)
        {
            string[] connectTimes = condate.Split(':');
            DateTime ret = cdinfo.ConnectedDate.Date + new TimeSpan(int.Parse(connectTimes[0]), int.Parse(connectTimes[1]), int.Parse(connectTimes[2]));
            return ret;
        }

		/// <summary>
		/// 単位時間に丸めます
		/// </summary>
		/// <param name="dt">時刻</param>
		/// <param name="d">指定時間の刻み</param>
		/// <returns>時刻</returns>
		DateTime RoundUp(DateTime dt, TimeSpan d)
		{
			return new DateTime(dt.Ticks / d.Ticks * d.Ticks);
		}
	}

	/// <summary>
	/// コールディテールテーブルの情報
	/// </summary>
	class CallDetailInfo
	{
		/// <summary>
		/// イベントログ
		/// </summary>
		public string CallEventLog { get; set; }
		/// <summary>
		/// 接続日時
		/// </summary>
		public DateTime ConnectedDate { get; set; }
		/// <summary>
		/// コールID
		/// </summary>
		public string CallId { get; set; }
        /// <summary>
        /// 通話時間
        /// </summary>
        public int callduration { get; set; }
        /// <summary>
        /// 保留時間
        /// </summary>
        public int holdduration { get; set; }
        /// <summary>
        /// 放棄呼フラグ customnum3=1
        /// </summary>
        public int customnum3 { get; set; }


	}

    /// <summary>
    /// IwrkgrpQueueStats
    /// </summary>

    class wrkgrpdata
    {
        public DateTime dIntervaldate { get; set; }
        public DateTime connecteddate { get; set; }
        public DateTime answereddate { get; set; }
        public string workgroupname { get; set; }
        public int nEnteredacd{get;set;}
        public int nAnsweredacd { get; set; }
        public double answeredduration { get; set; }
        public int callduration { get; set; }
        public int holdduration { get; set; }
        public int nHoldacd { get; set; }
        public int customnum3 { get; set; }
        public double abandonedduration { get; set; }
        public int nFlowoutacd { get; set; }
        public string callid{get;set;}

    }

    class cdIwrkgrp
    {
        public DateTime dIntervaldate { get; set; }
        public DateTime Entereddate { get; set; }
        public DateTime Connecteddate { get; set; }
        public DateTime Answereddate { get; set; }
        public DateTime Disconnecteddate { get; set; }
        public string workgroupname { get; set; }
        public string username { get; set; }
        public int nEnteredacd { get; set; }
        public int nAnsweredacd { get; set; }
        public int nFlowoutacd { get; set; }
        public int nTransferedacd { get; set; }
        public int nAbandonedacd { get; set; }
        public int nHoldacd { get; set; }
        public double Answeredduration { get; set; }
        public int Callduration { get; set; }
        public int Holdduration { get; set; }
        public double Abandonedduration { get; set; }
        public int customnum3 { get; set; }
        public string callid { get; set; }

    }


    class wrkgrpreport
    {
        public string dIntervaldate { get; set; }
        public string connecteddate { get; set; }
        public string answereddate { get; set; }
        public string workgroupname { get; set; }
        public string nEnteredacd { get; set; }
        public string ACDChakushin { get; set; }
        public string nAnsweredacd { get; set; }
        public string answeredduration { get; set; }
        public string callduration { get; set; }
        public string holdduration { get; set; }
        public string nHoldacd { get; set; }
        public string nAbandonedacd { get; set; }
        public string abandonedduration { get; set; }
        public string nFlowoutacd { get; set; }
        public string nTransferedacd { get;set;}
        public string Chakuyobidashi { get; set; }
        public string Chakutsuuwa { get; set; }
        public string Chakuhoryuu { get; set; }
        public string Heikinhouki { get; set; }
        public string Acdoutouritsu { get; set; }
        public string Acdhoukiritsu { get; set; }
        public string callid { get; set; }

    }

	/// <summary>
	/// IAgentQueueStats用の情報
	/// </summary>
	class UpdateData
	{
		/// <summary>
		/// 単位時間
		/// </summary>
		public DateTime dIntervalStart { get; set; }
		/// <summary>
		/// エージェントID
		/// </summary>
		public string cName { get; set; }
		/// <summary>
		/// ワークグループ名
		/// </summary>
		public string cReportGroup { get; set; }
		/// <summary>
		/// 保留回数
		/// </summary>
		public int CustomValue1 { get; set; }

		/// <summary>
		/// 全ワークグループが対象かどうか
		/// </summary>
		public bool isAllWorkgroup { get; set; }

		/// <summary>
		/// カンマ区切りの文字列を返却
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			const string WG_ALL = "*";
			const char SEPARATOR = ',';
			StringBuilder sb = new StringBuilder();
			sb.Append(dIntervalStart);
			sb.Append(SEPARATOR);
			sb.Append(cName);
			sb.Append(SEPARATOR);
			if (isAllWorkgroup)
			{
				sb.Append(WG_ALL);
			}
			else
			{
				sb.Append(cReportGroup);
			}
			sb.Append(SEPARATOR);
			sb.Append(CustomValue1);
			return sb.ToString();
		}
	}
}
