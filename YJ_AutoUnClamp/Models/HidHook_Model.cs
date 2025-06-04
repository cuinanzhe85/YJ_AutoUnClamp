using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace YJ_AutoUnClamp.Models
{
    public class HidHook_Model
    {
        private readonly IntPtr _hwnd;

        public HidHook_Model(IntPtr hwnd)
        {
            _hwnd = hwnd;
            RegisterHIDRawInput();
        }

        public void ProcessRawInputMessage(IntPtr hRawInput)
        {
            uint dwSize = 0;

            GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);

            try
            {
                if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));
                    if (raw.header.dwType == RIM_TYPEHID)
                    {
                        Console.WriteLine("HID 입력 감지됨");
                        Console.WriteLine("크기: " + raw.hid.dwSizeHid + ", 개수: " + raw.hid.dwCount);

                        IntPtr dataPtr = IntPtr.Add(buffer, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                        byte[] rawData = new byte[raw.hid.dwSizeHid];
                        //Marshal.Copy(dataPtr, rawData, 0, raw.hid.dwSizeHid);

                        string hex = BitConverter.ToString(rawData);
                        Console.WriteLine("데이터: " + hex);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void RegisterHIDRawInput()
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].UsagePage = 0x01;
            rid[0].Usage = 0x08;
            rid[0].Flags = 0;
            rid[0].Target = _hwnd;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                MessageBox.Show("Raw input 등록 실패!");
            }
        }

        // ---------------- WIN32 구조체 및 상수 ----------------

        private const int RID_INPUT = 0x10000003;
        private const int RIM_TYPEHID = 2;

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct RAWINPUT
        {
            [FieldOffset(0)]
            public RAWINPUTHEADER header;
            [FieldOffset(16)]
            public RAWHID hid;
        }

        [DllImport("User32.dll")]
        static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    }
}
