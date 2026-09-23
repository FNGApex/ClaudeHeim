// Compiled by Run-ClaudeHeim.ps1 (Add-Type), not part of the plugin build.
// Reads the Windows mixer's peak meter for one process's audio sessions on the default output device, so a run can
// report whether the game ever made a sound. Read-only: it never changes volumes or mute state.
using System;
using System.Runtime.InteropServices;

namespace ClaudeHeimRun
{
    public static class AudioPeak
    {
        /// <summary>Highest current peak (0..1) over all audio sessions of process <paramref name="pid"/>; 0 when it has none.</summary>
        public static float Peak(int pid)
        {
            int sessionCount;
            return Peak(pid, out sessionCount);
        }

        /// <summary>As <see cref="Peak(int)"/>, and how many audio sessions the process has (0 = the meter cannot see it).</summary>
        public static float Peak(int pid, out int sessionCount)
        {
            sessionCount = 0;
            float peak = 0f;
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                IMMDevice device;
                if (enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out device) != 0) return 0f;
                var iid = typeof(IAudioSessionManager2).GUID;
                object o;
                if (device.Activate(ref iid, 23 /* CLSCTX_ALL */, IntPtr.Zero, out o) != 0) return 0f;
                var manager = (IAudioSessionManager2)o;
                IAudioSessionEnumerator sessions;
                if (manager.GetSessionEnumerator(out sessions) != 0) return 0f;
                int count;
                sessions.GetCount(out count);
                for (int i = 0; i < count; i++)
                {
                    IAudioSessionControl2 control;
                    if (sessions.GetSession(i, out control) != 0) continue;
                    uint owner;
                    control.GetProcessId(out owner);
                    if (owner != (uint)pid) continue;
                    sessionCount++;
                    var meter = control as IAudioMeterInformation;
                    float value;
                    if (meter != null && meter.GetPeakValue(out value) == 0 && value > peak) peak = value;
                }
            }
            catch (Exception)
            {
                // Audio device switched or not available: report silence rather than failing the run.
            }

            return peak;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        }

        [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionManager2
        {
            int GetAudioSessionControl(IntPtr sessionGuid, int flags, out IntPtr control);
            int GetSimpleAudioVolume(IntPtr sessionGuid, int flags, out IntPtr volume);
            int GetSessionEnumerator(out IAudioSessionEnumerator sessions);
        }

        [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionEnumerator
        {
            int GetCount(out int count);
            int GetSession(int index, out IAudioSessionControl2 session);
        }

        [ComImport, Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl2
        {
            // IAudioSessionControl
            int GetState(out int state);
            int GetDisplayName(out IntPtr name);
            int SetDisplayName(IntPtr name, IntPtr context);
            int GetIconPath(out IntPtr path);
            int SetIconPath(IntPtr path, IntPtr context);
            int GetGroupingParam(out Guid param);
            int SetGroupingParam(ref Guid param, IntPtr context);
            int RegisterAudioSessionNotification(IntPtr client);
            int UnregisterAudioSessionNotification(IntPtr client);
            // IAudioSessionControl2
            int GetSessionIdentifier(out IntPtr id);
            int GetSessionInstanceIdentifier(out IntPtr id);
            int GetProcessId(out uint pid);
        }

        [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioMeterInformation
        {
            int GetPeakValue(out float peak);
        }
    }
}
