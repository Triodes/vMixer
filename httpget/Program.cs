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
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;

namespace httpget
{
    class Program
    {
        #region Scanning startup and loop

        const int nButtons = 5;
        SerialPort p;
        MyWebClient c = new MyWebClient();
        Stopwatch stopwatch = new Stopwatch();
        Trigger getvMixInfo;
        bool vmixOn = true;
        List<Trigger> triggers = new List<Trigger>();
        List<ButtonTrigger> buttonTriggers = new List<ButtonTrigger>();
        Dictionary<string, string> bindings = new Dictionary<string, string>();
        
        
        static void Main(string[] args)
        {
            Program prog = new Program();
        }

        public Program()
        {
            stopwatch.Start();
            AddTray();
            Scan();
        }

        void Scan()
        {
            Random r = new Random();
            string[] ports = SerialPort.GetPortNames();
            bool succes;
            for (int i = 0; i < ports.Length; i++)
            {
                succes = false;
                try
                {
                    p = new SerialPort(ports[i], 9600);
                    p.ReadTimeout = 1000;
                    byte t = (byte)r.Next(255);
                    p.Open();
                    p.Write(new byte[2] { 255, t }, 0, 2);
                    int temp = p.ReadByte();
                    if (temp == t)
                    {
                        succes = true;
                    }
                }
                catch { p = null; }

                if (succes)
                {
                    p.Write(new byte[1] { 1 }, 0, 1);
                    Start();
                    return;
                }
            }
            HandleDisconnectArduino(false);
        }

        void Start()
        {
            trayIcon.Icon = new Icon("succes.ico");
            getvMixInfo = new Trigger(500, GetInfo);
            triggers.Add(getvMixInfo);
            triggers.Add(new Trigger(250, KeepAlive));

            foreach (Trigger e in triggers)
            {
                e.Start();
            }

            buttonTriggers.Add(null);
            for (int i = 1; i <= nButtons; i++)
            {
                ButtonTrigger t = new ButtonTrigger(500, HandleButtonPress, i);
                triggers.Insert(i,t);
                buttonTriggers.Add(t);
            }

            while (true)
            {
                Loop();
                Application.DoEvents();
            }
        }

        void Exit()
        {
            Environment.Exit(0);
        }

        long previous, current;
        int elapsed;
        void Loop()
        {
            previous = current;
            current = stopwatch.ElapsedMilliseconds;
            elapsed = (int)(current - previous);
            Tick(elapsed);

            while (p.BytesToRead > 1)
                DataReceived();
        }

        void Tick(int elapsed)
        {
            foreach (Trigger e in triggers)
                e.tick(elapsed);
        }

        #endregion

        #region data handling

        bool flip = false;
        void DataReceived()
        {
            int type = ReadByte();
            if (type == 0) //fader value changed
            {
                int fadeLevel = ReadByte();                
                if (fadeLevel != -1)
                {
                    Stream st = Query("?Function=SetFader&Value=" + (flip ? 255 - fadeLevel : fadeLevel));
                    if (st != null) st.Close();
                    if (fadeLevel == 255)
                        flip = true;
                    else if (fadeLevel == 0)
                        flip = false;
                }
            }
            else if (type == 1) //button was pressed
            {
                int buttonNr = ReadByte();
                buttonTriggers[buttonNr].Start();
            }
            else if (type == 2)//button was released
            {
                int buttonNr = ReadByte();
                buttonTriggers[buttonNr].Reset();
            }
            else if (type == 3)
            {
                Exit();
            }
        }

        void HandleButtonPress(int button, bool LongPress)
        {
            Stream st;
            if (LongPress)
                st = Query(Settings.Default["Long" + button].ToString());
            else
                st = Query(Settings.Default["Button" + button].ToString());
            if (st != null) st.Close();
        }

        int ReadByte()
        {
            try
            {
                return p.ReadByte();
            }
            catch
            {
                HandleDisconnectArduino();
            }
            return -1;
        }

        void WriteBytes(byte[] toSend)
        {
            try
            {
                p.Write(toSend, 0, toSend.Length);
            }
            catch
            {
                HandleDisconnectArduino(true);
            }
                
        }

        void WriteLedState()
        {
            WriteBytes(new byte[2] { 0, (byte)(preview | active) });
        }

        void KeepAlive()
        {
            WriteBytes(new byte[1] { 125 });
        }


        int preview = 0, oldPreview = 0, active = 0, oldActive = 0;
        bool ftb = false, ftbOld = false;
        void GetInfo()
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
                                WriteBytes(new byte[2] { 1, Convert.ToByte(ftb) });
                            }
                        }
                    }
                }
                r.Close();
                response.Close();
            }
        }

        public Stream Query(string urlEnd = "")
        {
            string s = "http://127.0.0.1:8088/api/" + urlEnd;
            Stream str = null;
            try
            {
                str = c.OpenRead(s);
            }
            catch
            {
                HandleDisconnectVmix();
            }
            if (str != null)
            {
                vmixOn = true;
                getvMixInfo.SetInterval(20);
            }
            return str;
        }

        #endregion

        #region Connection error handling

        void HandleDisconnectArduino(bool running = true)
        {
            p = null;
            trayIcon.ShowBalloonTip(3000, (running ? "Verbinding verbroken!" : "Kan niet verbinden!"), (running ? "De verbinding met de mixer is verbroken. " : "Kan geen verbinding maken met de mixer. ") + "Het programma sluit nu af. Controleer de kabels en herstart het programma.", ToolTipIcon.Error);
            trayIcon.Icon = new Icon("error.ico");
            Exit();
        }

        void HandleDisconnectVmix()
        {
            if (vmixOn)
            {
                getvMixInfo.SetInterval(500);
                vmixOn = false;
                preview = active = 0;
                WriteLedState();
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
            trayMenu.MenuItems.Add("Configure", OpenConfig);

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

        private void OpenConfig(object sender, EventArgs e)
        {
            Configuration confWin = new Configuration();
            confWin.Show();
        }

        private void Exit(object sender, EventArgs e)
        {
            Exit();
        }
    }
}
