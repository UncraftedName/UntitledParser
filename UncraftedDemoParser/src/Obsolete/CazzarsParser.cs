//FROM: https://github.com/Traderain/Listdemo-/blob/master/Listdemo/Listdemo.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Text.Encoding;
using static System.Math;
using static System.BitConverter;
using static System.Globalization.CultureInfo;

namespace UncraftedDemoParser.Obsolete {
    public class Flag {
        public Flag(int t, float s, string type) //Flags like #SAVE# (in segmented).
        {
            Ticks = t;
            Time = s;
            Type = type;
        }

        public int Ticks {get;set;}
        public float Time {get;set;}
        public string Type {get;set;}
    }


    public class DemoParser {
        public enum DEMO_TYPE {
            Source,
            Goldsource,
            Unknown
        }

        public static string[] cheats = {
            "host_timescale", "god", "sv_cheats", "buddha", "host_framerate", "sv_accelerate",
            "sv_airaccelerate", "noclip", "ent_fire", "impulse", "ent_create", "sv_gravity", "upgrade_portalgun",
            "phys_timescale", "notarget", "give", "fire_energy_ball", "ent_create_portal_metal_sphere",
            "ent_create_portal_weight_box", "y_spt_autojump", "closecaption"
        };

        public static SourceDemoParseResult ParseDemo(Stream file, DEMO_TYPE DemoType) {
            #region Source Demo Parser

            var result = new SourceDemoParseResult();

            try {
                //using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(file)) {
                    #region Original HL2 Demo Parser

                    ASCII.GetString(br.ReadBytes(8)).TrimEnd('\0');
                    result.Protocol = (ToInt32(br.ReadBytes(4), 0)).ToString(InvariantCulture);
                    result.NProtocol = (ToInt32(br.ReadBytes(4), 0)).ToString(InvariantCulture);
                    result.ServerName = ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0');
                    result.PlayerName = ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0');
                    result.MapName = ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0');
                    result.GameName = ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0'); // gamedir=gamename

                    result.PTime = (Abs(ToInt32(br.ReadBytes(4), 0))).ToString(InvariantCulture);
                    result.Pticks = (Abs(ToInt32(br.ReadBytes(4), 0))).ToString(InvariantCulture);
                    result.Pframes = (Abs(ToInt32(br.ReadBytes(4), 0))).ToString(InvariantCulture);
                    var signOnLen = br.ReadInt32();
                    result.Flags = new List<Flag>();
                    result.Cheetz = new List<string>();
                    result.CoordsList = new List<Point3D>();

                    byte command;
                    do {
                        command = br.ReadByte();

                        if (command == 0x07) // dem_stop
                            break;

                        var tick = br.ReadInt32();
                        if (tick >= 0) {
                            result.TotalTicks = tick;
                        }


                        switch (command) {
                            case 0x01:
                                br.BaseStream.Seek(signOnLen, SeekOrigin.Current);
                                break;
                            case 0x02: {
                                br.BaseStream.Seek(4, SeekOrigin.Current); // skip flags

                                var x = br.ReadSingle();
                                var y = br.ReadSingle();
                                var z = br.ReadSingle();
                                result.CoordsList.Add(new Point3D((int) x, (int) y, (int) z));

                                br.BaseStream.Seek(0x44, SeekOrigin.Current);

                                var packetLen = br.ReadInt32();
                                br.BaseStream.Seek(packetLen, SeekOrigin.Current);
                            }
                                break;
                            case 0x04: {
                                var concmdLen = br.ReadInt32();
                                var concmd = ASCII.GetString(br.ReadBytes(concmdLen - 1));
                                result.CMDs.Add(concmd);
                                if (concmd.Contains("#SAVE#")) {
                                    if (tick >= 0) {
                                        result.Flags.Add(new Flag(tick, tick * 0.015f, "#SAVE#"));
                                    }
                                }

                                foreach (var s in cheats.Where(concmd.Contains)) {
                                    result.Cheated = true;
                                    result.Cheetz.Add(concmd);
                                }

                                if (concmd == "autosave") {
                                    //Autosave happened.
                                    if (tick >= 0) {
                                        result.Flags.Add(new Flag(tick, tick * 0.015f, "autosave"));
                                    }
                                }

                                if (concmd.StartsWith("+jump")) result.TotalJumps++;
                                br.BaseStream.Seek(1, SeekOrigin.Current); // skip null terminator
                            }
                                break;
                            case 0x05: {
                                br.BaseStream.Seek(4, SeekOrigin.Current); // skip sequence//int test = br.ReadInt32();
                                var userCmdLen = br.ReadInt32();
                                br.BaseStream.Seek(userCmdLen, SeekOrigin.Current);
                            }
                                break;
                            case 0x08: {
                                var stringTableLen = br.ReadInt32();
                                br.BaseStream.Seek(stringTableLen, SeekOrigin.Current);
                            }
                                break;
                        }
                    } while (command != 0x07); // dem_stop

                    result.Suceeded = true;

                    #endregion
                }
            }
            catch (Exception) {
                result.Suceeded = false;
            }

            result.TotalTime = result.TotalTicks * 0.015f; // 1 tick = 0.015s
            return result;

            #endregion
        }

        public static GoldSourceDemoParseResult ParseGoldSourceDemo(string file, DEMO_TYPE type) {
            #region Goldsource demo parser

            var result = new GoldSourceDemoParseResult();
            result.Flags = new List<Flag>();
            result.Cheetz = new List<string>();
            if (HLDEMO_IsValidDemo(file)) {
                if (HLDEMO_Open(file, 1) == IntPtr.Zero) {
                    if (HLSDEMO_IsValidDemo(file)) {
                        if (HLSDEMO_Open(file, 1) == IntPtr.Zero) {
                            result.Suceeded = true;
                            var demoFile = HLSDEMO_Open(file, 1);
                            var demoFileDemoHeader = HLSDEMO_DemoFileGetDemoHeader(demoFile);
                            result.MapName = Marshal.PtrToStringAnsi(demoFileDemoHeader.mapName);
                            result.Protocol = demoFileDemoHeader.demoProtocol.ToString();
                            result.MapCrc = demoFileDemoHeader.ToString();
                            result.DOffset = demoFileDemoHeader.directoryOffset.ToString();
                            result.NProtocol = demoFileDemoHeader.netProtocol.ToString();
                            result.Gamedir = Marshal.PtrToStringAnsi(demoFileDemoHeader.gameDir);
                            var size = HLDEMO_GetDirectoryEntryCount(demoFile);
                            for (var i = 0; i < size; i++) {
                                var currentDemoDirectoryEntry =
                                    HLDEMO_GetDirectoryEntry(demoFile, i);
                                var frameCount = HLDEMO_GetFrameCount(currentDemoDirectoryEntry.frame_data);
                                for (var j = 0; j < frameCount; j++) {
                                    var currFrame = HLDEMO_GetFrame(
                                        currentDemoDirectoryEntry.frame_data, j);
                                    if (currFrame.type == (int) demo_frame_type.CONSOLE_COMMAND) {
                                        var command =
                                            Marshal.PtrToStringAnsi(
                                                HLDEMO_TreatAsConsoleCommandFrame(currFrame.frame_pointer).command);
                                        result.TotalTime = currFrame.time;
                                        result.CMDs.Add(command);
                                        result.Pframes = currFrame.frame;
                                        if (command != null)
                                            foreach (var s in cheats.Where(command.Contains)) {
                                                result.Cheated = true;
                                                result.Cheetz.Add(command);
                                            }
                                    }
                                }
                            }
                        }
                        else {
                            result.Suceeded = false;
                        }
                    }
                    else {
                        result.Suceeded = false;
                    }
                }
                else {
                    var demoFile = HLDEMO_Open(file, 1);
                    var demoFileDemoHeader = HLDEMO_DemoFileGetDemoHeader(demoFile);
                    result.MapName = Marshal.PtrToStringAnsi(demoFileDemoHeader.mapName);
                    result.Protocol = demoFileDemoHeader.demoProtocol.ToString();
                    result.MapCrc = demoFileDemoHeader.mapCRC.ToString();
                    result.DOffset = demoFileDemoHeader.directoryOffset.ToString();
                    result.NProtocol = demoFileDemoHeader.netProtocol.ToString();
                    result.Gamedir = Marshal.PtrToStringAnsi(demoFileDemoHeader.gameDir);
                    var size = HLDEMO_GetDirectoryEntryCount(demoFile);
                    for (var i = 0; i < size; i++) {
                        var currentDemoDirectoryEntry =
                            HLDEMO_GetDirectoryEntry(demoFile, i);
                        var frameCount = HLDEMO_GetFrameCount(currentDemoDirectoryEntry.frame_data);
                        for (var j = 0; j < frameCount; j++) {
                            var currFrame = HLDEMO_GetFrame(
                                currentDemoDirectoryEntry.frame_data, j);
                            if (currFrame.type == (int) demo_frame_type.CONSOLE_COMMAND) {
                                var command =
                                    Marshal.PtrToStringAnsi(
                                        HLDEMO_TreatAsConsoleCommandFrame(currFrame.frame_pointer).command);
                                result.TotalTime = currFrame.time;
                                result.CMDs.Add(command);
                                result.Pframes = currFrame.frame;
                                if (command != null)
                                    foreach (var s in cheats.Where(command.Contains)) {
                                        result.Cheated = true;
                                        result.Cheetz.Add(command);
                                    }
                            }
                        }
                    }

                    result.Suceeded = true;
                }
            }
            else {
                result.Suceeded = false;
            }

            return result;

            #endregion
        }

        public static DEMO_TYPE DetermineDemoType(string file) {
            DEMO_TYPE dt;
            string mw;
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs)) {
                mw = ASCII.GetString(br.ReadBytes(8)).TrimEnd('\0');
            }

            switch (mw) {
                case "HLDEMO":
                    dt = DEMO_TYPE.Goldsource;
                    break;
                case "HL2DEMO":
                    dt = DEMO_TYPE.Source;
                    break;
                default:
                    dt = DEMO_TYPE.Unknown;
                    break;
            }

            return dt;
        }

        public class Point3D {
            public int X;
            public int Y;
            public int Z;

            public Point3D(int x, int y, int z) {
                X = x;
                Y = y;
                Z = z;
            }
        }

        #region Pinvoke <3 YaLTeR

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern demo_header HLDEMO_DemoFileGetDemoHeader(IntPtr demofile);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr HLDEMO_Open([MarshalAs(UnmanagedType.LPStr)] string lpString, int readFrame);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void HLDEMO_Close(IntPtr demofile);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HLDEMO_IsValidDemo([MarshalAs(UnmanagedType.LPStr)] string lpString);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HLDEMO_DemoFileDidReadFrames([MarshalAs(UnmanagedType.LPStr)] string lpString);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void HLDEMO_DemoFileReadFrames(IntPtr demofile);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int HLDEMO_GetDirectoryEntryCount(IntPtr demofile);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern demo_directory_entry HLDEMO_GetDirectoryEntry(IntPtr demofile, int index);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int HLDEMO_GetFrameCount(IntPtr frame_data);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern demo_frame HLDEMO_GetFrame(IntPtr frame_data, int index);

        [DllImport("HLDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern console_command_frame HLDEMO_TreatAsConsoleCommandFrame(IntPtr frame_pointer);

        [StructLayout(LayoutKind.Sequential)]
        public struct demo_header {
            public int netProtocol;
            public int demoProtocol;
            public IntPtr mapName;
            public IntPtr gameDir;
            public int mapCRC;
            public int directoryOffset;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct demo_directory_entry {
            public int type;
            public IntPtr description;
            public int flags;
            public int CDTrack;
            public float trackTime;
            public int frameCount;
            public int offset;

            public int fileLength;

            // Pass this to relevant functions.
            public IntPtr frame_data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct demo_frame {
            public int type;
            public float time;

            public int frame;

            // Pass this to HLDEMO_TreatAs<x>Frame().
            public IntPtr frame_pointer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct console_command_frame {
            public IntPtr command;
        }

        public enum demo_frame_type {
            DEMO_START = 2,
            CONSOLE_COMMAND = 3,
            CLIENT_DATA = 4,
            NEXT_SECTION = 5,
            EVENT = 6,
            WEAPON_ANIM = 7,
            SOUND = 8,
            DEMO_BUFFER = 9
        }

        #endregion

        #region <3

        [StructLayout(LayoutKind.Sequential)]
        public struct hls_demo_header {
            public int netProtocol;
            public int demoProtocol;
            public IntPtr mapName;
            public IntPtr gameDir;
            public int directoryOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct hls_demo_directory_entry {
            public int tpye;
            public int playbackTime;
            public int frameCount;
            public int offset;
            public int fileLength;
        }

        public enum hls_demo_frame_type {
            NETWORK_PACKET = 2,
            JUMPTIME = 3,
            CONSOLE_COMMAND = 4,
            USERCMD = 5,
            STRINGTABLES = 6,
            NEXT_SECTION = 7
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct hls_demo_frame {
            public int type;
            public float time;
            public int frame;

            public IntPtr frame_pointer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct hls_console_command_frame {
            public IntPtr command;
        }

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HLSDEMO_IsValidDemo([MarshalAs(UnmanagedType.LPStr)] string lpString);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr HLSDEMO_Open([MarshalAs(UnmanagedType.LPStr)] string lpString, int read_frames);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void HLSDEMO_Close(IntPtr demo_file);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern hls_demo_header HLSDEMO_DemoFileGetDemoHeader(IntPtr demo_file);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void HLSDEMO_DemoFileReadFrames(IntPtr demo_file);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HLSDEMO_DemoFileDidReadFrames(IntPtr demo_file);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int HLSDEMO_GetDirectoryEntryCount(IntPtr demo_file);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern hls_demo_directory_entry HLSDEMO_GetDirectoryEntry(IntPtr demo_file, int index);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HLSDEMO_GetFrameCount(IntPtr frame_data);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern hls_demo_frame HLSDEMO_GetFrame(IntPtr frame_data, int index);

        [DllImport("HLSDemo.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern hls_console_command_frame HLSDEMO_TreatAsConsoleCommandFrame(IntPtr frame_pointer);

        #endregion
    }

    public class SourceDemoParseResult {
        public SourceDemoParseResult() {
            CrosshairAppearTick = -1;
            CrosshairDisappearTick = -1;
        }

        public string demo_header {get;set;}
        public bool Cheated {get;set;}
        public List<string> Cheetz {get;set;}
        public List<Flag> Flags {get;set;}
        public List<string> CMDs {get;set;} = new List<string>();
        public int CrosshairAppearTick {get;set;}
        public int CrosshairDisappearTick {get;set;}
        public int TotalTicks {get;set;}
        public float TotalTime {get;set;}
        public string MapName {get;set;} = "-";
        public string PlayerName {get;set;} = "-";
        public string GameName {get;set;} = "-";
        public int TotalJumps {get;set;}
        public string PTime {get;set;} = "-";
        public string Pframes {get;set;} = "-";
        public string Pticks {get;set;} = "-";
        public string Protocol {get;set;} = "-";
        public string NProtocol {get;set;} = "-";
        public string ServerName {get;set;} = "-";
        public string MapCrc {get;set;} = "-";
        public string Gamedir {get;set;} = "-";
        public string DOffset {get;set;} = "-";
        public bool Suceeded {get;set;}
        public List<DemoParser.Point3D> CoordsList {get;set;}
    }

    public class GoldSourceDemoParseResult {
        public List<Flag> Flags {get;set;}
        public List<string> Cheetz {get;set;}
        public List<string> CMDs {get;set;}
        public bool Suceeded {get;set;}
        public bool Cheated {get;set;}
        public string MapName {get;set;}
        public string Protocol {get;set;}
        public string MapCrc {get;set;}
        public string DOffset {get;set;}
        public string NProtocol {get;set;}
        public string Gamedir {get;set;}
        public float TotalTime {get;set;}
        public int Pframes {get;set;}
    }
}