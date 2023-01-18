using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace WOLtv
{
    public partial class MainWindow : Window
    {

        // Set the IP and MAC address of your TV
        public String TvIpAddress = "192.168.0.28";
        public String TvMacAddress = "64:CB:E9:8D:E8:7E";

        private HwndSource _HwndSource;
        private readonly IntPtr _ScreenStateNotify;
        public byte[] payload = new byte[1024];

        public MainWindow()
        {
            InitializeComponent();
            //PrepareWOLPayload();
            if (!EventLog.SourceExists("Monitor"))
            {
                EventLog.CreateEventSource("Monitor", "Application");
            }
            // register for console display state system event 
            var wih = new WindowInteropHelper(this);
            var hwnd = wih.EnsureHandle();
            _ScreenStateNotify = NativeMethods.RegisterPowerSettingNotification(hwnd, ref NativeMethods.GUID_CONSOLE_DISPLAY_STATE, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            _HwndSource = HwndSource.FromHwnd(hwnd);
            _HwndSource.AddHook(HwndHook);
            SendWOL();
        }

        public void PrepareWOLPayload()
        {
            var macAddress = Regex.Replace(TvMacAddress, "[-|:]", "");       // Remove any semicolons or minus haracters present in our MAC address
            int payloadIndex = 0;
            /// The magic packet is a broadcast frame containing anywhere within its payload 6 bytes of all 255 (FF FF FF FF FF FF in hexadecimal), followed
            /// by sixteen repetitions of the target computer's 48-bit MAC address, for a total of 102 bytes. */
            /// byte[] payload = new byte[1024];    // Our packet that we will be broadcasting
            /// Add 6 bytes with value 255 (FF) in our payload
            for (int i = 0; i < 6; i++)
            {
                payload[payloadIndex] = 255;
                payloadIndex++;
            }
            // Repeat the device MAC address sixteen times
            for (int j = 0; j < 16; j++)
            {
                for (int k = 0; k < macAddress.Length; k += 2)
                {
                    var v = macAddress.Substring(k, 2);
                    payload[payloadIndex] = byte.Parse(v, NumberStyles.HexNumber);
                    payloadIndex++;
                }
            }
        }

        public void SendWOL()
        {
            PrepareWOLPayload();
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                EnableBroadcast = true
            };

            for (int i = 0; i < 2; i++)
            {
                IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 0);
                sock.SendTo(payload, RemoteEndPoint);  // Broadcast our packet
                IPEndPoint RemoteEndPoint1 = new IPEndPoint(IPAddress.Parse("192.168.0.255"), 9);
                sock.SendTo(payload, RemoteEndPoint1);  // Broadcast our packet
                IPEndPoint RemoteEndPoint2 = new IPEndPoint(IPAddress.Parse(TvIpAddress), 9);
                sock.SendTo(payload, RemoteEndPoint2);  // Broadcast our packet
                Thread.Sleep(200);
            }
            sock.Close(2000);
        }

        public void Elog(byte b)
        {
            var temp = b.ToString();
            if (temp.Length < 1) { temp = "pusto"; }

            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Boniek";
                eventLog.WriteEntry("Boniek monitor power event - " + temp, EventLogEntryType.Information, b, b);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // handler of console display state system event 
            if (msg == NativeMethods.WM_POWERBROADCAST)
            {
                if (wParam.ToInt32() == NativeMethods.PBT_POWERSETTINGCHANGE)
                {
                    var s = (NativeMethods.POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(NativeMethods.POWERBROADCAST_SETTING));
                    if (s.PowerSetting == NativeMethods.GUID_CONSOLE_DISPLAY_STATE)
                    {
                        Task.Run(() => Elog(s.Data));
                        //if (s.Data == 1)                     //// not tested
                        //{
                        Task.Run(() => SendWOL());
                        //}

                    }
                }
            }

            return IntPtr.Zero;
        }

        ~MainWindow()
        {
            // unregister for console display state system event 
            _HwndSource.RemoveHook(HwndHook);
            NativeMethods.UnregisterPowerSettingNotification(_ScreenStateNotify);
        }

    }

    internal static class NativeMethods
    {
        public static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid(0x6fe69556, 0x704a, 0x47a0, 0x8f, 0x24, 0xc2, 0x8d, 0x93, 0x6f, 0xda, 0x47);
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        public const int WM_POWERBROADCAST = 0x0218;
        public const int PBT_POWERSETTINGCHANGE = 0x8013;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, Int32 Flags);

        [DllImport(@"User32", SetLastError = true, EntryPoint = "UnregisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnregisterPowerSettingNotification(IntPtr handle);
    }


}
