﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace YJ_AutoUnClamp.Models
{
    public class NFC_HID
    {
        // WinAPI 선언
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);


        private static IntPtr _hookID = IntPtr.Zero;
        private static string tagBuffer = "";
        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x0100;

        private LowLevelKeyboardProc _proc = HookCallback;

        public NFC_HID()
        {
            _hookID = SetHook(_proc);
        }
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        public void HookEnd()
        {
            UnhookWindowsHookEx(_hookID);
        }
        

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                char ch = Convert.ToChar(vkCode);
                tagBuffer += ch;

                if (vkCode == (int)Keys.Enter)
                {
                    Console.WriteLine("NFC Tag Read: " + tagBuffer.Trim());
                    tagBuffer = tagBuffer.TrimEnd('\r');

                    SingletonManager.instance.Nfc_Data = tagBuffer;
                    tagBuffer = "";
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}
