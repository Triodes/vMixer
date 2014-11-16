using System;
using System.Net;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Timers;

namespace httpget
{
    class Program : ApplicationContext
    {
        #region Scanning and startup

        SerialPort p;
        WebClient c = new WebClient();
        private System.Timers.Timer timer, keepAlive;
        bool connected = false, vmixOn = true;
        
        static void Main(string[] args)
        {
            Program prog = new Program();
            Application.Run(prog);
        }

        public Program()
        {
            AddTray();
            Scan();
        }

        void Start()
        {
            trayIcon.Icon = new Icon("succes.ico");

            p.DataReceived += p_DataReceived;
            p.ReceivedBytesThreshold = 2;

            timer = new System.Timers.Timer();
            timer.Interval = 20;
            timer.Elapsed += new ElapsedEventHandler(getInfo);
            timer.Start();

            keepAlive = new System.Timers.Timer();
            keepAlive.Interval = 1000;
            keepAlive.Elapsed += new ElapsedEventHandler(KeepAlive);
            keepAlive.Start();
        }

        void Scan()
        {
            connected = true;
            Random r = new Random();
            string[] ports = SerialPort.GetPortNames();
            for (int i = 0; i < ports.Length; i++)
            {
                try
                {
                    p = new SerialPort(ports[i], 9600);
                    p.ReadTimeout = 1000;
                    byte t = (byte)r.Next(255);
                    p.Close();
                    p.Open();
                    p.Write(new byte[2] { 255, t }, 0, 2);
                    int temp = p.ReadByte();
                    if (temp == t)
                    {
                        p.Write(new byte[1] { 1 }, 0, 1);
                        Thread.Sleep(1500);
                        Start();
                        return;
                    }
                }
                catch { p = null; }
            }
            trayIcon.ShowBalloonTip(3000, "Kan niet verbinden!", "Er kan geen verbinding worden gemaakt met de mixer! Check de kabels en klik op 'rescan'. Als het probleem blijft bestaan: Stop het programma, maak de USB-kabel los en weer vast, start het programma opnieuw.", ToolTipIcon.Error);
            trayIcon.Icon = new Icon("error.ico");
            connected = false;
        }

        void Exit()
        {
            Environment.Exit(0);
        }

        #endregion

        #region data handling

        bool flip = false;
        void p_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int type;
            lock (p)
            {
                type = ReadByte();
            }
            if (type == 0)
            {
                int fadeLevel;
                lock (p)
                {
                    fadeLevel = ReadByte();
                }
                if (fadeLevel != -1)
                {
                    Stream st = Query("?Function=SetFader&Value=" + (flip ? 255 - fadeLevel : fadeLevel));
                    st.Close();
                    if (fadeLevel == 255)
                        flip = true;
                    else if (fadeLevel == 0)
                        flip = false;
                }
            }
            else if (type == 1)
            {
                int buttonNr;
                lock (p)
                {
                    buttonNr = ReadByte();
                }
                if (buttonNr < 5)
                {
                    Stream st = Query("?Function=PreviewInput&Input=" + buttonNr);
                    if (st != null) st.Close();
                }
                else
                {
                    Stream st = Query("?Function=FadeToBlack");
                    if (st != null) st.Close();
                }
            }
        }

        int ReadByte()
        {
            lock (p)
            {
                try
                {
                    return p.ReadByte();
                }
                catch 
                {
                    HandleDisconnectArduino();
                }
            }
            return -1;
        }


        int preview = 0, oldPreview = 0, active = 0, oldActive = 0;
        bool ftb = false, ftbOld = false;
        void getInfo(object sender, EventArgs e)
        {
            Stream response = Query();
            if (response != null)
            {
                XmlReader r = XmlReader.Create(response);
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.Element)
                    {
                        if (r.Name == "preview")
                        {
                            r.Read();
                            oldPreview = preview;
                            preview = 1 << (8 - int.Parse(r.Value));

                            if (oldPreview != preview)
                                WriteLedState();
                        }
                        else if (r.Name == "active")
                        {
                            r.Read();
                            oldActive = active;
                            active = 1 << (4 - int.Parse(r.Value));

                            if (oldActive != active)
                                WriteLedState();
                        }
                        else if (r.Name == "fadeToBlack")
                        {
                            r.Read();
                            ftbOld = ftb;
                            ftb = bool.Parse(r.Value);

                            if (ftb != ftbOld)
                            {
                                lock (p)
                                {
                                    try
                                    {
                                        p.Write(new byte[2] { 1, Convert.ToByte(ftb) }, 0, 2);
                                    }
                                    catch
                                    {
                                        HandleDisconnectArduino();
                                    }
                                }
                            }
                        }
                    }
                }
                r.Close();
                response.Close(); 
            }
        }

        void WriteLedState()
        {
            lock (p)
            {
                try
                {
                    p.Write(new byte[2] { 0, (byte)(preview | active) }, 0, 2);
                }
                catch
                {
                    HandleDisconnectArduino();
                }
            }
        }

        void KeepAlive(object sender, EventArgs e)
        {
            lock (p)
            {
                try
                {
                    p.Write(new byte[1] { 125 }, 0, 1);
                }
                catch
                {
                    HandleDisconnectArduino();
                }
            }
        }

        public Stream Query(string urlEnd = "")
        {
            string s = "http://127.0.0.1:8088/api/"+urlEnd;
            Stream str = null;
            lock (c)
            {
                try
                {
                    str = c.OpenRead(s);
                }
                catch
                {
                    HandleDisconnectVmix();
                }
            }
            if (str != null)
                vmixOn = true;
            return str;
        }

        #endregion

        #region Connection error handling


        object locker = new object();
        void HandleDisconnectArduino()
        {
            lock (locker)
            {
                if (connected)
                {
                    connected = false;
                    timer.Stop();
                    keepAlive.Stop();
                    p.DataReceived -= p_DataReceived;
                    p = null;
                    trayIcon.ShowBalloonTip(3000, "Kan niet verbinden!", "Er kan geen verbinding worden gemaakt met de mixer! Check de kabels en klik op 'rescan'. Als het probleem blijft bestaan: Stop het programma, maak de USB-kabel los en weer vast, start het programma opnieuw.", ToolTipIcon.Error);
                    trayIcon.Icon = new Icon("error.ico");
                }
            }
        }

        void HandleDisconnectVmix()
        {
            if (vmixOn)
            {
                vmixOn = false;
                trayIcon.ShowBalloonTip(2000, "vMix niet gevonden!", "Het programma kan vMix niet vinden! Controleer of vMix draait.", ToolTipIcon.Error);
            }
        }

        #endregion

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;

        public void AddTray()
        {
            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", Exit);
            trayMenu.MenuItems.Add("Rescan", Rescan);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "vMixer";
            trayIcon.Icon = new Icon("error.ico");

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
        }

        private void Exit(object sender, EventArgs e)
        {
            Exit();
        }
        private void Rescan(object sender, EventArgs e)
        {
            if (!connected)
                Scan();
        }
    }
}
