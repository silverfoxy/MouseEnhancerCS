using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MouseEnhancerCS
{
    class Win32
    {
        public const int
        WM_DEVICECHANGE = 0x0219;
        public const int
        DBT_DEVICEARRIVAL = 0x8000,
        DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int
        DEVICE_NOTIFY_WINDOW_HANDLE = 0,
        DEVICE_NOTIFY_SERVICE_HANDLE = 1;
        public const int
        DBT_DEVTYP_DEVICEINTERFACE = 5;
        public static Guid
        GUID_DEVINTERFACE_HID = new
        Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public short dbcc_name;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(
        IntPtr hRecipient,
        IntPtr NotificationFilter,
        Int32 Flags);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();
    }
}
