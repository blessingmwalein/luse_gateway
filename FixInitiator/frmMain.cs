using QuickFix;
using QuickFix.Transport;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;
using Message = QuickFix.Message;

namespace FixInitiator
{
    public partial class frmMain : Form
    {
        public FixClient50Sp2 _client;

        public frmMain()
        {
            InitializeComponent();
        }


        protected override void OnLoad(EventArgs e)
        {
            try
            {


                base.OnLoad(e);


                // FIX app settings and related
                var settings = new SessionSettings("C:\\Fix50EscrowEquities\\Dependency\\Fixinitiator.cfg");

                // FIX application setup
                FileStoreFactory storeFactory = new FileStoreFactory(settings);


                //ScreenLogFactory logFactory = new ScreenLogFactory(settings);
                FileLogFactory logFactory = new FileLogFactory(settings);
                DefaultMessageFactory messageFactory = new DefaultMessageFactory();
                _client = new FixClient50Sp2(settings);

                IInitiator initiator = new SocketInitiator(_client, storeFactory, settings, logFactory);
                _client.Initiator = initiator;

                _client.LogonEvent += ClientLogonEvent;

                _client.OnProgress += _client_OnProgress;

                _client.MessageEvent += ClientMessageEvent;

                //   _client.OnMarketDataIncrementalRefresh += Client_OnMarketDataIncrementalRefresh;

            }
            catch (Exception ex)
            {
                //     writetoerrorfile(e);
                Trace.WriteLine("## FromApp: " + ex.ToString());
            }
        }
        private void TimerCallback(Object o)
        {

            _client.sendOrder(_client.ActiveSessionId);

        }
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            _client.Stop();

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            //_client.onst
            _client.Start();

            //  _client.LogonEvent += ClientLogonEvent;
            //_client.OnProgress += _client_OnProgress;
            // _client.MessageEvent += ClientMessageEvent;

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_client != null && _client.Initiator.IsLoggedOn())
                _client.Initiator.Stop();

            base.OnClosing(e);
        }

        private void _client_OnProgress(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(_client_OnProgress), msg);
                return;
            }

            AddItem(msg);

        }


        private void ClientLogoutEvent()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ClientLogoutEvent), null);
                return;
            }
            AddItem("Log out called");
            enableControls(false);
        }

        private void AddItem(string message)
        {
            if (listBox1.Items.Count > 70)
                listBox1.Items.Clear();

            listBox1.Items.Add(message);
        }

        private void ClientLogonEvent()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ClientLogonEvent), null);
                return;
            }

            AddItem("Logged on");

            enableControls(true);

        }

        private void enableControls(bool enable)
        {
            btnDisconnect.Enabled = enable;
            btnStartMarketPrice.Enabled = enable;
            // btnUpdateSecurities.Enabled = enable;
            btnConnect.Enabled = !enable;
        }

        private void ClientMessageEvent(Message arg1, bool arg2)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Message, bool>(ClientMessageEvent), arg1, arg2);
                return;
            }

            AddItem(arg1.ToString());

        }


        private void Client_OnMarketDataIncrementalRefresh(MarketPrice obj)
        {
            AddItem(obj.ToString());
        }

        private void LogError(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogError), msg);
                return;
            }
            AddItem(msg);
        }

        private void Client_OnMarketDataSnapshotFullRefresh(MarketPrice obj)
        {
            AddItem(obj.ToString());
        }




        private void btnStartMarketPrice_Click(object sender, EventArgs e)
        {

            if (btnStartMarketPrice.Text == "&Start Market Data Request")
            {
                //TODO: this is sample symbol id, it can be replaced by real id

               

                btnStartMarketPrice.Text = "&Stop";

                System.Timers.Timer aTimer = new System.Timers.Timer();
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent1);
                aTimer.Interval = 120000;
                aTimer.Enabled = true;
            }
            else
            {
                btnStartMarketPrice.Text = "&Start Market Data Request";

            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void frmMain_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            //System.Timers.Timer aTimer = new System.Timers.Timer();
            //aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            //aTimer.Interval = 5000;
            //aTimer.Enabled = true;
            timer1.Enabled = true;
           
        }


        public void OnTimedEvent2(object source, ElapsedEventArgs e)
        {
            //_client.Log("received execution report");
            //_client.writetoerrorfile("ordernumber "+ DateTime.DaysInMonth(2022,11).ToString());
            _client.CancelOrder(_client.ActiveSessionId);
            //Console.WriteLine("Hello World!");
            // Trace.WriteLine("Hello World!");
        }

        public void OnTimedEvent1(object source, ElapsedEventArgs e)
        {
            //_client.Log("received execution report");
            //_client.writetoerrorfile("ordernumber "+ DateTime.DaysInMonth(2022,11).ToString());
            // _client.sendOrder(_client.ActiveSessionId);
            //Console.WriteLine("Hello World!");
            // Trace.WriteLine("Hello World!");
            string sym = "MSFT";
            _client.Subscribe(sym, _client.ActiveSessionId);
        }
        public  void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //_client.Log("received execution report");
            //_client.writetoerrorfile("ordernumber "+ DateTime.DaysInMonth(2022,11).ToString());
            _client.sendOrder(_client.ActiveSessionId);
            //Console.WriteLine("Hello World!");
           // Trace.WriteLine("Hello World!");
        }
        private void button2_Click(object sender, EventArgs e)
        {
            _client.Orderstatus(_client.ActiveSessionId);
        }

        private void button3_Click(object sender, EventArgs e)
        {
        

            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent2);
            aTimer.Interval = 5000;
            aTimer.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _client.Securitydefition(_client.ActiveSessionId);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _client.partyrequest(_client.ActiveSessionId);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            _client.Tradestatus(_client.ActiveSessionId);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
      if (    _client.ActiveSessionId != null)
            {
               // _client.writetoerrorfile("testing successful " + DateTime.Now.ToString());
                _client.sendOrder(_client.ActiveSessionId);
            }
      else
            {
                _client.writetoerrorfile("testing failed "+ DateTime.Now.ToString());
            }
         
        }

   

        private void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }
    }

}