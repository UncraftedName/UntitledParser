using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using static System.Text.Encoding;
using static System.Math;
using static System.BitConverter;
using static System.Globalization.CultureInfo;
using static UncraftedDemoParser.obsolete.SpecialTickTracker;

namespace UncraftedDemoParser.obsolete
{

    public static class OriginalParser
    {
        public enum PacketType
        {
            SIGNON = 1,
            PACKET,
            SYNCTICK,
            CONSOLECMD,
            USERCMD,
            DATATABLES,
            STOP,
            CUSTOMDATA,
            STRINGTABLES
        }

        public static String GetData(BinaryReader br)
        {
            StringBuilder output = new StringBuilder();
            // header stuff
            output.AppendLine($"Header: {ASCII.GetString(br.ReadBytes(8)).TrimEnd('\0')}");
            int demoProtocol = ToInt32(br.ReadBytes(4), 0);
            output.AppendLine($"Demo protocol: {demoProtocol.ToString(InvariantCulture)}");
            int networkProtocol = ToInt32(br.ReadBytes(4), 0);
            output.AppendLine($"Network protocol: {networkProtocol.ToString(InvariantCulture)}");
            output.AppendLine($"Server name: {ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0')}");
            output.AppendLine($"Client name: {ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0')}");
            output.AppendLine($"Map name: {ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0')}");
            output.AppendLine($"Game directory: {ASCII.GetString(br.ReadBytes(260)).TrimEnd('\0')}");
            output.AppendLine($"Playback time: {br.ReadSingle().ToString(InvariantCulture)}");
            output.AppendLine($"Tick count: {ToInt32(br.ReadBytes(4), 0).ToString(InvariantCulture)}");
            output.AppendLine($"Frame count: {ToInt32(br.ReadBytes(4), 0).ToString(InvariantCulture)}");
            int signOnLen = Abs(ToInt32(br.ReadBytes(4), 0));
            output.AppendLine($"Sign on length: {signOnLen.ToString()}\n");

            int maxSplitscreenPlayers;
            bool hasAlignmentByte;
            switch (networkProtocol)
            {
                case 15: // portal 1
                    maxSplitscreenPlayers = 1;
                    hasAlignmentByte = false;
                    break;
                case 2001: // portal 2
                    maxSplitscreenPlayers = 2;
                    hasAlignmentByte = true;
                    break;
                default:
                    maxSplitscreenPlayers = 4;
                    hasAlignmentByte = true;
                    break;
            }

            //SpecialTickTracker tracker = new SpecialTickTracker();
            
            // read demo until end is reached
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                PacketType type = (PacketType)br.ReadByte();
                if (type == PacketType.STOP)
                {
                    // the last byte is cut off, so the tick cannot be determined with 100% accuracy
                    output.Append(PacketType.STOP);
                    break;
                }
                int currentTick = br.ReadInt32();
                SimplePacket tmpPacket = new SimplePacket(type, currentTick);
                /*if (tracker.PacketCounter.ContainsKey(tmpPacket)) {
                    tracker.PacketCounter[tmpPacket]++;
                } else {
                    tracker.PacketCounter[tmpPacket] = 1;
                }*/
                output.AppendLine($"\n[{currentTick.ToString()}] {type}");
                if (hasAlignmentByte) {
                    br.ReadByte();
                    //br.BaseStream.Seek(1, SeekOrigin.Current);
                }
                
                switch (type)
                {
                    case PacketType.SIGNON:
                        br.BaseStream.Seek(signOnLen, SeekOrigin.Current);
                        break;
                    // position is not guaranteed to be updated every frame.
                    // when it is, it copies the position of the previous UserCMD packet (not yet thoroughly tested)
                    case PacketType.PACKET: // position is not guaranteed to be updated every frame
                        //specialTicks.Add(currentTick);
                        for (int i = 0; i < maxSplitscreenPlayers; i++) {
                            
                            string flagsAsString = System.Convert.ToString(br.ReadInt32(), 2).PadLeft(32, '0');
                            output.AppendLine("\tflags: " + $"{String.Join(" ", StringToChunks(flagsAsString, 8))}");
                            
                            // there are 6 * 3 floats for every player in this packet
                            // view origin, view angles, local view angles, view origin 2, view angles 2, local view angles 2
                            // z is off from showpos by -64
                            float[] floats = new float[18];
                            string[] asStrings = new string[18];
                            List<Vector3> asVectors = new List<Vector3>();
                            
                            for (int j = 0; j < 18; j++) {
                                floats[j] = br.ReadSingle();
                                asStrings[j] = floats[j].ToString("F2");
                                if (j % 3 == 2)
                                    asVectors.Add(new Vector3(floats[j - 2], floats[j - 1], floats[j]));
                            }

                            output.AppendFormat("\t{3,-20} x: {0,-13} y: {1,-11} z: {2,-11}\n", $"{asStrings[0]},", $"{asStrings[1]},", $"{asStrings[2]}", "view origin");
                            output.AppendFormat("\t{3,-20} x: {0,-13} y: {1,-11} z: {2,-11}\n", $"{asStrings[9]},", $"{asStrings[10]},", $"{asStrings[11]}", "view origin 2");
                            output.AppendFormat("\t{3,-20} pitch: {0,-9} yaw: {1,-9} roll: {2,-9}\n", $"{asStrings[3]}°,", $"{asStrings[4]}°,", $"{asStrings[5]}°", "view angles");
                            output.AppendFormat("\t{3,-20} pitch: {0,-9} yaw: {1,-9} roll: {2,-9}\n", $"{asStrings[12]}°,", $"{asStrings[13]}°,", $"{asStrings[14]}°", "view angles 2");
                            output.AppendFormat("\t{3,-20} pitch: {0,-9} yaw: {1,-9} roll: {2,-9}\n", $"{asStrings[6]}°,", $"{asStrings[7]}°,", $"{asStrings[8]}°", "local view angles");
                            output.AppendFormat("\t{3,-20} pitch: {0,-9} yaw: {1,-9} roll: {2,-9}\n", $"{asStrings[15]}°,", $"{asStrings[16]}°,", $"{asStrings[17]}°", "local view angles 2");

                            // if view angles != local view angles then :thinking:
                            // this might be a sign that the player is going through a portal
                            /*if (!asVectors[1].Equals(asVectors[2])) {
                                tracker.Add(TickType.ViewMismatchWithLocalView, currentTick);
                            }
                            if (!Array.TrueForAll<Vector3>(asVectors.GetRange(3, 3).ToArray(),
                                    vec3 => vec3.Equals(Vector3.Zero))) 
                            {
                                tracker.Add(TickType.View2NonZero, currentTick);
                            }*/
                        }

                        int inSequence = br.ReadInt32();
                        int outSequence = br.ReadInt32();
                        int packetLen = br.ReadInt32();
                        byte messageType = (byte)(br.ReadByte() & 0b00111111);
                        output.AppendLine($"\tin sequence: {inSequence}");
                        output.AppendLine($"\tout sequence: {outSequence}");
                        output.AppendLine($"\tpacket length: {packetLen}");
                        output.AppendLine($"\tmessage type: {messageType}");
                        /*if (messageType > 32)
                            tracker.Add(TickType.UnknownSvcMessageType, currentTick);*/
                        br.BaseStream.Seek(packetLen - 1, SeekOrigin.Current);
                        break;
                    case PacketType.CONSOLECMD:
                        int concmdLen = br.ReadInt32() - 1;
                        //output.AppendLine($"\tconsole cmd length: {concmdLen.ToString()}");
                        output.AppendLine($"\t{ASCII.GetString(br.ReadBytes(concmdLen))}");
                        br.BaseStream.Seek(1, SeekOrigin.Current); // skip null terminator
                        break;
                    case PacketType.USERCMD:
                        int sequence = br.ReadInt32();
                        output.AppendLine($"\tsequence: {sequence}");
                        int usercmdLen = br.ReadInt32();
                        output.AppendLine($"\tuser cmd length: {usercmdLen}");
                        UserCmdOld userCmd = new UserCmdOld(br.ReadBytes(usercmdLen), currentTick);
                        output.Append(userCmd.ToString());
                        /*if (userCmd._commandNumber != sequence) {
                            tracker.Add(TickType.CommandMismatchWithSequence, currentTick);
                        }
                        if (currentTick == 0) { // saves the first difference and compares the rest to it
                            tracker.CmdTickDifference = (int)(userCmd._tickCount - currentTick);
                        }
                        if (userCmd._tickCount - currentTick != tracker.CmdTickDifference) {
                            tracker.Add(TickType.BadCmdTickCount, currentTick);
                        }*/

                        float?[] movements = {userCmd._forwardMove, userCmd._sideMove, userCmd._upMove};
                        // i've observed that demos for a specific game (almost) always take on only some movement quantities in the UserCMD packet
                        /*if (networkProtocol == 2001 &&
                            !Array.TrueForAll(movements,
                                val => !val.HasValue ||
                                       !Array.TrueForAll(new[] {175, 87.5}, m => m != Abs(val.Value))) ||
                            networkProtocol == 15   &&
                            !Array.TrueForAll(movements,
                                val => !val.HasValue ||
                                       !Array.TrueForAll(new[] {112.5, 225, 400, 450}, m => m != Abs(val.Value)))) 
                        {
                            tracker.Add(TickType.BadMovementInUserCmd, currentTick);
                        }*/
                        break;
                    case PacketType.CUSTOMDATA:
                        // This is actually StringTables in protocol 3
                        if (demoProtocol == 3)
                        {
                            int stringTableLen = br.ReadInt32();
                            output.AppendLine($"\tstring table length: {stringTableLen}");
                            br.BaseStream.Seek(stringTableLen, SeekOrigin.Current);
                        }
                        else
                        {
                            br.ReadInt32(); //skip unknown int
                            int customDataLen = br.ReadInt32();
                            output.AppendLine($"\tstring table length: {customDataLen}");
                            br.BaseStream.Seek(customDataLen, SeekOrigin.Current);
                        }
                        break;
                    case PacketType.STRINGTABLES:
                        int tableOfStringsLen = br.ReadInt32();
                        output.AppendLine($"\tstring table length: {tableOfStringsLen}");
                        br.BaseStream.Seek(tableOfStringsLen, SeekOrigin.Current);
                        break;
                    default:
                        output.AppendLine("^some weird packet^");
                        br.Close();
                        return output.ToString();
                }
            }
            //Console.WriteLine(tracker.ToString());
            br.Close();
            return output.ToString();
        }
        
        private static string[] StringToChunks(string str, int chunkSize) {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize)).ToArray();
        }
    }
}