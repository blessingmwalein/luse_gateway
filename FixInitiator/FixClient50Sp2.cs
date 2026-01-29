using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using QuickFix;
using QuickFix.FIX50;
using System.Data;
using QuickFix.Fields;
using Application = QuickFix.Application;
using Message = QuickFix.Message;
using SecurityStatus = QuickFix.FIX50.SecurityStatus;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Text;
using System.Timers;
using Timer = System.Threading.Timer;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

namespace FixInitiator
{
    public class FixClient50Sp2 : MessageCracker, Application
    {
        public FixClient50Sp2 _client;

        private IInitiator _initiator;


        public FixClient50Sp2(SessionSettings settings)
        {
            ActiveSessionId = null;
        }

        public SessionID ActiveSessionId { get; set; }

        public IInitiator Initiator
        {
            set
            {
                if (_initiator != null)
                    throw new Exception("You already set the initiator");
                _initiator = value;
            }
            get
            {
                if (_initiator == null)
                    throw new Exception("You didn't provide an initiator");
                return _initiator;
            }
        }

        #region Application Members
        /// <summary>
        /// every inbound admin level message will pass through this method, 
        /// such as heartbeats, logons, and logouts. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sessionId"></param>
        public void FromAdmin(Message message, SessionID sessionId)
        {
            Trace.WriteLine("## FromAdmin: " + message);
            Log("## FromAdmin: "+message.ToString());
            if (message.Header.GetField(Tags.MsgType) == MsgType.LOGOUT)
            {
                message.SetField(new SessionStatus(100));
                // message.SetField(new Text("Logout"));
            }
            else if (message.Header.GetField(Tags.MsgType) == MsgType.PARTYDETAILSLISTREPORT)
            {
                Trace.WriteLine("## PARTYDETAILSLISTREPORT: " + message);
            }
          // sendOrder(_client.ActiveSessionId);
            //sendOrder(sessionId);
            //AmendOrder(sessionId);
            //CancelOrder(sessionId);
        }


        /// <summary>
        /// every inbound application level message will pass through this method, 
        /// such as orders, executions, secutiry definitions, and market data.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sessionID"></param>
        public void FromApp(Message message, SessionID sessionID)
        {
            try
            {
                // Trace.WriteLine("## FromApp: " + message);

                //Crack(message, sessionID);
                //   Log("Security Definition: " + message);


                string BrokerRef = "";
                string AmountValue = "";
                string account = "";
                string Shareholder = "";
                string orderstatus = "";
                string ordernumber = "";
                string orderidentifier = "";
                string settlementdate = "";
                string quantity = "";
                string timeinforce = "";
                string force = "";
                string orderside = "";
                string securitycode = "";
                string side = "";
                string Price = "";
                string maturitydate = "";
                string securitytype = "";
                string leavesquantity = "";
                string grosstradeamt = "";
                string nopartyID = "";
                string brokercode = "";
                string trader = "";
                string newprice = "";
                string matcheddate = "";
                string ExecutionType = "";
                double prices = 0.0;
                SecurityID SecurityID = new SecurityID();
                Symbol Symbol = new Symbol();
                string qnty = "", Bprice = "";
                double qnt = 0.0;
                double pric = 0.0;
                double amt = 0.0;
                string orderno = "";
                string securityid = "";
                string brokerref = "";
                string BasePrice = "";
                string percentageorvalue = "";
                if (message is TradeCaptureReport)
                {
                    Trace.WriteLine("## FromApp: " + message);
                    Log("received Trade Capture report");
                    Log("## FromApp: " + message);
                    QuickFix.FIX50.TradeCaptureReport er = new QuickFix.FIX50.TradeCaptureReport();
                    er = (QuickFix.FIX50.TradeCaptureReport)message;
                    //orderstatus = er.OrdStatus.getValue().ToString();
                    Price = er.LastPx.getValue().ToString();
                    quantity = er.LastQty.getValue().ToString();
                    grosstradeamt = er.GrossTradeAmt.getValue().ToString();
                    securityid = er.SecurityID.getValue().ToString();
                    matcheddate = er.TradeDate.getValue().ToString();

                    //ExecutionType = er.ExecType.getValue().ToString();
                    var group = new TradeCaptureReport.NoSidesGroup();
                    group = (TradeCaptureReport.NoSidesGroup)er.GetGroup(1, group);
                    if (group.IsSetSide())
                    {
                        switch (group.Side.getValue())
                        {
                            case Side.BUY:
                                side = "BUY";
                                break;
                            case Side.SELL:
                                side = "SELL";
                                break;
                        }
                    }

                    Shareholder = group.Account.getValue();
                    ordernumber = group.ClOrdID.getValue();
                    settlementdate = er.MaturityDate.getValue().ToString();
                    orderidentifier = group.ClOrdID.getValue();
                    SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    string query14 = "  insert into [testcds_ROUTER].[dbo].[Trades] ([SecurityID],[MatchedQuantity],[MatchedPrice],[GrossTradeAmount],[MatchedDate],[OrderNumber],[TradingAccount],[SettlementDate],[SIDE]) values ('" + securityid + "','" + quantity + "','" + Price + "','" + grosstradeamt + "',getdate(),'" + ordernumber + "','" + Shareholder + "','" + settlementdate + "','" + side + "')";
                    SqlCommand cmd14 = new SqlCommand(query14, con14);

                    con14.Open();
                    cmd14.CommandTimeout = 0;
                    cmd14.ExecuteNonQuery();
                    con14.Close();
                    SqlConnection con141 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    string query141 = "Update testcds_ROUTER.dbo.[Pre_Order_Live] set OrderStatus ='MATCHED ORDER', exchange_orderNumber ='" + orderidentifier + "',Quantity = '" + quantity + "', MatchedDate = '" + DateTime.Now.ToString() + "', MatchedPrice = '" + Price + "' where OrderNumber = '" + orderidentifier + "' and  OrderStatus in ('NEW','POSTED')";
                    SqlCommand cmd141 = new SqlCommand(query141, con141);
                    con141.Open();
                    cmd141.CommandTimeout = 0;
                    cmd141.ExecuteNonQuery();
                    con141.Close();


                    string orderattribute = "";

                    SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    string query = "  select  top 1 OrderAttribute,orderno, brokerref, BasePrice,Shareholder from testcds_ROUTER.dbo.Pre_Order_Live  where OrderNumber = '" + orderidentifier + "' and  OrderStatus ='MATCHED ORDER'  order by orderno desc";

                    SqlCommand cmd = new SqlCommand(query, con3);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet("ds");

                    da.Fill(ds);

                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {

                            // insert prices into table u created
                            orderattribute = dr["OrderAttribute"].ToString();
                            orderno = dr["orderno"].ToString();
                            brokerref = dr["brokerref"].ToString();
                            BasePrice = dr["BasePrice"].ToString();
                            Shareholder = dr["Shareholder"].ToString();

                            double bprice = (double)Convert.ToDouble(BasePrice);
                            double mprice = (double)Convert.ToDouble(Price);
                            double nprice = 0.0;
                            double residueamount = 0.0;
                            int orderattr = (int)Convert.ToInt64(orderattribute);

                            int quant = (int)Convert.ToInt64(quantity);
                            if (mprice < bprice)
                            {
                                nprice = bprice - mprice;

                                residueamount = quant * nprice;

                                SqlConnection con31 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query31 = "SELECT * FROM [CDS_ROUTER].[dbo].[para_Billing] where ChargeName = 'Luse Charges'";

                                SqlCommand cmd31 = new SqlCommand(query31, con31);
                                cmd31.CommandTimeout = 0;
                                SqlDataAdapter da1 = new SqlDataAdapter(cmd31);
                                DataSet ds1 = new DataSet("ds");

                                da1.Fill(ds1);

                                DataTable dt1 = ds1.Tables[0];
                                if (dt1.Rows.Count > 0)
                                {
                                    foreach (DataRow dr1 in dt1.Rows)
                                    {

                                        // insert prices into table u created
                                        percentageorvalue = dr1["percentageorvalue"].ToString();
                                        //AmountValue = dr["Amount"].ToString();


                                    }
                                }


                                double percentvalue = 0.0;
                                percentvalue = Convert.ToDouble(percentageorvalue);

                                residueamount = residueamount * percentvalue;
                                SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query1 = " insert into [CDSC].[dbo].[CashTrans]([Description],[TransType],[Amount],[DateCreated],[CDS_Number],[Reference]) values ('Cash Refund Residual','Refund','" + residueamount + "',getdate(),'" + Shareholder + "','" + brokerref + "')    ";
                                SqlCommand cmd1 = new SqlCommand(query1, con4);
                                con4.Open();
                                cmd1.CommandTimeout = 0;
                                cmd1.ExecuteNonQuery();
                                con4.Close();
                            }


                            int newquantity = 0;
                            newquantity = orderattr - quant;
                            if (newquantity > 0)
                            {
                                SqlConnection con25 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query25 = "insert into testcds_ROUTER.dbo.pre_order_live (OrderType,Company,SecurityType,CDS_AC_No,Broker_Code,Client_Type,Tax,Shareholder,ClientName,TotalShareHolding,OrderStatus,Create_date,Deal_Begin_Date ,Expiry_Date,Quantity,BasePrice,AvailableShares,OrderPref,OrderAttribute,Marketboard,TimeInForce,OrderQualifier,BrokerRef ,ContraBrokerId ,MaxPrice  ,MiniPrice,Flag_oldorder,OrderNumber,Currency ,FOK ,Affirmation,trading_platform ,Symbol ,Custodian,Source,borrowStatus,AmountValue ,Source_of_Funds,Purpose_of_Investment,side,exchange_orderNumber) SELECT [OrderType] ,[Company] ,[SecurityType] ,[CDS_AC_No] ,[Broker_Code],[Client_Type],[Tax] ,[Shareholder],[ClientName] ,[TotalShareHolding],'NEW',[Create_date],[Deal_Begin_Date],[Expiry_Date] ,'" + newquantity + "' ,BasePrice ,[AvailableShares],[OrderPref] ,'" + newquantity + "',[Marketboard] ,[TimeInForce],[OrderQualifier],[BrokerRef]+'_" + partialcount(orderno).ToString() + "' ,[ContraBrokerId] ,[MaxPrice],[MiniPrice] ,[Flag_oldorder],[OrderNumber],[Currency] ,[FOK],[Affirmation],[trading_platform] ,[Symbol],Custodian, NULL,NULL, NULL, NULL, NULL,side,exchange_orderNumber   FROM testcds_ROUTER.dbo.[Pre_Order_Live] where orderno='" + orderno + "'";


                                SqlCommand cmd25 = new SqlCommand(query25, con25);
                                con25.Open();
                                cmd25.CommandTimeout = 0;
                                cmd25.ExecuteNonQuery();
                                con25.Close();


                            }


                        }
                    }

                }
                if (message is ExecutionReport)
                {

                    Trace.WriteLine("## FromApp: " + message);
                    Log("received execution report");
                    Log("## FromApp: " + message);
                    QuickFix.FIX50.ExecutionReport er = new QuickFix.FIX50.ExecutionReport();
                    er = (QuickFix.FIX50.ExecutionReport)message;
                    orderstatus = er.OrdStatus.getValue().ToString();
                    ordernumber = er.OrderID.getValue().ToString();
                    orderidentifier = er.ClOrdID.getValue().ToString();
                    ExecutionType = er.ExecType.getValue().ToString();


                    if (ExecutionType == "0")
                    {
                        if (orderstatus == "0")
                        {

                            orderidentifier = er.ClOrdID.getValue().ToString();
                            quantity = er.OrderQty.getValue().ToString();
                            SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                            string query14 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='NEW' , exchange_orderNumber ='" + orderidentifier + "' where OrderNumber = '" + orderidentifier + "' and Orderstatus = 'POSTED'";
                            SqlCommand cmd14 = new SqlCommand(query14, con14);
                            con14.Open();
                            cmd14.CommandTimeout = 0;
                            cmd14.ExecuteNonQuery();
                            con14.Close();

                            //quantity = er.OrderQty.getValue().ToString();
                            //timeinforce = er.TimeInForce.getValue().ToString();

                            securitycode = er.SecurityID.getValue().ToString();
                            side = er.Side.getValue().ToString();
                            Price = er.Price.getValue().ToString();
                            maturitydate = er.MaturityDate.getValue().ToString();
                            securitytype = er.SecurityType.getValue().ToString();
                            account = er.Account.getValue().ToString();


                            SqlConnection con16 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                            string query16 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='PENDING CANCELLATION'  where OrderNumber ='" + orderidentifier + "' and Orderstatus = 'CancellationSentToAts'";
                            SqlCommand cmd16 = new SqlCommand(query16, con16);
                            con16.Open();
                            cmd16.CommandTimeout = 0;
                            cmd16.ExecuteNonQuery();
                            con16.Close();

                            // Retrieving Broker Code  and Trader 

                            nopartyID = er.NoPartyIDs.getValue().ToString();
                            var sidesGrp1 = new QuickFix.FIX50.ExecutionReport.NoPartyIDsGroup();
                            brokercode = er.GetGroup(1, sidesGrp1).ToString();
                            // sidesGrp1 now has all fields populated
                            var sidesGrp2 = new QuickFix.FIX50.ExecutionReport.NoPartyIDsGroup();
                            trader = er.GetGroup(2, sidesGrp2).ToString();

                            if (timeinforce == "0")
                            {
                                force = "Day";
                            }
                            if (timeinforce == "1")
                            {
                                force = "Good Till Cancel (GTC)";
                            }
                            if (timeinforce == "2")
                            {
                                force = "At the Opening (OPG)";
                            }
                            if (timeinforce == "3")
                            {
                                force = "Immediate or Cancel (IOC)";
                            }
                            if (timeinforce == "4")
                            {
                                force = "Fill or Kill (FOK)";
                            }
                            if (timeinforce == "5")
                            {
                                force = "Good Till Crossing (GTX)";
                            }
                            if (timeinforce == "6")
                            {
                                force = "Good Till Date";
                            }

                            //  writetoerrorfile("ordernumber " + ordernumber + " orderstatus " + orderstatus + " orderidentifier " + orderidentifier);

                            if (side == "1")
                            {
                                orderside = "BUY";
                                SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query = "select company from [testcds_ROUTER].[dbo].CompanyPrices where Company = '" + securitycode + "'";

                                SqlCommand cmd = new SqlCommand(query, con3);

                                SqlDataAdapter da = new SqlDataAdapter(cmd);
                                DataSet ds = new DataSet("ds");

                                da.Fill(ds);

                                DataTable dt = ds.Tables[0];
                                if (dt.Rows.Count > 0)
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {

                                        // insert prices into table u created


                                        SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                        string query1 = "    update [testcds_ROUTER].[dbo].CompanyPrices set BestBid = '" + Price + "', maturitydate = '" + maturitydate + "'  where Company ='" + securitycode + "'";
                                        SqlCommand cmd1 = new SqlCommand(query1, con4);
                                        con4.Open();
                                        cmd1.CommandTimeout = 0;
                                        cmd1.ExecuteNonQuery();
                                        con4.Close();
                                    }
                                }
                                else
                                {
                                    SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                    string query1 = " insert into [testcds_ROUTER].[dbo].CompanyPrices (BestBid, maturitydate,  COMPANY, SecurityType) values ('" + Price + "', '" + maturitydate + "','" + securitycode + "','' )  ";
                                    SqlCommand cmd1 = new SqlCommand(query1, con4);
                                    con4.Open();
                                    cmd1.CommandTimeout = 0;
                                    cmd1.ExecuteNonQuery();
                                    con4.Close();
                                }
                                con3.Close();
                            }
                            else if (side == "2")
                            {
                                orderside = "SELL";
                                SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query = "select company from [testcds_ROUTER].[dbo].CompanyPrices where Company = '" + securitycode + "'";

                                SqlCommand cmd = new SqlCommand(query, con3);
                                cmd.CommandTimeout = 0;
                                SqlDataAdapter da = new SqlDataAdapter(cmd);
                                DataSet ds = new DataSet("ds");

                                da.Fill(ds);

                                DataTable dt = ds.Tables[0];
                                if (dt.Rows.Count > 0)
                                {
                                    foreach (DataRow dr in dt.Rows)
                                    {

                                        //besk ask 


                                        SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                        string query1 = "    update [testcds_ROUTER].[dbo].CompanyPrices set BestAsk = '" + Price + "', maturitydate = '" + maturitydate + "'  where Company ='" + securitycode + "'";
                                        SqlCommand cmd1 = new SqlCommand(query1, con4);
                                        con4.Open();
                                        cmd1.CommandTimeout = 0;
                                        cmd1.ExecuteNonQuery();
                                        con4.Close();
                                    }
                                }
                                else
                                {
                                    SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                    string query1 = " insert into [testcds_ROUTER].[dbo].CompanyPrices (BestAsk, maturitydate,  COMPANY, SecurityType) values ('" + Price + "', '" + maturitydate + "','" + securitycode + "','' )  ";
                                    SqlCommand cmd1 = new SqlCommand(query1, con4);
                                    con4.Open();
                                    cmd1.CommandTimeout = 0;
                                    cmd1.ExecuteNonQuery();
                                    con4.Close();
                                }
                                con3.Close();
                            }

                            //  market demand market  supply 
                            quantity = er.OrderQty.getValue().ToString();
                            writetoerrorfile("## FromApp: insert into  [testcds_ROUTER].[dbo].[Live_Orders] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier], [CDS_AC_No],[Broker_code], [Trader] ) values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','NEW' ,'" + quantity + "','" + Price + "','" + force + "',' " + maturitydate + "', '" + orderside + "', '" + orderidentifier + "', '" + account + "','" + brokercode + "', '" + trader + "') ");
                            SqlConnection con24 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

                            string query21 = " insert into  [testcds_ROUTER].[dbo].[Live_Orders] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier], [CDS_AC_No] ) values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','NEW' ,'" + quantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "', '" + account + "')  ";
                            SqlCommand cmd21 = new SqlCommand(query21, con24);
                            con24.Open();
                            cmd21.CommandTimeout = 0;
                            cmd21.ExecuteNonQuery();
                            con24.Close();

                            //con23.Close();
                        }
                    }


                    if (orderstatus == "8")

                    {
                        string rejectionreason = "";
                        ordernumber = er.OrderID.getValue().ToString();
                        orderidentifier = er.ClOrdID.getValue().ToString();
                        rejectionreason = er.Text.getValue().ToString();
                        rejectionreason = rejectionreason.Replace("'", "");
                        SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query14 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='REJECTED', RejectionReason = '" + rejectionreason + "' , exchange_orderNumber ='" + orderidentifier + "' where OrderNumber = '" + orderidentifier + "'";
                        SqlCommand cmd14 = new SqlCommand(query14, con14);
                        con14.Open();
                        cmd14.CommandTimeout = 0;
                        cmd14.ExecuteNonQuery();
                        con14.Close();


                        //SqlConnection con15 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        //string query15 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='REJECTED' where OrderNumber like '%" + orderidentifier + "%'";
                        //SqlCommand cmd15 = new SqlCommand(query15, con15);
                        //con14.Open();
                        //cmd14.ExecuteNonQuery();
                        //con14.Close();


                        //  market demand market  supply 
                        //    quantity = er.OrderQty.getValue().ToString();
                        //  timeinforce = er.TimeInForce.getValue().ToString();

                        //securitycode = er.SecurityID.getValue().ToString();
                        //side = er.Side.getValue().ToString();
                        //Price = er.Price.getValue().ToString();
                        //maturitydate = er.MaturityDate.getValue().ToString();
                        //securitytype = er.SecurityType.getValue().ToString();
                        if (timeinforce == "0")
                        {
                            force = "Day";
                        }
                        if (timeinforce == "1")
                        {
                            force = "Good Till Cancel (GTC)";
                        }
                        if (timeinforce == "2")
                        {
                            force = "At the Opening (OPG)";
                        }
                        if (timeinforce == "3")
                        {
                            force = "Immediate or Cancel (IOC)";
                        }
                        if (timeinforce == "4")
                        {
                            force = "Fill or Kill (FOK)";
                        }
                        if (timeinforce == "5")
                        {
                            force = "Good Till Crossing (GTX)";
                        }
                        if (timeinforce == "6")
                        {
                            force = "Good Till Date";
                        }

                        //  SqlConnection con24 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        ////  quantity = er.OrderQty.getValue().ToString();
                        //  string query21 = " insert into  [testcds_ROUTER].[dbo].[Live_Orders] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier])values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','REJECTED' ,'" + quantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "')  ";
                        //  SqlCommand cmd21 = new SqlCommand(query21, con24);
                        //  con24.Open();
                        //  cmd21.ExecuteNonQuery();
                        //  con24.Close();


                        ////SqlConnection con23 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        ////string query23 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set  RejectionReason = '" + rejectionreason + "' where OrderNumber like '%" + orderidentifier + "%'";
                        ////SqlCommand cmd23 = new SqlCommand(query23, con23);
                        ////con23.Open();
                        ////cmd23.ExecuteNonQuery();
                        ////con23.Close();

                        SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query = "select Shareholder, BrokerRef, Amount, POL.Quantity as qnty, POL.BasePrice as Bprice,* from  [testcds_ROUTER].[dbo].[Pre_Order_Live] POL join  [CDSC].[dbo].[CashTrans] CT on POL.BrokerRef = CT.Reference where POL.OrderNumber like '%" + orderidentifier + "%' AND POL.SIDE ='BUY'";

                        SqlCommand cmd = new SqlCommand(query, con3);
                        cmd.CommandTimeout = 0;
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataSet ds = new DataSet("ds");

                        da.Fill(ds);

                        DataTable dt = ds.Tables[0];
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dt.Rows)
                            {

                                // insert prices into table u created
                                BrokerRef = dr["BrokerRef"].ToString();
                                //AmountValue = dr["Amount"].ToString();
                                Shareholder = dr["Shareholder"].ToString();
                                qnty = dr["qnty"].ToString();
                                Bprice = dr["Bprice"].ToString();


                                SqlConnection con31 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query31 = "SELECT * FROM [CDS_ROUTER].[dbo].[para_Billing] where ChargeName = 'Luse Charges'";

                                SqlCommand cmd31 = new SqlCommand(query31, con31);
                                cmd31.CommandTimeout = 0;
                                SqlDataAdapter da1 = new SqlDataAdapter(cmd31);
                                DataSet ds1 = new DataSet("ds");

                                da1.Fill(ds1);

                                DataTable dt1 = ds1.Tables[0];
                                if (dt1.Rows.Count > 0)
                                {
                                    foreach (DataRow dr1 in dt1.Rows)
                                    {

                                        // insert prices into table u created
                                        percentageorvalue = dr1["percentageorvalue"].ToString();
                                        //AmountValue = dr["Amount"].ToString();


                                    }
                                }

                                double percentvalue = 0.0;
                                percentvalue = Convert.ToDouble(percentageorvalue);
                                qnt = Convert.ToDouble(qnty);
                                pric = Convert.ToDouble(Bprice);

                                amt = qnt * pric * percentvalue;

                                AmountValue = Convert.ToString(amt);
                                double totalamount = 0.0;
                                totalamount = Convert.ToDouble(AmountValue);
                                //totalamount = totalamount * -1;
                                AmountValue = totalamount.ToString();
                                SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query1 = " insert into [CDSC].[dbo].[CashTrans]([Description],[TransType],[Amount],[DateCreated],[CDS_Number],[Reference]) values ('Cash Refund','Refund','" + AmountValue + "',getdate(),'" + Shareholder + "','" + BrokerRef + "')    ";
                                SqlCommand cmd1 = new SqlCommand(query1, con4);
                                con4.Open();
                                cmd1.CommandTimeout = 0;
                                cmd1.ExecuteNonQuery();
                                con4.Close();
                            }
                        }

                    }
                    else if (orderstatus == "2")
                    {
                        account = er.Account.getValue().ToString();
                        quantity = er.CumQty.getValue().ToString();
                        //  timeinforce = er.TimeInForce.getValue().ToString();

                        //    securitycode = er.SecurityID.getValue().ToString();
                        side = er.Side.getValue().ToString();
                        Price = er.Price.getValue().ToString();
                        // prices = Convert.ToDouble( er.LastPx.getValue());
                        //  maturitydate = er.MaturityDate.getValue().ToString();
                        if (ExecutionType == "2")
                        {

                            //  prices = Convert.ToDouble(er.LastPx.getValue());

                            matcheddate = er.TradeDate.getValue().ToString();
                        }
                        securitytype = er.SecurityType.getValue().ToString();

                        //SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        //string query14 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='MATCHED ORDER', exchange_orderNumber ='" + orderidentifier + "',Quantity = '" + quantity + "', MatchedDate = '" + DateTime.Now.ToString() + "', MatchedPrice = '" + prices + "' where OrderNumber LIKE '%" + orderidentifier + "%' and  OrderStatus ='NEW'";
                        //SqlCommand cmd14 = new SqlCommand(query14, con14);
                        //con14.Open();
                        //cmd14.ExecuteNonQuery();
                        //con14.Close();


                        //string orderattribute = "";

                        //SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        //string query = "  select  top 1 OrderAttribute,orderno, brokerref from testcds_ROUTER.dbo.Pre_Order_Live  where OrderNumber LIKE '%" + orderidentifier + "%' and  OrderStatus ='MATCHED ORDER'  order by orderno desc";

                        //SqlCommand cmd = new SqlCommand(query, con3);

                        //SqlDataAdapter da = new SqlDataAdapter(cmd);
                        //DataSet ds = new DataSet("ds");

                        //da.Fill(ds);

                        //DataTable dt = ds.Tables[0];
                        //if (dt.Rows.Count > 0)
                        //{
                        //    foreach (DataRow dr in dt.Rows)
                        //    {

                        //        // insert prices into table u created
                        //        orderattribute = dr["OrderAttribute"].ToString();
                        //        orderno = dr["orderno"].ToString();
                        //        brokerref = dr["brokerref"].ToString();
                        //        int orderattr = (int)Convert.ToInt64(orderattribute);

                        //        int quant = (int)Convert.ToInt64(quantity);

                        //        int newquantity = 0;
                        //            newquantity = orderattr - quant;
                        //        if ( newquantity  > 0)
                        //        {
                        //            SqlConnection con25 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        //            quantity = er.OrderQty.getValue().ToString();
                        //            //string query25 = " insert into  [testcds_ROUTER].[dbo].[Pre_Order_Live] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier], [CDS_AC_No], [Trader], [Broker_Code],[OrderAttribute])values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','NEW' ,'" + newquantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "', '" + account + "','" + trader + "','" + brokercode + "','"+ newquantity + "')  ";
                        //            string query25 = "insert into testcds_router.dbo.pre_order_live (OrderType,Company,SecurityType,CDS_AC_No,Broker_Code,Client_Type,Tax,Shareholder,ClientName,TotalShareHolding,OrderStatus,Create_date,Deal_Begin_Date ,Expiry_Date,Quantity,BasePrice,AvailableShares,OrderPref,OrderAttribute,Marketboard,TimeInForce,OrderQualifier,BrokerRef ,ContraBrokerId ,MaxPrice  ,MiniPrice,Flag_oldorder,OrderNumber,Currency ,FOK ,Affirmation,trading_platform ,Symbol ,Custodian,Source,borrowStatus,AmountValue ,Source_of_Funds,Purpose_of_Investment,side) SELECT [OrderType] ,[Company] ,[SecurityType] ,[CDS_AC_No] ,[Broker_Code],[Client_Type],[Tax] ,[Shareholder],[ClientName] ,[TotalShareHolding],'NEW',[Create_date],[Deal_Begin_Date],[Expiry_Date] ,'" + newquantity + "' ,BasePrice ,[AvailableShares],[OrderPref] ,'" + newquantity + "',[Marketboard] ,[TimeInForce],[OrderQualifier],[BrokerRef]+'_" + partialcount(orderno).ToString() + "' ,[ContraBrokerId] ,[MaxPrice],[MiniPrice] ,[Flag_oldorder],[OrderNumber],[Currency] ,[FOK],[Affirmation],[trading_platform] ,[Symbol],Custodian, NULL,NULL, NULL, NULL, NULL,side   FROM [testcds_ROUTER].[dbo].[Pre_Order_Live] where orderno='" + orderno + "'";


                        //            SqlCommand cmd25 = new SqlCommand(query25, con25);
                        //            con25.Open();
                        //            cmd25.ExecuteNonQuery();
                        //            con25.Close();



                        //        }


                        //    }
                        //}

                        //  market demand market  supply 
                        quantity = er.OrderQty.getValue().ToString();
                        //timeinforce = er.TimeInForce.getValue().ToString();

                        securitycode = er.SecurityID.getValue().ToString();
                        side = er.Side.getValue().ToString();
                        Price = er.Price.getValue().ToString();
                        maturitydate = er.MaturityDate.getValue().ToString();
                        //securitytype = er.SecurityType.getValue().ToString();

                        SqlConnection con24 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

                        string query21 = " insert into  [testcds_ROUTER].[dbo].[Live_Orders] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier], [CDS_AC_No], Trader, Broker_Code)values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','MATCHED ORDER' ,'" + quantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "', '" + account + "','" + trader + "','" + brokercode + "')  ";
                        SqlCommand cmd21 = new SqlCommand(query21, con24);
                        con24.Open();
                        cmd21.CommandTimeout = 0;
                        cmd21.ExecuteNonQuery();
                        con24.Close();

                    }
                    else if (orderstatus == "1")
                    {

                        quantity = er.CumQty.getValue().ToString();
                        //  timeinforce = er.TimeInForce.getValue().ToString();
                        account = er.Account.getValue().ToString();
                        securitycode = er.SecurityID.getValue().ToString();
                        side = er.Side.getValue().ToString();
                        Price = er.Price.getValue().ToString();
                        //  maturitydate = er.MaturityDate.getValue().ToString();
                        securitytype = er.SecurityType.getValue().ToString();
                        account = er.Account.getValue().ToString();
                        leavesquantity = er.LeavesQty.getValue().ToString();
                        grosstradeamt = er.GrossTradeAmt.getValue().ToString();
                        //  matchedprice = er.LastPx.getValue().ToString();
                        matcheddate = er.TradeDate.getValue().ToString();

                        bool result;
                        result = checkorder(orderidentifier);
                        if (result == true)
                        {


                            //SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                            //string query14 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='MATCHED ORDER' ,exchange_orderNumber ='" + orderidentifier + "', Quantity = '" + quantity + "', LeavesQuantity = '" + leavesquantity + "' ,[MatchedPrice]='" + matchedprice + "',[MatchedDate]='" + matcheddate + "' where OrderNumber = '" + orderidentifier + "' and  OrderStatus ='NEW'";
                            //SqlCommand cmd14 = new SqlCommand(query14, con14);
                            //con14.Open();
                            //cmd14.ExecuteNonQuery();
                            //con14.Close();



                            string orderattribute = "";

                            //SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                            //string query = "  select  top 1 OrderAttribute,orderno, brokerref from testcds_ROUTER.dbo.Pre_Order_Live  where OrderNumber = '" + orderidentifier + "' and  OrderStatus ='MATCHED ORDER'  order by orderno desc";

                            //SqlCommand cmd = new SqlCommand(query, con3);

                            //SqlDataAdapter da = new SqlDataAdapter(cmd);
                            //DataSet ds = new DataSet("ds");

                            //da.Fill(ds);

                            //DataTable dt = ds.Tables[0];
                            //if (dt.Rows.Count > 0)
                            //{
                            //    foreach (DataRow dr in dt.Rows)
                            //    {

                            //        // insert prices into table u created
                            //        orderattribute = dr["OrderAttribute"].ToString();
                            //        orderno = dr["orderno"].ToString();
                            //        brokerref = dr["brokerref"].ToString();
                            //        int orderattr = (int)Convert.ToInt64(orderattribute);

                            //        int quant = (int)Convert.ToInt64(quantity);

                            //        int newquantity = 0;
                            //        newquantity = orderattr - quant;
                            //        if (newquantity > 0)
                            //        {
                            //            SqlConnection con25 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                            //            quantity = er.OrderQty.getValue().ToString();
                            //            //string query25 = " insert into  [testcds_ROUTER].[dbo].[Pre_Order_Live] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier], [CDS_AC_No], [Trader], [Broker_Code],[OrderAttribute])values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','NEW' ,'" + newquantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "', '" + account + "','" + trader + "','" + brokercode + "','"+ newquantity + "')  ";
                            //            string query25 = "insert into testcds_router.dbo.pre_order_live (OrderType,Company,SecurityType,CDS_AC_No,Broker_Code,Client_Type,Tax,Shareholder,ClientName,TotalShareHolding,OrderStatus,Create_date,Deal_Begin_Date ,Expiry_Date,Quantity,BasePrice,AvailableShares,OrderPref,OrderAttribute,Marketboard,TimeInForce,OrderQualifier,BrokerRef ,ContraBrokerId ,MaxPrice  ,MiniPrice,Flag_oldorder,OrderNumber,Currency ,FOK ,Affirmation,trading_platform ,Symbol ,Custodian,Source,borrowStatus,AmountValue ,Source_of_Funds,Purpose_of_Investment,side) SELECT [OrderType] ,[Company] ,[SecurityType] ,[CDS_AC_No] ,[Broker_Code],[Client_Type],[Tax] ,[Shareholder],[ClientName] ,[TotalShareHolding],'NEW',[Create_date],[Deal_Begin_Date],[Expiry_Date] ,'" + newquantity + "' ,BasePrice ,[AvailableShares],[OrderPref] ,'" + newquantity + "',[Marketboard] ,[TimeInForce],[OrderQualifier],[BrokerRef]+'_" + partialcount(orderno).ToString() + "' ,[ContraBrokerId] ,[MaxPrice],[MiniPrice] ,[Flag_oldorder],[OrderNumber],[Currency] ,[FOK],[Affirmation],[trading_platform] ,[Symbol],Custodian, NULL,NULL, NULL, NULL, NULL,side   FROM [testcds_ROUTER].[dbo].[Pre_Order_Live] where orderno='" + orderno + "'";


                            //            SqlCommand cmd25 = new SqlCommand(query25, con25);
                            //            con25.Open();
                            //            cmd25.ExecuteNonQuery();
                            //            con25.Close();


                            //        }


                            //    }
                            //}

                        }

                        //securitytype = er.SecurityType.getValue().ToString();
                        if (timeinforce == "0")
                        {
                            force = "Day";
                        }
                        if (timeinforce == "1")
                        {
                            force = "Good Till Cancel (GTC)";
                        }
                        if (timeinforce == "2")
                        {
                            force = "At the Opening (OPG)";
                        }
                        if (timeinforce == "3")
                        {
                            force = "Immediate or Cancel (IOC)";
                        }
                        if (timeinforce == "4")
                        {
                            force = "Fill or Kill (FOK)";
                        }
                        if (timeinforce == "5")
                        {
                            force = "Good Till Crossing (GTX)";
                        }
                        if (timeinforce == "6")
                        {
                            force = "Good Till Date";
                        }
                        SqlConnection con24 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        quantity = er.OrderQty.getValue().ToString();
                        string query21 = " insert into  [testcds_ROUTER].[dbo].[Live_Orders] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier],[CDS_AC_No],[OrderAttribute] )values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','PARTIALLY  MATCHED' ,'" + quantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "', '" + account + "', '" + leavesquantity + "')  ";
                        SqlCommand cmd21 = new SqlCommand(query21, con24);
                        con24.Open();
                        cmd21.CommandTimeout = 0;
                        cmd21.ExecuteNonQuery();
                        con24.Close();

                    }
                    else if (orderstatus == "5")

                    {
                        ordernumber = er.OrderID.getValue().ToString();
                        orderidentifier = er.ClOrdID.getValue().ToString();
                        //   quantity = er.OrderQty.getValue().ToString();
                        //  timeinforce = er.TimeInForce.getValue().ToString();
                        account = er.Account.getValue().ToString();
                        securitycode = er.SecurityID.getValue().ToString();
                        side = er.Side.getValue().ToString();
                        Price = er.Price.getValue().ToString();
                        //
                        //maturitydate = er.MaturityDate.getValue().ToString();
                        securitytype = er.SecurityType.getValue().ToString();

                        //SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        //string query14 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='REPLACED', exchange_orderNumber ='" + orderidentifier + "' where OrderNumber LIKE '%" + orderidentifier + "%'";
                        //SqlCommand cmd14 = new SqlCommand(query14, con14);
                        //con14.Open();
                        //cmd14.ExecuteNonQuery();
                        //con14.Close();



                        quantity = er.OrderQty.getValue().ToString();
                        timeinforce = er.TimeInForce.getValue().ToString();

                        securitycode = er.SecurityID.getValue().ToString();
                        side = er.Side.getValue().ToString();
                        Price = er.Price.getValue().ToString();
                        maturitydate = er.MaturityDate.getValue().ToString();
                        //securitytype = er.SecurityType.getValue().ToString();
                        if (timeinforce == "0")
                        {
                            force = "Day";
                        }
                        if (timeinforce == "1")
                        {
                            force = "Good Till Cancel (GTC)";
                        }
                        if (timeinforce == "2")
                        {
                            force = "At the Opening (OPG)";
                        }
                        if (timeinforce == "3")
                        {
                            force = "Immediate or Cancel (IOC)";
                        }
                        if (timeinforce == "4")
                        {
                            force = "Fill or Kill (FOK)";
                        }
                        if (timeinforce == "5")
                        {
                            force = "Good Till Crossing (GTX)";
                        }
                        if (timeinforce == "6")
                        {
                            force = "Good Till Date";
                        }
                        SqlConnection con24 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

                        string query21 = " insert into  [testcds_ROUTER].[dbo].[Live_Orders] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier])values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','REPLACED' ,'" + quantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "')  ";
                        SqlCommand cmd21 = new SqlCommand(query21, con24);
                        con24.Open();
                        cmd21.CommandTimeout = 0;
                        cmd21.ExecuteNonQuery();
                        con24.Close();
                    }
                    else if (orderstatus == "4")

                    {
                        ordernumber = er.OrderID.getValue().ToString();
                        orderidentifier = er.ClOrdID.getValue().ToString();
                        //     quantity = er.OrderQty.getValue().ToString();
                        //  timeinforce = er.TimeInForce.getValue().ToString();

                        //securitycode = er.SecurityID.getValue().ToString();
                        //side = er.Side.getValue().ToString();
                        //Price = er.Price.getValue().ToString();
                        //  maturitydate = er.MaturityDate.getValue().ToString();
                        //  securitytype = er.SecurityType.getValue().ToString();

                        SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query14 = "Update TOP (1) [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='CANCELLED' ,exchange_orderNumber ='" + orderidentifier + "' where OrderNumber = '" + orderidentifier + "' and OrderStatus = 'NEW'";
                        SqlCommand cmd14 = new SqlCommand(query14, con14);
                        con14.Open();
                        cmd14.CommandTimeout = 0;
                        cmd14.ExecuteNonQuery();
                        con14.Close();



                        //   quantity = er.OrderQty.getValue().ToString();
                        ////   timeinforce = er.TimeInForce.getValue().ToString();

                        //   securitycode = er.SecurityID.getValue().ToString();
                        //   side = er.Side.getValue().ToString();
                        //   Price = er.Price.getValue().ToString();
                        //   maturitydate = er.MaturityDate.getValue().ToString();
                        //securitytype = er.SecurityType.getValue().ToString();
                        if (timeinforce == "0")
                        {
                            force = "Day";
                        }
                        if (timeinforce == "1")
                        {
                            force = "Good Till Cancel (GTC)";
                        }
                        if (timeinforce == "2")
                        {
                            force = "At the Opening (OPG)";
                        }
                        if (timeinforce == "3")
                        {
                            force = "Immediate or Cancel (IOC)";
                        }
                        if (timeinforce == "4")
                        {
                            force = "Fill or Kill (FOK)";
                        }
                        if (timeinforce == "5")
                        {
                            force = "Good Till Crossing (GTX)";
                        }
                        if (timeinforce == "6")
                        {
                            force = "Good Till Date";
                        }
                        SqlConnection con24 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        //  quantity = er.OrderQty.getValue().ToString();
                        string query21 = " insert into  [testcds_ROUTER].[dbo].[Live_Orders] ([Company],[SecurityType],[Create_date], [OrderStatus],[Quantity] ,[BasePrice],[TimeInForce],[MaturityDate],[Side],[OrderIdentifier])values ('" + securitycode + "', '" + securitytype + "','" + DateTime.Now.ToString() + "','CANCELLED' ,'" + quantity + "','" + Price + "','" + force + "','" + maturitydate + "', '" + orderside + "', '" + orderidentifier + "')  ";
                        SqlCommand cmd21 = new SqlCommand(query21, con24);
                        con24.Open();
                        cmd21.CommandTimeout = 0;
                        cmd21.ExecuteNonQuery();
                        con24.Close();


                        SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query = "select TOP(1) Shareholder, BrokerRef, Amount, POL.Quantity as qnty, POL.BasePrice as Bprice, *from[testcds_ROUTER].[dbo].[Pre_Order_Live] POL join[CDSC].[dbo].[CashTrans] CT on POL.BrokerRef = CT.Reference where POL.OrderNumber = '" + orderidentifier + "' and POL.OrderStatus = 'CANCELLED'  and POL.SIDE = 'BUY'";

                        SqlCommand cmd = new SqlCommand(query, con3);

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataSet ds = new DataSet("ds");

                        da.Fill(ds);

                        DataTable dt = ds.Tables[0];
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dt.Rows)
                            {

                                // insert prices into table u created
                                BrokerRef = dr["BrokerRef"].ToString();
                                // AmountValue = dr["Amount"].ToString();
                                Shareholder = dr["Shareholder"].ToString();
                                qnty = dr["qnty"].ToString();
                                Bprice = dr["Bprice"].ToString();

                                qnt = Convert.ToDouble(qnty);
                                pric = Convert.ToDouble(Bprice);

                                amt = qnt * pric * 1.015;

                                AmountValue = Convert.ToString(amt);

                                double totalamount = 0.0;
                                totalamount = Convert.ToDouble(AmountValue);
                                // totalamount = totalamount * -1;
                                AmountValue = totalamount.ToString();
                                SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                                string query1 = " insert into [CDSC].[dbo].[CashTrans]([Description],[TransType],[Amount],[DateCreated],[CDS_Number],[Reference]) values ('Cash Refund','Refund','" + AmountValue + "',getdate(),'" + Shareholder + "','" + BrokerRef + "')    ";
                                SqlCommand cmd1 = new SqlCommand(query1, con4);
                                con4.Open();
                                cmd1.CommandTimeout = 0;
                                cmd1.ExecuteNonQuery();
                                con4.Close();
                            }
                        }
                    }





                }



                if (message is SecurityDefinition)
                {
                    Trace.WriteLine("## FromApp: " + message);
                    Log("## FromApp: " + message);
                    QuickFix.FIX50.SecurityDefinition tr = new QuickFix.FIX50.SecurityDefinition();
                    tr = (QuickFix.FIX50.SecurityDefinition)message;
                    string company, companyid, ISIN;
                    //insert company name and company id 
                    companyid = tr.SecurityID.getValue().ToString();
                    company = tr.Symbol.getValue().ToString();
                    securitytype = tr.SecurityType.getValue().ToString();
                    ISIN = tr.NoSecurityAltID.getValue().ToString();
                    //  Trace.WriteLine("Security Definition" + message);

                    var sidesGrp1 = new QuickFix.FIX50.SecurityDefinition.NoSecurityAltIDGroup();
                    ISIN = tr.GetGroup(1, sidesGrp1).GetString(455).ToString();





                    //select company from the same table where company id = securitycode
                    SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

                    string query = "select company from  [testcds_Router].[dbo].[para_company] where Symbol = '" + company + "'";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.CommandTimeout = 0;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet("ds");

                    da.Fill(ds);

                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {

                            SqlConnection con1 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                            string query1 = "    update [testcds_Router].[dbo].[para_company] set [Company] ='" + companyid + "' , [Symbol] =  '" + company + "',Fnam = '" + company + "',[exchange] ='LUSE', [SecurityType]= '" + securitytype + "', [Date_created] =getdate(),[ISIN_No]= '" + ISIN + "' WHERE  Symbol = '" + company + "'";
                            SqlCommand cmd1 = new SqlCommand(query1, con1);
                            con1.Open();
                            cmd1.ExecuteNonQuery();
                            con.Close();



                        }
                    }
                    else
                    {
                        SqlConnection con1 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query1 = "    insert [testcds_Router].[dbo].[para_company] ([Company], [Symbol] ,[exchange], [SecurityType], [Date_created],Fnam,[ISIN_No]) values ('" + companyid + "', '" + company + "', 'LUSE','" + securitytype + "',getdate(),'" + company + "', '" + ISIN + "' )";
                        SqlCommand cmd1 = new SqlCommand(query1, con1);
                        con1.Open();
                        cmd1.CommandTimeout = 0;
                        cmd1.ExecuteNonQuery();
                        con.Close();

                    }
                }

                else if (message is MarketDataSnapshotFullRefresh)
                {
                    Trace.WriteLine("## FromApp: " + message);
                    Log("## FromApp: " + message);

                    //Log("## FromApp: " + mess                    age);
                    string VWAP_PRICE, closingprice;
                    string openingprice = "";
                    string settlementprice = "";
                    string SessionHighestPrice = "";
                    string SessionLowestPrice = "";
                    string company = "";


                    string tradeVolume = "";
                    string openInterest = "";

                    QuickFix.FIX50.MarketDataSnapshotFullRefresh er = new QuickFix.FIX50.MarketDataSnapshotFullRefresh();
                    er = (QuickFix.FIX50.MarketDataSnapshotFullRefresh)message;
                    MaturityDate maturity = new MaturityDate();
                    MDEntryPx mDEntryPx = new MDEntryPx();
                    //company = er.Symbol.getValue().ToString();

                    securitycode = er.SecurityID.getValue().ToString();
                    maturitydate = er.MaturityDate.getValue().ToString();
                    securitytype = er.SecurityType.getValue().ToString();
                    company = securitycode;
                    //writetoerrorfile(er.ToString());
                    QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup noMDEntries = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    noMDEntries = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(1, noMDEntries);
                    VWAP_PRICE = noMDEntries.Get(mDEntryPx).ToString();
                    QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup opening = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    opening = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(2, opening);
                    openingprice = opening.Get(mDEntryPx).ToString();
                    QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup closing = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    closing = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(4, closing);
                    closingprice = closing.Get(mDEntryPx).ToString();

                    QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup settlement = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    settlement = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(5, settlement);
                    settlementprice = settlement.Get(mDEntryPx).ToString();

                    QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup high = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    high = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(6, high);
                    SessionHighestPrice = high.Get(mDEntryPx).ToString();
                    QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup low = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    low = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(7, low);
                    SessionLowestPrice = low.Get(mDEntryPx).ToString();








                    //QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup tradevolume = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    //tradevolume = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(B, tradevolume);
                    //tradeVolume = tradevolume.Get(mDEntryPx).ToString();
                    //select company from the same table where company id = securitycode



                    QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup openinterest = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
                    openinterest = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(9, openinterest);
                    openInterest = openinterest.Get(mDEntryPx).ToString();


                    SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    string query = "select company from [testcds_ROUTER].[dbo].CompanyPrices where Company = '" + securitycode + "'";

                    SqlCommand cmd = new SqlCommand(query, con3);
                    cmd.CommandTimeout = 0;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet("ds");

                    da.Fill(ds);

                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {

                            // insert prices into table u created


                            SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                            string query1 = "    update [testcds_ROUTER].[dbo].CompanyPrices set VwapPrice = '" + VWAP_PRICE + "',OpeningPrice = '" + openingprice + "',ClosingPrice = '" + closingprice + "' ,Settlementprice = '" + settlementprice + "', HighestPrice = '" + SessionHighestPrice + "',LowestPrice ='" + SessionLowestPrice + "', SecurityType = '" + securitytype + "',[ShareVOL ]= '" + tradeVolume + "', [Openinterest] = '" + openInterest + "', maturitydate = '" + maturitydate + "'  where Company ='" + securitycode + "'";
                            SqlCommand cmd1 = new SqlCommand(query1, con4);
                            con4.Open();
                            cmd1.CommandTimeout = 0;
                            cmd1.ExecuteNonQuery();
                            con4.Close();
                        }
                    }
                    else
                    {
                        SqlConnection con4 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query1 = " insert into [testcds_ROUTER].[dbo].CompanyPrices (VwapPrice, OpeningPrice,ClosingPrice, Settlementprice,HighestPrice, LowestPrice, COMPANY, SecurityType,[ShareVOL ],[Openinterest], maturitydate) values ('" + VWAP_PRICE + "', '" + openingprice + "', '" + closingprice + "' ,  '" + settlementprice + "',   '" + SessionHighestPrice + "', '" + SessionLowestPrice + "' ,'" + securitycode + "','" + securitytype + "', '" + tradeVolume + "', '" + openInterest + "', '" + maturitydate + "' )  ";
                        SqlCommand cmd1 = new SqlCommand(query1, con4);
                        con4.Open();
                        cmd1.CommandTimeout = 0;
                        cmd1.ExecuteNonQuery();
                        con4.Close();
                    }
                    con3.Close();
                    //Trace.WriteLine("## FromAdmin: " + "received tt");

                }


                string msgType = message.Header.GetField(Tags.MsgType);
                //string field = message.GetField(35);
                Trace.WriteLine("## PARTYDETAILSLISTREPORT: " + msgType.ToString());
                string clientname = "";
                string broker = "";
                string tradingaccount = "";
                string csdaccount = "";

                if (message.Header.GetField(Tags.MsgType) == MsgType.PARTYDETAILSLISTREPORT)
                {
                    Trace.WriteLine("## FromApp: " + message);
                    Log("## FromApp: " + message);

                    SqlConnection con11 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    string query11 = "      insert into [CDS_ROUTER].[dbo].[MessageLog]([message], [TimeStamp]  ) values('" + message.ToString() + "', getdate()) ";
                    SqlCommand cmd11 = new SqlCommand(query11, con11);
                    con11.Open();
                    cmd11.CommandTimeout = 0;
                    cmd11.ExecuteNonQuery();
                    con11.Close();
                    // Check if tag 453 (NoPartyIDs) exists before using GetGroup()
                    //if (message.IsSetField(Tags.NoPartyIDs))
                    //{
                    //   // int partyCount = message.GetInt(Tags.NoPartyIDs);
                       

                    //    // Loop through all Party groups
                    //    //for (int i = 1; i <= partyCount; i++)
                    //    //{
                    //        Group group2 = message.GetGroup(1, Tags.NoPartyIDs);

                    //        tradingaccount = group2.GetField(Tags.PartyID).ToString();
                            

                    //        writetoerrorfile("trading account"  + tradingaccount);
                        
                    //        Group group3 = message.GetGroup(2, Tags.NoPartyAltIDs);
                    //        clientname = group3.GetField(Tags.PartyAltID).ToString();
                    //      broker = message.GetGroup(3, Tags.PartyAltID).ToString();
                    //        writetoerrorfile("clientname" + clientname);
                    //       writetoerrorfile(" broker " + broker);
                    //    //}
                       
                    //}

                    //if (message.Header.GetField(Tags.MsgType) == MsgType.PARTYDETAILSLISTREPORT)
                    //{
                    //    Trace.WriteLine("## FromApp: " + message);
                    //    Log("## FromApp: " + message);
                    //    //writetoerrorfile(" message " + message);
                    //    PartyID partyID = new PartyID();

                    //    Group group2;
                    //    NoPartyIDs num = new NoPartyIDs();
                    //    int partyCount = message.GetInt(Tags.NoPartyIDs);
                    //    for (int i = 1; i <= partyCount; i++)
                    //    {
                    //        group2 = message.GetGroup(i, Tags.NoPartyIDs);

                    //        tradingaccount = group2.GetField(Tags.PartyID).ToString();
                    //        writetoerrorfile("trading account " + group2 + "  " + tradingaccount);
                    //        Group group3;

                    //        group3 = message.GetGroup(i, Tags.NoPartyAltIDs);
                    //        clientname = group3.GetField(Tags.PartyAltID).ToString();
                    //        //broker = message.GetGroup(3, Tags.PartyAltID).ToString();
                    //        //writetoerrorfile(" num " + num);
                    //        writetoerrorfile("client name " + group3 + "  " + clientname);
                    //        //writetoerrorfile( " msgType " + msgType);
                    //        group3 = message.GetGroup(2, Tags.NoPartyAltIDs);
                    //        csdaccount = group3.GetField(Tags.PartyAltID).ToString();
                    //        //Trace.WriteLine("## PARTYDETAILSLISTREPORT: " + message.ToString());
                    //        writetoerrorfile("csdaccount " + group3 + "  " + csdaccount);
                    //        group3 = message.GetGroup(3, Tags.NoPartyAltIDs);
                    //        broker = group3.GetField(Tags.PartyAltID).ToString();
                    //        writetoerrorfile("broker " + group3 + "  " + broker);


                    //SqlConnection con1 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    //string query1 = "      insert into[CDS_ROUTER].[dbo].[Trading_Accounts]([CDS_Number]    ,[Trading_Account]  ,[Broker_code]  ,[Client_Name]  ,[Date_Created] ) values('" + csdaccount + "', '" + tradingaccount + "', '" + broker + "', '" + clientname + "', getdate()) ";
                    //SqlCommand cmd1 = new SqlCommand(query1, con1);
                    //con1.Open();
                    //cmd1.CommandTimeout = 0;
                    //cmd1.ExecuteNonQuery();
                    //con1.Close();
                    //    }

                    //    //    //    }
                    //    //    //}
                    //    //    //else


                    //    //    //{
                    //    //    //    SqlConnection con1 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    //    //    //    string query1 = "     update [CDS_ROUTER].[dbo].[Accounts_Clients] set Trading_Account_Number = '" + tradingaccount + "', BrokerCode = '" + broker + "', TradingStatus = 'Trading' where CSD_Account_Number = '" + csdaccount + "'  ";
                    //    //    //    SqlCommand cmd1 = new SqlCommand(query1, con1);
                    //    //    //    con1.Open();
                    //    //    //    cmd1.ExecuteNonQuery();
                    //    //    //    con1.Close();

                    //    //    //}

                    //    //}

                    //}

                    //if (awsmFld == MsgType.PARTYDETAILSLISTREPORT)
                    //{
                    //    writetoerrorfile(" awsmFld " + awsmFld);
                    //    clientname = message.GetField(Tags.PartyAltID).ToString();
                    //   broker = message.GetField(Tags.PartyAltID).ToString();
                    //  tradingaccount = message.GetField(Tags.PartyID).ToString();
                    //    csdaccount = message.GetField(Tags.PartyAltID).ToString();

                    //    writetoerrorfile(" tareader mesaage");
                    //}

                    //if (message is BusinessMessageReject)
                    //      {
                    //          QuickFix.FIX50.BusinessMessageReject er = new QuickFix.FIX50.ExecBusinessMessageRejectutionReport();
                    //          er = (QuickFix.FIX50.BusinessMessageReject)message;
                    //          orderstatus = er.OrdStatus.getValue().ToString();
                    //          if (orderstatus == "0")
                    //          {
                    //              ordernumber = er.OrderID.getValue().ToString();
                    //              orderidentifier = er.ClOrdID.getValue().ToString();

                    //          }
                }
            }
            catch (Exception ex)
            {
                writetoerrorfile(ex.ToString());
                Trace.WriteLine("## Error: " + ex.ToString());
                Log("## Error: " + ex.ToString());

            }

        }

        /// <summary>
        /// this method is called whenever a new session is created.
        /// </summary>
        /// <param name="sessionID"></param>
        /// 


        public string genordernumber(string brokerref)
        {
            string ordernumber = "";

            SqlConnection con3 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
            string query = "select ordernumber from testcds_router.dbo.pre_order_live where orderno='" + brokerref + "'";
            SqlCommand cmd = new SqlCommand(query, con3);
            cmd.CommandTimeout = 0;
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet("ds");

            da.Fill(ds);

            DataTable dt = ds.Tables[0];
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {

                    // insert prices into table u created
                    ordernumber = dr["ordernumber"].ToString();
                  
                }
            }

            return ordernumber;
        }


        public string originaorderid(string brokerref)
        {

            string sql2 = "select orderno from testcds_router.dbo.pre_order_live where brokerref='" + brokerref + "'";
            SqlConnection connection2 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
            SqlDataAdapter dataadapter2 = new SqlDataAdapter(sql2, connection2);
            DataSet ds1 = new DataSet();
            ds1.Clear();
            connection2.Open();
            dataadapter2.Fill(ds1, "start2");
            connection2.Close();

            DataTable dt = ds1.Tables[0];
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr2 in dt.Rows)
                {
                    return dr2["OrderNo"].ToString();
                }
            }
            else
            {
                return "";

            }
            return "";

 

            
        }
        public string partialcount(object originalorderid)
        {
          
            string sql2 = "select isnull(count(*),0)+2 as nxtorder from [CDS_ROUTER].[dbo].[tbl_partials] where originalorder='" + originalorderid + "'";
            SqlConnection connection2 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
            SqlDataAdapter dataadapter2 = new SqlDataAdapter(sql2, connection2);
            DataSet ds1 = new DataSet();
            ds1.Clear();
            connection2.Open();
            dataadapter2.Fill(ds1, "start2");
            connection2.Close();

 

            DataTable dt = ds1.Tables[0];
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr2 in dt.Rows)
                {
                    return dr2["nxtorder"].ToString();
                }
            }
            else
            {
                return "";

            }
            return "";
        }
        public void inserttocharges (string Transactioncode, string chargename,string totalcharges)
        {
            SqlConnection con6 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
            string query1 = " insert into BO.dbo.TransactionCharges (transactionCode, chargecode, Charges, [Date]) values ('" + Transactioncode + "','" + chargename + "','" + totalcharges + "',getdate())  ";
            SqlCommand cmd1 = new SqlCommand(query1, con6);
            con6.Open();
            cmd1.ExecuteNonQuery();
            con6.Close();
        }

        public void updatecharges(string Transactioncode, string chargename, string totalcharges)
        {
            SqlConnection con6 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
            string query1 = " update   BO.dbo.TransactionCharges set charges ='"+ totalcharges + "' where transactionCode ='"+ Transactioncode + "' and  chargecode ='"+ chargename + "' ";
            SqlCommand cmd1 = new SqlCommand(query1, con6);
            con6.Open();
            cmd1.ExecuteNonQuery();
            con6.Close();
        }


        public Boolean checkorder (string orderidentifier)
        {
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

            string query = "SELECT * FROM [testcds_ROUTER].[dbo].[Pre_Order_Live]where OrderNumber like '%" + orderidentifier + "%'";
            SqlCommand cmd = new SqlCommand(query, con);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet("ds");

            da.Fill(ds);

            DataTable dt = ds.Tables[0];
            if (dt.Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
       
        }
        public void OnCreate(SessionID sessionID)
        {
            if (OnProgress != null)
                Log(string.Format("Session {0} created", sessionID));
        }

        /// <summary>
        /// notifies when a successful logon has completed.
        /// </summary>
        /// <param name="sessionID"></param>
        public void OnLogon(SessionID sessionID)
        {
            ActiveSessionId = sessionID;
            Trace.WriteLine(String.Format("==OnLogon: {0}==", ActiveSessionId));

            if (LogonEvent != null)
                LogonEvent();
        }

        /// <summary>
        /// notifies when a session is offline - either from 
        /// an exchange of logout messages or network connectivity loss.
        /// </summary>
        /// <param name="sessionID"></param>
        public void OnLogout(SessionID sessionID)
        {
            // not sure how ActiveSessionID could ever be null, but it happened.
            string a = (ActiveSessionId == null) ? "null" : ActiveSessionId.ToString();
            Trace.WriteLine(String.Format("==OnLogout: {0}==", a));

            if (LogoutEvent != null)
               LogoutEvent();
        }


        /// <summary>
        /// all outbound admin level messages pass through this callback.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sessionID"></param>
        public void ToAdmin(Message message, SessionID sessionID)
        {

            string pass = "Vendors123@";
            //pass = "";
            pass = "VmVuZG9yczEyM0A=";
           //pass = "THUkM0BjN3JANjM=";
            //pass = "Vendors123@";s

            if (message.Header.GetField(Tags.MsgType) == MsgType.LOGON)
            {


              message.SetField(new Password(pass));
            //  message.SetField(new SessionStatus(0));
             // message.SetField(new MsgSeqNum(1));
              message.SetField(new DefaultApplVerID("8"));
             // message.SetField(new ResetSeqNumFlag(false));

             // Session.SendToTarget(message, sessionID);
               }
              
        if (message.Header.GetField(Tags.MsgType)== MsgType.LOGOUT)
        { 
            message.SetField(new SessionStatus(100));
           // message.SetField(new Text("Logout"));
        }
            if (message is MarketDataSnapshotFullRefresh)
            {
                
            }
            Log("To Admin : " + message);
        }

        /// <summary>
        /// all outbound application level messages pass through this callback before they are sent. 
        /// If a tag needs to be added to every outgoing message, this is a good place to do that.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sessionId"></param>
        public void ToApp(Message message, SessionID sessionId)
        {
            Log("To App : " + message);
        }

        #endregion

        public event Action LogonEvent;
        public event Action LogoutEvent;

        public event Action<MarketPrice> OnMarketDataIncrementalRefresh;
        public event Action<string> OnProgress;

        /// <summary>
        /// Triggered on any message sent or received (arg1: isIncoming)
        /// </summary>
        public event Action<Message, bool> MessageEvent;


        public void Log(string message)
        {
            Trace.WriteLine(message);

            if (OnProgress != null)
                OnProgress(message);
        }

        public void Start()
        {
            Log("Application starting....");

            if (Initiator.IsStopped)
                Initiator.Start();
            else
                Log("(already started)");
        }

        public void Stop()
        {
            Log("Stopping.....");

            Initiator.Stop();
        }

        /// <summary>
        /// Tries to send the message; throws if not logged on.
        /// </summary>
        /// <param name="m"></param>
        public void Send(Message m)
        {
            if (Initiator.IsLoggedOn() == false)
                throw new Exception("Can't send a message.  We're not logged on.");
            if (ActiveSessionId == null)
                throw new Exception("Can't send a message.  ActiveSessionID is null (not logged on?).");

            Session.SendToTarget(m, ActiveSessionId);
        }


        public void Subscribe(string symbol, SessionID sessionId)
        {
            string str = "FXD1/2007/015";
            symbol = str;
            String sid = "OQYF";
            string code = "CORP";
            var marketDataRequest = new MarketDataRequest
            {
                MDReqID = new MDReqID(symbol),
                SubscriptionRequestType = new SubscriptionRequestType('1'),
                //incremental refresh
                MarketDepth = new MarketDepth(1), //yes market depth need
                //MDUpdateType = new MDUpdateType(1), //
               
            };
           
            var relatedSymbol = new MarketDataRequest.NoRelatedSymGroup { Symbol = new Symbol(symbol) };
            relatedSymbol.Set(  new SecurityType(code));
            relatedSymbol.Set(new SecurityID(sid));
            marketDataRequest.AddGroup(relatedSymbol);

            var noMdEntryTypes = new MarketDataRequest.NoMDEntryTypesGroup();

            var mdEntryTypeBid = new MDEntryType('5');

            noMdEntryTypes.MDEntryType = mdEntryTypeBid;

            marketDataRequest.AddGroup(noMdEntryTypes);

            noMdEntryTypes = new MarketDataRequest.NoMDEntryTypesGroup();

            var mdEntryTypeOffer = new MDEntryType('3');

            noMdEntryTypes.MDEntryType = mdEntryTypeOffer;

            marketDataRequest.AddGroup(noMdEntryTypes);



            noMdEntryTypes = new MarketDataRequest.NoMDEntryTypesGroup();

             mdEntryTypeOffer = new MDEntryType('2');

            noMdEntryTypes.MDEntryType = mdEntryTypeOffer;

            marketDataRequest.AddGroup(noMdEntryTypes);


            noMdEntryTypes = new MarketDataRequest.NoMDEntryTypesGroup();

            mdEntryTypeOffer = new MDEntryType('1');

            noMdEntryTypes.MDEntryType = mdEntryTypeOffer;

            marketDataRequest.AddGroup(noMdEntryTypes);



            noMdEntryTypes = new MarketDataRequest.NoMDEntryTypesGroup();

            mdEntryTypeOffer = new MDEntryType('8');

            noMdEntryTypes.MDEntryType = mdEntryTypeOffer;

            marketDataRequest.AddGroup(noMdEntryTypes);


            noMdEntryTypes = new MarketDataRequest.NoMDEntryTypesGroup();

            mdEntryTypeOffer = new MDEntryType('0');

            noMdEntryTypes.MDEntryType = mdEntryTypeOffer;

            marketDataRequest.AddGroup(noMdEntryTypes);



            //noMdEntryTypes = new MarketDataRequest.NoMDEntryTypesGroup();

            //mdEntryTypeOffer = new MDEntryType('C');

            //noMdEntryTypes.MDEntryType = mdEntryTypeOffer;

            //marketDataRequest.AddGroup(noMdEntryTypes);
         
            //Send message
            Session.SendToTarget(marketDataRequest, sessionId);
        }
        public void sendSecurityRequest(SessionID sessionId)
        {
            var generator = new RandomGenerator();
            string ciordid = generator.RandomPassword();
            int request = 3;
            QuickFix.FIX50.SecurityDefinitionRequest securityDefition = new QuickFix.FIX50.SecurityDefinitionRequest();
            securityDefition.Set(new SecurityReqID(ciordid));
            securityDefition.Set(new SecurityRequestType(request));
            Session.SendToTarget(securityDefition, sessionId);
          


        }
        public void sendOrder(SessionID sessionId)
        {
            //MS neworder1 = MsgType.PARTYDETAILSLISTREPORT();

            string orderNo, orderType, company, securitytype, cdsacc, brokerCode, expiryday,  acctype ="";
            string  quantity,  price, timeinforce, maturitydate;
            string ordercapacity, securityID,side , Trader, OrderIdentifier = "";
            string stype = "";

            try
            {

                OrderIdentifier = "Limit";

                   SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

                string query = "SELECT top 10 * FROM[testcds_ROUTER].[dbo].[Pre_Order_Live] where OrderStatus = 'OPEN' and OrderNumber not in (select  ordernumber from[testcds_ROUTER].[dbo].[sent_orders] )";
                SqlCommand cmd = new SqlCommand(query, con);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet("ds");

                da.Fill(ds);

                DataTable dt = ds.Tables[0];
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr2 in dt.Rows)
                    {
                        orderNo = dr2["OrderNo"].ToString();
                        orderType = dr2["OrderType"].ToString();
                      

                        securitytype = dr2["SecurityType"].ToString();
                        cdsacc = dr2["CDS_AC_No"].ToString();
                        brokerCode = dr2["Broker_Code"].ToString();
                        company = dr2["Symbol"].ToString();

                        quantity = dr2["Quantity"].ToString();
                        price = dr2["BasePrice"].ToString();
                        timeinforce = dr2["TimeInForce"].ToString();
                
              
               
                         ordercapacity = dr2["OrderCapacity"].ToString();
                     
                        securityID = dr2["Company"].ToString();
                        side = dr2["Side"].ToString();
                        Trader= dr2["Trader"].ToString();
                        OrderIdentifier = dr2["OrderNumber"].ToString();
                        maturitydate = dr2["maturity"].ToString();
                        //         IFormatProvider cul = new CultureInfo ("en-us", true);


                        //         // var  expiry = DateTime.ParseExact(dr["Expiry_Date"],"yyyyddmm", cul);
                        //        //  DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                        //         DateTime oDate = DateTime.Now;


                        expiryday = dr2["Expiry_Date"].ToString();

                        stype = dr2["SecurityType"].ToString();
                        decimal qty, prc;
                        char ord = '0';
                    
                        char cap = ' ';
                        char force = ' ';
                        int num = 2;
                      
                        char type = ' ';
                   
                      
                        QuickFix.FIX50.NewOrderSingle neworder = new QuickFix.FIX50.NewOrderSingle();

                     
                        qty = Convert.ToDecimal(quantity);
                        prc = Convert.ToDecimal(price);
                        DateTime dat = DateTime.Now;

                        //dat = Convert.ToDateTime(createdate);

                        //oDate = Convert.ToDateTime(expiryday);

                        //expiryday = oDate.ToShortDateString();
                   
                        if (side == "BUY")
                        {
                            ord = '1';
                        }
                        else if (side == "SELL")
                        {
                            ord = '2';
                        }
                     
                        if (timeinforce == "Day Order (DO)")
                        {
                            force = '0';
                        }
                        if (timeinforce == "Good Till Cancelled (GTC)")
                        {
                            force = '1';
                        }
                        if (timeinforce == "Good Till Time (GTT)")
                        {
                            force = '6';
                        }
                        if (timeinforce == "Fill or Kill (FOK)")
                        {
                            force = '4';
                        }
                        if (timeinforce == "Immediate/Cancel Order (IOC)")
                        {
                            force = '3';
                        }
                        if (timeinforce == "At the Opening (OPG)")
                        {
                            force = '2';
                        }
                        if (acctype == "Account is carried on customer Side of Books")
                        {
                          
                        }
                        else if (acctype == "Account is carried on non-Customer Side of books")
                        {
                           
                        }
                        else if (acctype == "House Trader")
                        {
                          
                        }
                        else if (acctype == "Floor Trader")
                        {
                           
                        }
                        else if (acctype == "Account is carried on non-customer side of books and is cross margined")
                        {    
                        }
                        else if (acctype == "Account is house trader and is cross margined")
                        {
                          
                        }
                        else if (acctype == "Joint Backoffice Account (JBO)")
                        {
                          
                        }
                        else if (acctype == "Custodian")
                        {
                           
                        }
                        ordercapacity = "Agency";


                            if (ordercapacity == "Agency")
                        {
                            cap = 'A';
                        }
                        else if (ordercapacity == "Principal")
                        {
                            cap = 'P';
                        }



                        type = '2';


                      OrderIdentifier = OrderIdentifier + "A";
                        neworder.Set(new ClOrdID(OrderIdentifier));
                        neworder.Set(new NoPartyIDs(num));
                        QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup noparty = new QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup();
                        noparty.Set(new PartyID(brokerCode));
                        noparty.Set(new PartyRole(1));
                        neworder.AddGroup(noparty);
                        QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup noparty2 = new QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup();
                        noparty2.Set(new PartyID(Trader));
                        noparty2.Set(new PartyRole(12));
                        neworder.AddGroup(noparty2);

                        neworder.Set(new Account(cdsacc));
                        neworder.Set(new OrderQty(qty));
                        neworder.Set(new OrdType(type));
                        neworder.Set(new Price(prc));
                        neworder.Set(new Side(ord));
                        neworder.Set(new Symbol(company));
                        neworder.Set(new SecurityID(securityID));
                        neworder.Set(new SecurityType(stype));

                       
                        neworder.Set(new MaturityDate(maturitydate));

                        neworder.Set(new TransactTime(dat));
                        neworder.Set(new OrderCapacity(cap));
                        neworder.Set(new QuickFix.Fields.Text(OrderIdentifier));
                        neworder.Set(new TimeInForce(force));

                        if (force == '6')
                        {
                           
                         
                            neworder.Set(new ExpireDate(expiryday));
                           
                        }


                        // neworder.Set(new TradeDate(createdate));



                        // neworder.Set(new MaturityDate(createdate));

                        //neworder.Set(new ExpireTime(DateTime.Today));
                        SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query14 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='POSTED'  where OrderNo = '" + orderNo + "'";
                        SqlCommand cmd14 = new SqlCommand(query14, con14);
                        con14.Open();
                        cmd14.ExecuteNonQuery();
                        con14.Close();


                        SqlConnection con15 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query15 = "  insert into [testcds_ROUTER].[dbo].[sent_orders] (ordernumber) values('" + OrderIdentifier + "')";
                        SqlCommand cmd15 = new SqlCommand(query15, con15);
                        con15.Open();
                        cmd15.ExecuteNonQuery();
                        con15.Close();

                        Session.SendToTarget(neworder, sessionId);


                        

                     


                    }
                }
                 

                



       



                    //}
               // }


                   // }
              //  }
            }
            catch (Exception ex)
            {
                // Get stack trace for the exception with source file information
                var st = new StackTrace(ex, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();
                writetoerrorfile(ex.ToString());
                writetoerrorfile("Line number " + line.ToString());
             ;
            }
        }


        public void AmendOrder(SessionID sessionId)
        {
            string  orderType = "BUY";
            string  quantity = "1000",  price = "2.59", timeinforce = "Day Order (DO)";
            try
            {
               
                       
                               
                                decimal qty, prc;
                                char ord = '0';
                               
                             
                                int num = 2;
                             
                                char type = ' ';
                               
                               
                var generator = new RandomGenerator();
                string ciordid = generator.RandomPassword();
                String stype = "CS";
                             
                                qty = Convert.ToDecimal(quantity);
                                prc = Convert.ToDecimal(price);
                               // prc = 16;
                                DateTime dat = DateTime.Now; ;

                                //dat = Convert.ToDateTime(createdate);

                                if (orderType == "BUY")
                                {
                                    ord = '1';
                                }
                                else if (orderType == "SELL")
                                {
                                    ord = '2';
                                }

                                if (timeinforce == "Day Order (DO)")
                                {
                                   
                                }
                                if (timeinforce == "Good Till Cancelled (GTC)")
                                {
                                   
                                }
                                if (timeinforce == "Good Till Time (GTT)")
                                {
                             
                                }
                                if (timeinforce == "Fill Or Kill")
                                {
                                   
                                }
                                if (timeinforce == "Immediate or Cancel (IOC)")
                                {
                                   
                                }
                                if (timeinforce == "At the Opening (OPG)")
                                {
                                   
                                }

                                type = '2';
                                  QuickFix.FIX50.OrderCancelReplaceRequest neworder = new QuickFix.FIX50.OrderCancelReplaceRequest();


                string maturitydate = "20220923";

                neworder.Set(new ClOrdID ("000002"));
                neworder.Set(new OrigClOrdID("100069770"));

                neworder.Set(new NoPartyIDs(num));
                QuickFix.FIX50.OrderCancelReplaceRequest.NoPartyIDsGroup noparty = new QuickFix.FIX50.OrderCancelReplaceRequest.NoPartyIDsGroup();
              
                noparty.Set(new PartyID("SBZL"));
                noparty.Set(new PartyRole(1));
                neworder.AddGroup(noparty);

                QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup noparty2 = new QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup();
            
                noparty2.Set(new PartyID("BWA"));
                noparty2.Set(new PartyRole(12));
                neworder.AddGroup(noparty2);

                neworder.Set(new Symbol("BATA"));
                neworder.Set(new SecurityID("YAGP"));
                neworder.Set(new SecurityType(stype));
                neworder.Set(new MaturityDate(maturitydate));

                neworder.Set(new Account("JWU677"));
                neworder.Set(new OrderQty(qty));
                neworder.Set(new OrdType(type));
                neworder.Set(new Price(prc));
                neworder.Set(new Side(ord));
                neworder.Set(new TransactTime(dat));
                neworder.Set(new TimeInForce('0'));
                neworder.Set(new OrderCapacity('A'));
               
                  
               
                Session.SendToTarget(neworder, sessionId);

                    }
                
            
            catch (Exception ex)
            {
                writetoerrorfile(ex.ToString());
            }
        }
        public void Tradestatus(SessionID sessionId)
        {
            QuickFix.FIX50.TradeCaptureReportRequest neworder = new QuickFix.FIX50.TradeCaptureReportRequest();



            neworder.Set(new TradeRequestID("$UNIQUE"));
            neworder.Set(new TradeRequestType(0));






            Session.SendToTarget(neworder, sessionId);

            neworder.Set(new Side('2'));

            Session.SendToTarget(neworder, sessionId);
        }
        public void Orderstatus(SessionID sessionId)
        {
            QuickFix.FIX50.OrderStatusRequest neworder = new QuickFix.FIX50.OrderStatusRequest();




            neworder.Set(new OrderID("0"));
            neworder.Set(new Side('1'));





            Session.SendToTarget(neworder, sessionId);

            neworder.Set(new Side('2'));

            Session.SendToTarget(neworder, sessionId);
        }
        public void CancelOrder(SessionID sessionId)
        {
           


            string orderNo,exchangeorderno, orderType= "BUY", company, securitytype, cdsacc, brokerCode;
            string  quantity, createdate = "20220923", price, timeinforce ="Day",   board ="ICE", Trader, securityID, side;
            try
            {
                 SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

                string query = "SELECT replace(testcds_router.dbo.GetDateAfter3WorkingDays(getdate()),'-','')       as maturityDate1,*FROM[testcds_ROUTER].[dbo].[Pre_Order_Live]  where OrderStatus='PENDING CANCELLATION'";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.CommandTimeout = 0;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet("ds");

                da.Fill(ds);

                DataTable dt = ds.Tables[0];
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        orderNo = dr["OrderNumber"].ToString();
                        exchangeorderno = dr["exchange_orderNumber"].ToString();
                        orderType = dr["OrderType"].ToString();
                    
                        Trader = dr["Trader"].ToString();
                        brokerCode = dr["Broker_Code"].ToString();

                        createdate = dr["maturityDate1"].ToString();


                        securitytype = dr["SecurityType"].ToString();
                        cdsacc = dr["CDS_AC_No"].ToString();
                      
                        company = dr["Symbol"].ToString();

                        quantity = dr["Quantity"].ToString();
                        price = dr["BasePrice"].ToString();
                        timeinforce = dr["TimeInForce"].ToString();
                   
                        side = dr["Side"].ToString();
             

                        securityID = dr["Company"].ToString();
                  
                                char ord = '1';
                  
                                DateTime dat = DateTime.Now; ;

                      

                                if (orderType == "BUY")
                                {
                                    ord = '1';
                                }
                                else if (orderType == "SELL")
                                {
                                    ord = '2';
                                }

                                if (timeinforce == "Day Order (DO)")
                                {
                                   
                                }
                                if (timeinforce == "Good Till Cancelled (GTC)")
                                {
                            
                                }
                                if (timeinforce == "Good Till Time (GTT)")
                                {
                                  
                                }
                                if (timeinforce == "Fill Or Kill")
                                {
                                    
                                }
                                if (timeinforce == "Immediate or Cancel (IOC)")
                                {
                                   
                                }
                                if (timeinforce == "At the Opening (OPG)")
                                {
                               
                                }


                               
                                else if (board == "Odd lot")
                                {
                           
                                   

                                }
                                QuickFix.FIX50.OrderCancelRequest neworder = new QuickFix.FIX50.OrderCancelRequest();

                        // neworder.Set(new Account(cdsacc));
                        var generator = new RandomGenerator();
                        string ciordid = generator.RandomPassword();

                        neworder.Set(new ClOrdID("$UNIQUE"));
                                neworder.Set(new OrigClOrdID(exchangeorderno));
                QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup noparty = new QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup();
                noparty.Set(new PartyRole(1));
                noparty.Set(new PartyID(brokerCode));
               
         
                neworder.AddGroup(noparty);
                QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup noparty2 = new QuickFix.FIX50.NewOrderSingle.NoPartyIDsGroup();
                noparty2.Set(new PartyRole(12));
                noparty2.Set(new PartyID(Trader));
               
                neworder.AddGroup(noparty2);

                        //neworder.Set(new Price(prc));

                        int Tquantity = (int)Convert.ToInt64(quantity);
                neworder.Set(new Symbol(company));
                neworder.Set(new SecurityID(securityID));
                neworder.Set(new SecurityType(securitytype));
                neworder.Set(new MaturityDate(createdate));
               // neworder.Set(new Account(cdsacc));
              //  neworder.Set(new OrderQty(Tquantity));
                neworder.Set(new Side(ord));
                neworder.Set(new TransactTime(dat));
     

                Session.SendToTarget(neworder, sessionId);

                        SqlConnection con14 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                        string query14 = "Update [testcds_ROUTER].[dbo].[Pre_Order_Live] set OrderStatus ='CancellationSentToAts'  where OrderNumber = '" + orderNo + "' and OrderStatus = 'PENDING CANCELLATION'";
                        SqlCommand cmd14 = new SqlCommand(query14, con14);
                        con14.Open();
                        cmd14.CommandTimeout = 0;
                        cmd14.ExecuteNonQuery();
                        con14.Close();
                        writetoerrorfile("Cancellation done..");
                        //    }
                        //}


                    }
                }
            }
            catch (Exception ex)
            {
                writetoerrorfile(ex.ToString());
            }
        }

       public void partyrequest(SessionID sessionId)
        {
          //  if (message.Header.GetField(Tags.MsgType) == MsgType.PARTYDETAILSLISTREPORT)
                var msg = new Message();
            msg.Header.SetField(new QuickFix.Fields.MsgType("CF"));

            msg.SetField( new PartyDetailsListRequestID("REQ12345")); // Unique request ID
            msg.SetField(new NoPartyListResponseTypes(1));         // Expecting response
    

            msg.SetField(new QuickFix.Fields.PartyDetailsListRequestID("00001"));
      
            msg.SetField(new QuickFix.Fields.PartyListResponseType(0));
           
   // Full list
          //  msg.SetField(new TransactTime(DateTime.UtcNow));

            Session.SendToTarget(msg, sessionId);
            Console.WriteLine("Sent PartyDetailsListRequest");
        }
        public void Securitydefition(SessionID sessionId)
        {

            QuickFix.FIX50.SecurityDefinitionRequest neworder = new QuickFix.FIX50.SecurityDefinitionRequest();

            neworder.Set(new SecurityReqID("10001"));
            neworder.Set(new SecurityRequestType(8));
            Session.SendToTarget(neworder, sessionId);
        }
        public void writetoerrorfile(String mssg)
        {
            string fileName = @"C:\Fix50EscrowEquities\error.txt";
            StreamWriter objWriter3 = new System.IO.StreamWriter(fileName, true);
            objWriter3.WriteLine(mssg);
            objWriter3.Close();
        }
        public void OnMessage(SecurityDefinition message,SessionID session)
        {
            Log("Security Definition: " + message);
            string company, companyid;
            //insert company name and company id 
            companyid = message.SecurityID.getValue().ToString();
            company = message.Symbol.getValue().ToString();
          //  Trace.WriteLine("Security Definition" + message);
          
          



           

            // select company from the same table where company id = securitycode
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

            string query = "select company from CompanyPrices where CompanyCode = '" + companyid + "'";
            SqlCommand cmd = new SqlCommand(query, con);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet("ds");

            da.Fill(ds);

            DataTable dt = ds.Tables[0];
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {

                    // insert prices into table u created


                }
            }
            else
            {
                 SqlConnection con1 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    string query1 = "  insert into CompanyPrices(company,companyCode) VALUES('" + company + "','" + companyid + "')";
                    SqlCommand cmd1 = new SqlCommand(query1, con1);
                    con1.Open();
                    cmd1.ExecuteNonQuery();
                    con.Close();
                
            }
        }
        public void OnMessage(MarketDataSnapshotFullRefresh message, SessionID session)
        {
            Trace.WriteLine("MarketData SnapshotFullRefresh" + message);
            Log("MarketData SnapshotFullRefresh: " + message);
            string securitycode, VWAP_PRICE, maturitydate, closingprice;
            string openingprice = "";
            string settlementprice = "";
            string SessionHighestPrice = "";
            QuickFix.FIX50.MarketDataSnapshotFullRefresh er = new QuickFix.FIX50.MarketDataSnapshotFullRefresh();
            er = (QuickFix.FIX50.MarketDataSnapshotFullRefresh)message;
            MaturityDate maturity = new MaturityDate();
            MDEntryPx mDEntryPx = new MDEntryPx();
            securitycode = er.SecurityID.getValue().ToString();
            maturitydate = er.MaturityDate.getValue().ToString();
            QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup noMDEntries = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
            noMDEntries = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(1, noMDEntries);
            VWAP_PRICE = noMDEntries.Get(mDEntryPx).ToString();
            QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup closing = new QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
            closing = (QuickFix.FIX50.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)er.GetGroup(3, closing);
            closingprice = closing.Get(mDEntryPx).ToString();



            // select company from the same table where company id = securitycode
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);

            string query = "select company from CompanyPrices where CompanyCode = '" + securitycode + "'";
            SqlCommand cmd = new SqlCommand(query, con);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet("ds");

            da.Fill(ds);

            DataTable dt = ds.Tables[0];
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {

                    // insert prices into table u created


                    SqlConnection con1 = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["conpath"]);
                    string query1 = " insert into CompanyPrices(VWAP_PRICE,OpeningPrice,ClosingPrice,Settlementprice,SessionHighestPrice,SessionLowestPrice) VALUES('" + VWAP_PRICE + "','" + openingprice + "','" + closingprice + "' , '" + settlementprice + "','" + SessionHighestPrice + "') where CompanyCode ='" + securitycode + "'";
                    SqlCommand cmd1 = new SqlCommand(query1, con1);
                    con1.Open();
                    cmd1.ExecuteNonQuery();
                    con.Close();
                }
            }

        }

       
        public void OnMessage(MarketDataRequestReject message, SessionID session)
        {
            Trace.WriteLine("MarketDataRequestReject" + message);

            if (MessageEvent != null)
                MessageEvent(message, false);
        }
        public void OnMessage(BusinessMessageReject message,SessionID sesssion)
        {

        }
          
     
        public void OnMessage(MarketDataIncrementalRefresh message, SessionID session)
        {
            var noMdEntries = message.NoMDEntries;
            var listOfMdEntries = noMdEntries.getValue();
            //message.GetGroup(1, noMdEntries);
            var group = new MarketDataIncrementalRefresh.NoMDEntriesGroup();

            Group gr = message.GetGroup(1, group);

            string sym = message.MDReqID.getValue();

            var price = new MarketPrice();


            for (int i = 1; i <= listOfMdEntries; i++)
            {
                group = (MarketDataIncrementalRefresh.NoMDEntriesGroup)message.GetGroup(i, group);

                price.Symbol = group.Symbol.getValue();

                MDEntryType mdentrytype = group.MDEntryType;

                if (mdentrytype.getValue() == '0') //bid
                {
                    decimal px = group.MDEntryPx.getValue();
                    price.Bid = px;
                }
                else if (mdentrytype.getValue() == '1') //offer
                {
                    decimal px = group.MDEntryPx.getValue();
                    price.Offer = px;
                }

                price.TimeStamp = group.MDEntryTime.ToString();
            }

            if (OnMarketDataIncrementalRefresh != null)
            {
                OnMarketDataIncrementalRefresh(price);
            }
        }



    }


    public class RandomGenerator
    {
        // Instantiate random number generator.  
        // It is better to keep a single Random instance 
        // and keep using Next on the same instance.  
        private readonly Random _random = new Random();

        // Generates a random number within a range.      
        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        // Generates a random string with a given size.    
        public string RandomString(int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);

            // Unicode/ASCII Letters are divided into two blocks
            // (Letters 65–90 / 97–122):   
            // The first group containing the uppercase letters and
            // the second group containing the lowercase.  

            // char is a single Unicode character  
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length = 26  

            for (var i = 0; i < size; i++)
            {
                var @char = (char)_random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        // Generates a random password.  
        // 4-LowerCase + 4-Digits + 2-UpperCase  
        public string RandomPassword()
        {
            var passwordBuilder = new StringBuilder();

            // 4-Letters lower case   
            passwordBuilder.Append(RandomString(3));

            // 4-Digits between 1000 and 9999  
            passwordBuilder.Append(RandomNumber(1000, 9999));

            passwordBuilder.Append(RandomString(4));
            // 2-Letters upper case  
            // passwordBuilder.Append(RandomString(2));
            return passwordBuilder.ToString();
        }
    }

}