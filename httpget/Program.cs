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
        SerialPort p;
        WebClient c = new WebClient();
        private System.Timers.Timer timer, keepAlive;
        bool connected = false;
        
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
            for (int i = 1; i < 256; i++)
            {
                try
                {
                    p = new SerialPort("COM" + i, 9600);
                    p.ReadTimeout = 1000;
                    byte t = (byte)r.Next(255);
                    p.Open();
                    p.Write(new byte[2] { 255, t }, 0, 2);
                    int temp = p.ReadByte();
                    if (temp == t)
                    {
                        p.Write(new byte[1] { 1 }, 0, 1);
                        Start();
                        return;
                    }
                }
                catch { p = null; }
            }
            trayIcon.ShowBalloonTip(2000, "Kan niet verbinden!", "Er kan geen verbinding worden gemaakt met de mixer! Check de kabels en klik op rescan.", ToolTipIcon.Error);
            trayIcon.Icon = new Icon("error.ico");
            connected = false;
        }

        void Exit()
        {
            Environment.Exit(0);
        }

        bool flip = false;
        void p_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int type;
            lock (p)
            {
                type = p.ReadByte();
            }
            //Console.WriteLine(i);
            if (type == 0)
            {
                int fadeLevel;
                lock (p)
                {
                    fadeLevel = p.ReadByte();
                }
                Console.WriteLine(fadeLevel);
                Stream st = Query("?Function=SetFader&Value=" + (flip ? 255 - fadeLevel : fadeLevel));
                st.Close();
                if (fadeLevel == 255)
                    flip = true;
                else if (fadeLevel == 0)
                    flip = false;

            }
            else if (type == 1)
            {
                int buttonNr;
                lock (p)
                {
                    buttonNr = p.ReadByte();
                }
                if (buttonNr < 5)
                {
                    Stream st = Query("?Function=PreviewInput&Input=" + buttonNr);
                    st.Close();
                }
                else
                {
                    Stream st = Query("?Function=FadeToBlack");
                    st.Close();
                }
            }
        }


        byte preview = 0, oldPreview = 0, active = 0, oldActive = 0;
        bool ftb = false, ftbOld = false;
        byte[] previewLeds = new byte[4] { 2, 3, 4, 5 };
        byte[] activeLeds = new byte[4] { 6, 7, 8, 9 };
        const byte ftbLed = 10;
        void getInfo(object sender, EventArgs e)
        {
            //Console.WriteLine(isAlive.Enabled);
            Stream response = Query();
            XmlReader r = XmlReader.Create(response);
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Element)
                {
                    if (r.Name == "preview")
                    {
                        r.Read();
                        oldPreview = preview;
                        preview = byte.Parse(r.Value);

                        if (oldPreview != preview)
                        {
                            lock (p)
                            {
                                WriteLed(previewLeds[preview-1], 1);
                                if (oldPreview != 0)
                                    WriteLed(previewLeds[oldPreview-1], 0);
                            }
                        }
                    }
                    else if (r.Name == "active")
                    {
                        r.Read();
                        oldActive = active;
                        active = byte.Parse(r.Value);

                        if (oldActive != active)
                        {
                            lock (p)
                            {
                                WriteLed(activeLeds[active-1], 1);
                                if (oldActive != 0)
                                    WriteLed(activeLeds[oldActive - 1], 0);
                            }
                        }
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
                                WriteLed(ftbLed, Convert.ToByte(ftb));
                            }
                        }
                    }
                }
            }
            r.Close();
            response.Close();
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
                    Exit();
                }
            }
        }


        void WriteLed(byte led, byte val)
        {
            lock (p)
            {
                try
                {
                    p.Write(new byte[3] { 0, led, val }, 0, 3);
                }
                catch
                {
                    Exit();
                }
            }
        }

        public Stream Query(string urlEnd = "")
        {
            string s = "http://127.0.0.1:8088/api/"+urlEnd;
            //Console.WriteLine(s);
            Stream str = null;
            lock (c)
            {
                try
                {
                    str = c.OpenRead(s);
                }
                catch
                {
                    Exit();
                }
            }
            return str;
        }

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
