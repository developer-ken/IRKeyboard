using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IRKeyboard
{
    struct Block
    {
        public byte data0, data1, data2, data3;
        public byte[] ByteArray => new byte[] { data0, data1, data2, data3 };
    }

    struct JumpToPending
    {
        public Block Enterance;
        public byte CommandAddress;
        public bool UseHyper;
    }

    struct ConfBlock
    {
        public Block Enterance;
        public byte Address;
        public bool IsHyper;
    }

    internal class ConfigCompiler
    {
        public static string Log = "";
        public static List<Block> GetBytesFromJsonConfig(JObject root, bool ignore_combos = false)
        {
            List<JumpToPending> jumps = new List<JumpToPending>();
            List<ConfBlock> confblocks = new List<ConfBlock>();
            Log = "JsonCode编译器 By Dev_ken\n";
            Log += "--- Pass 1: 启动块构建 ---\n";
            List<Block> blocks = new List<Block>();
            var init = root["boot"];
            if (init == null)
            {
                Log += "无启动块，保持默认启动逻辑。\n";
                blocks.Add(new Block
                {
                    data0 = 0xFF,
                    data1 = 0xFF,
                    data2 = 0xFF,
                    data3 = 0xFF
                });
            }
            else
            {
                switch (init.Value<string>("type"))
                {
                    case "jump":
                        blocks.Add(Jump(0x00));
                        jumps.Add(new JumpToPending
                        {
                            CommandAddress = (byte)(blocks.Count - 1),
                            Enterance = GetByteBlock(init.Value<string>("target")),
                            UseHyper = init.Value<bool>("target_hyper")
                        });
                        break;
                    case "highpulse":
                        blocks.Add(HighPulseLow(init.Value<byte>("len")));
                        break;
                    case "lowpulse":
                        blocks.Add(LowPulseHigh(init.Value<byte>("len")));
                        break;
                    default:
                        Log += "启动块存在不兼容指令，不会被执行。\n";
                        blocks.Add(new Block
                        {
                            data0 = 0xFF,
                            data1 = 0xFF,
                            data2 = 0xFF,
                            data3 = 0xFF
                        });
                        break;
                }
            }
            int blockid = 0;
            Log += "--- Pass 2: 生成条件块 ---\n";
            foreach (KeyValuePair<String, JToken> jb in root)
            {
                try
                {
                    List<Block> innerblock = new List<Block>();
                    List<ConfBlock> innerconfb = new List<ConfBlock>();
                    List<JumpToPending> innerjumps = new List<JumpToPending>();
                    innerblock.Add(new Block
                    {
                        data0 = 0x00
                    });
                    JToken action = jb.Value;
                    innerblock.Add(GetByteBlock(jb.Key));
                    innerconfb.Add(new ConfBlock
                    {
                        Address = (byte)(innerblock.Count + blocks.Count),
                        Enterance = GetByteBlock(jb.Key),
                        IsHyper = false
                    });
                    switch (action.Value<string>("type"))
                    {
                        case "key":
                            {
                                var keycode = action.Value<byte>("keycode");
                                byte ascii = KeyCodeMapAscii.GetAscii(keycode);
                                if (ascii == 0)
                                {
                                    Log += ("无法编译'" + keycode + "':没有对应的按键指令\n");
                                    continue;
                                }
                                else
                                    innerblock.Add(KeyPress(ascii));
                            }
                            break;
                        case "keycombo":
                            innerblock.AddRange(CompileKeyCombo(action.Value<string>("list")));
                            break;
                        case "cmd":
                            Log += ("无法编译CMD指令:没有对应的硬件指令\n");
                            continue;
                        case "move":
                            innerblock.Add(MouseMove((byte)(127 + action.Value<int>("dx")), (byte)(127 + action.Value<int>("dy")), 127));
                            break;
                        case "lclick":
                            innerblock.Add(MouseClick(BaseCommands.MouseLeft));
                            break;
                        case "rclick":
                            innerblock.Add(MouseClick(BaseCommands.MouseRight));
                            break;
                        case "mclick":
                            innerblock.Add(MouseClick(BaseCommands.MouseMiddle));
                            break;
                        case "roll":
                            innerblock.Add(MouseMove(0, 0, (byte)(127 + action.Value<int>("dist"))));
                            break;
                        case "hyper":
                            innerblock.Add(HyperSwitch());
                            break;
                        case "ldown":
                            innerblock.Add(MouseDown(BaseCommands.MouseLeft));
                            break;
                        case "rdown":
                            innerblock.Add(MouseDown(BaseCommands.MouseRight));
                            break;
                        case "mdown":
                            innerblock.Add(MouseDown(BaseCommands.MouseMiddle));
                            break;
                        case "lup":
                            innerblock.Add(MouseUp(BaseCommands.MouseLeft));
                            break;
                        case "rup":
                            innerblock.Add(MouseUp(BaseCommands.MouseRight));
                            break;
                        case "mup":
                            innerblock.Add(MouseUp(BaseCommands.MouseMiddle));
                            break;
                        case "lswitch":
                            innerblock.Add(MouseSwitch(BaseCommands.MouseLeft));
                            break;
                        case "rswitch":
                            innerblock.Add(MouseSwitch(BaseCommands.MouseRight));
                            break;
                        case "mswitch":
                            innerblock.Add(MouseSwitch(BaseCommands.MouseMiddle));
                            break;
                        case "jump":
                            innerblock.Add(Jump(0x00));
                            innerjumps.Add(new JumpToPending
                            {
                                CommandAddress = (byte)(blocks.Count + innerblock.Count - 1),
                                Enterance = GetByteBlock(action.Value<string>("target")),
                                UseHyper = action.Value<bool>("target_hyper")
                            });
                            break;
                        case "highpulse":
                            innerblock.Add(HighPulseLow(action.Value<byte>("len")));
                            break;
                        case "lowpulse":
                            innerblock.Add(LowPulseHigh(action.Value<byte>("len")));
                            break;
                    }
                    if (action["hyper"] != null)
                    {
                        innerblock.Add(new Block
                        {
                            data0 = 0xFF,
                            data1 = 0xFF,
                            data2 = 0xFF,
                            data3 = 0xFF
                        });
                        innerblock.Add(GetByteBlock(jb.Key));
                        action = action["hyper"];
                        innerconfb.Add(new ConfBlock
                        {
                            Address = (byte)(innerblock.Count + blocks.Count),
                            Enterance = GetByteBlock(jb.Key),
                            IsHyper = true
                        });
                        switch (action.Value<string>("type"))
                        {
                            case "key":
                                {
                                    var keycode = action.Value<byte>("keycode");
                                    byte ascii = KeyCodeMapAscii.GetAscii(keycode);
                                    if (ascii == 0)
                                    {
                                        Log += ("无法编译'" + keycode + "':没有对应的按键指令\n");
                                        continue;
                                    }
                                    else
                                        innerblock.Add(KeyPress(ascii));
                                }
                                break;
                            case "keycombo":
                                innerblock.AddRange(CompileKeyCombo(action.Value<string>("list")));
                                break;
                            case "cmd":
                                Log += ("无法编译CMD指令:没有对应的硬件指令\n");
                                continue;
                            case "move":
                                innerblock.Add(MouseMove((byte)(127 + action.Value<int>("dx")), (byte)(127 + action.Value<int>("dy")), 127));
                                break;
                            case "lclick":
                                innerblock.Add(MouseClick(BaseCommands.MouseLeft));
                                break;
                            case "rclick":
                                innerblock.Add(MouseClick(BaseCommands.MouseRight));
                                break;
                            case "mclick":
                                innerblock.Add(MouseClick(BaseCommands.MouseMiddle));
                                break;
                            case "roll":
                                innerblock.Add(MouseMove(0, 0, (byte)(127 + action.Value<int>("dist"))));
                                break;
                            case "hyper":
                                innerblock.Add(HyperSwitch());
                                break;
                            case "ldown":
                                innerblock.Add(MouseDown(BaseCommands.MouseLeft));
                                break;
                            case "rdown":
                                innerblock.Add(MouseDown(BaseCommands.MouseRight));
                                break;
                            case "mdown":
                                innerblock.Add(MouseDown(BaseCommands.MouseMiddle));
                                break;
                            case "lup":
                                innerblock.Add(MouseUp(BaseCommands.MouseLeft));
                                break;
                            case "rup":
                                innerblock.Add(MouseUp(BaseCommands.MouseRight));
                                break;
                            case "mup":
                                innerblock.Add(MouseUp(BaseCommands.MouseMiddle));
                                break;
                            case "lswitch":
                                innerblock.Add(MouseSwitch(BaseCommands.MouseLeft));
                                break;
                            case "rswitch":
                                innerblock.Add(MouseSwitch(BaseCommands.MouseRight));
                                break;
                            case "mswitch":
                                innerblock.Add(MouseSwitch(BaseCommands.MouseMiddle));
                                break;
                            case "jump":
                                innerblock.Add(Jump(0x00));
                                innerjumps.Add(new JumpToPending
                                {
                                    CommandAddress = (byte)(blocks.Count + innerblock.Count - 1),
                                    Enterance = GetByteBlock(action.Value<string>("target")),
                                    UseHyper = action.Value<bool>("target_hyper")
                                });
                                break;
                            case "highpulse":
                                innerblock.Add(HighPulseLow(action.Value<byte>("len")));
                                break;
                            case "lowpulse":
                                innerblock.Add(LowPulseHigh(action.Value<byte>("len")));
                                break;
                        }
                    }
                    blocks.AddRange(innerblock);
                    confblocks.AddRange(innerconfb);
                    jumps.AddRange(innerjumps);
                }
                catch (Exception err)
                {
                    Log += "Block" + blockid + "指令无效：" + err.Message + "\n";
                }
                blockid++;
            }
            blocks.Add(new Block
            {
                data0 = 0xCC,
                data1 = 0xCC,
                data2 = 0xCC,
                data3 = 0xCC
            });
            Log += "--- Pass 3: 计算跳转地址 ---\n";
            foreach (var j in jumps)
            {
                bool hit = false;
                foreach (var conf in confblocks)
                {
                    if (conf.Enterance.ByteArray.Equals(j.Enterance.ByteArray) && conf.IsHyper == j.UseHyper)
                    {
                        if (blocks[j.CommandAddress].data0 != BaseCommands.JumpTo)
                            throw new ApplicationException("Unexpected command at #" + j.CommandAddress + ":" +
                                "should be '" + BaseCommands.JumpTo + "', '" + blocks[j.CommandAddress].data0 + "' found.");
                        blocks[j.CommandAddress] = new Block
                        {
                            data0 = BaseCommands.JumpTo,
                            data1 = conf.Address
                        };
                        hit = true;
                        Log += "'#" + j.CommandAddress + "' -> '#" + conf.Address + "'\n";
                        break;
                    }
                }
                if (!hit)
                {
                    Log += "跳转指令'#" + j.CommandAddress + "'的目标不存在，将会指向'#0'\n";
                }
            }
            Log += "编译完成，程序大小" + blocks.Count + "指令块，包括空指令块和条件指令块。\n";
            return blocks;
        }

        public static List<Block> CompileKeyCombo(string cmd)
        {
            List<Block> blocks = new List<Block>();
            var match = Regex.Match(cmd, "(?<cmd>[+\\-?.])(?<key>[0123456789]+)");
            while (match.Success)
            {
                string command = match.Groups["cmd"].Value;
                byte key = byte.Parse(match.Groups["key"].Value);
                byte ascii = KeyCodeMapAscii.GetAscii(key);
                if (ascii == 0)
                {
                    throw new InvalidOperationException("无法编译'" + key + "':没有对应的按键指令");
                }
                switch (command)
                {
                    case "+":
                        blocks.Add(KeyDown(ascii));
                        break;
                    case "-":
                        blocks.Add(KeyUp(ascii));
                        break;
                    case ".":
                        blocks.Add(KeyPress(ascii));
                        break;
                    case "?":
                        blocks.Add(KeySwitch(ascii));
                        break;
                }
                match = match.NextMatch();
            }
            return blocks;
        }

        public static byte[] BlockListToByteArray(List<Block> blocklist)
        {
            List<byte> data = new List<byte>();
            foreach (Block blk in blocklist)
            {
                data.AddRange(blk.ByteArray);
            }
            return data.ToArray();
        }

        public static Block GetByteBlock(string hex)
        {
            return new Block
            {
                data0 = Convert.ToByte(hex[..2], 16),
                data1 = Convert.ToByte(hex.Substring(2, 2), 16),
                data2 = Convert.ToByte(hex.Substring(4, 2), 16),
                data3 = Convert.ToByte(hex.Substring(6, 2), 16)
            };
        }

        #region Commands
        public static Block KeyPress(byte asc) => new Block
        {
            data0 = BaseCommands.KeyPress,
            data1 = asc
        };

        public static Block KeyDown(byte asc) => new Block
        {
            data0 = BaseCommands.KeyDown,
            data1 = asc
        };

        public static Block KeyUp(byte asc) => new Block
        {
            data0 = BaseCommands.KeyUp,
            data1 = asc
        };

        public static Block MouseDown(byte asc) => new Block
        {
            data0 = BaseCommands.MouseDown,
            data1 = asc
        };

        public static Block MouseUp(byte asc) => new Block
        {
            data0 = BaseCommands.MouseUp,
            data1 = asc
        };

        public static Block MouseClick(byte asc) => new Block
        {
            data0 = BaseCommands.MouseClick,
            data1 = asc
        };

        public static Block MouseMove(byte dx, byte dy, byte sc) => new Block
        {
            data0 = BaseCommands.MouseMove,
            data1 = dx,
            data2 = dy,
            data3 = sc
        };

        public static Block HyperSwitch() => new Block
        {
            data0 = BaseCommands.HyperSwitch
        };

        public static Block BreathRate(byte val, byte max) => new Block
        {
            data0 = BaseCommands.LightBreath,
            data1 = val,
            data2 = max
        };

        public static Block Brightness(byte val) => new Block
        {
            data0 = BaseCommands.LightLevel,
            data1 = val
        };

        public static Block Jump(byte addr) => new Block
        {
            data0 = BaseCommands.JumpTo,
            data1 = addr
        };

        public static Block MouseSwitch(byte asc) => new Block
        {
            data0 = BaseCommands.MouseSwitch,
            data1 = asc
        };

        public static Block KeySwitch(byte asc) => new Block
        {
            data0 = BaseCommands.KeySwitch,
            data1 = asc
        };

        public static Block HighPulseLow(byte asc) => new Block
        {
            data0 = BaseCommands.HighPulsLow,
            data1 = asc
        };
        public static Block LowPulseHigh(byte asc) => new Block
        {
            data0 = BaseCommands.LowPulsHigh,
            data1 = asc
        };
        #endregion
    }
}
