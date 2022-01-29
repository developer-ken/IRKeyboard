﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IRKeyboard
{
    public partial class Form1 : Form
    {
        EventUART uart;
        JObject mapping;
        Dictionary<byte, bool> keystatus;
        //Form2 hyperico;
        bool hyper = false;
        bool hardwareio = false;
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            keystatus = new Dictionary<byte, bool>();
            if (File.Exists("mapping.json"))
            {
                mapping = JObject.Parse(File.ReadAllText("mapping.json"));
            }
            else
            {
                mapping = new JObject();
            }
            //hyperico = new Form2();
            Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var ports = UART.ListPorts();
            if (ports.Count() == 0)
            {
                notifyIcon1.ShowBalloonTip(2, "红外遥控器", "没有连接到接收器", ToolTipIcon.Error);
                Process.GetCurrentProcess().Kill();
                return;
            }
            var port = ports.Last();
            uart = new EventUART(port, 9600);
            uart.LineReceived += Uart_LineReceived;
            initcmd();
            Hide();
            hardwareio = mapping.ContainsKey("UseHardwareInput")
                   && mapping.Value<bool>("UseHardwareInput");

            notifyIcon1.ShowBalloonTip(2, "红外遥控器", "红外遥控器已启用\n" + (hardwareio ? "正在使用VDevice" : "正在使用WinApi"), ToolTipIcon.None);
            SendCommand(BaseCommands.ProbeEEPROM);
            SendCommand(BaseCommands.LightLevel, 100);
        }

        int keycode = -1;
        string lastcode = "0";
        DateTime lasttime;

        private void Uart_LineReceived(string obj)
        {
            if (obj[0] == '!') return;
            //label1.Text = obj;

            if (obj == "EEPENABLED")
            {
                SendCommand(BaseCommands.LightLevel, 0);
                notifyIcon1.ShowBalloonTip(2, "设备EEPROM中含有配置文件", "该配置文件的优先级将高于本地配置文件。\n" +
                    "如果希望使用本地配置文件，应当擦除设备中的配置文件。", ToolTipIcon.None);
            }
            else
            if (obj.IndexOf("DEHYPER") > -1)
            {
                hyper = false;
                notifyIcon1.Icon = Properties.Resources.normal;
            }
            else
            if (obj.IndexOf("HYPER") > -1)
            {
                hyper = true;
                notifyIcon1.Icon = Properties.Resources.hyper;
            }

            if (keycode != -1)
            {
                JObject action = new JObject();
                action.Add("type", "key");
                action.Add("keycode", keycode);

                if (!mapping.ContainsKey(obj))
                    mapping.Add(obj, action);
                else
                    mapping[obj] = action;
                label1.Text = "Saved.";
                keycode = -1;
            }
            else
            {
                if (obj == "0")
                {
                    if (lastcode != "0" && (DateTime.Now - lasttime).TotalSeconds < 2)
                        obj = lastcode;
                    else return;
                }
                else
                    lastcode = obj;
                lasttime = DateTime.Now;
                bool hardwareio = mapping.ContainsKey("UseHardwareInput")
                    && mapping.Value<bool>("UseHardwareInput");

                Keypresser.SendHWCmd = SendCommand;
                Keypresser.UseHardwareInput = hardwareio;
                Mouse.UseHardwareInput = hardwareio;
                Mouse.SendHWCmd = SendCommand;

                if (mapping.ContainsKey(obj))
                {
                    var action = mapping[obj];
                    if (hyper && action.Value<string>("type") != "hyper" && action["hyper"] != null)
                    {
                        action = action["hyper"];//hyper模式，切换操作表
                    }
                    switch (action.Value<string>("type"))
                    {
                        case "key":
                            {
                                var keycode = action.Value<byte>("keycode");
                                Keypresser.KeyPress(keycode);
                            }
                            break;
                        case "keycombo":
                            RunKeyCombo(action.Value<string>("list"));
                            break;
                        case "cmd":
                            RunCommand(action.Value<string>("command"));
                            break;
                        case "move":
                            Mouse.MouseMove(action.Value<int>("dx"), action.Value<int>("dy"));
                            break;
                        case "lclick":
                            Mouse.MouseLDown();
                            Mouse.MouseLUp();
                            break;
                        case "rclick":
                            Mouse.MouseRDown();
                            Mouse.MouseRUp();
                            break;
                        case "roll":
                            Mouse.MouseWheel(action.Value<int>("dist"));
                            break;
                        case "hyper":
                            hyper = !hyper;
                            if (hyper)
                            {
                                notifyIcon1.Icon = Properties.Resources.hyper;
                                SendCommand(BaseCommands.LightBreath, 26, 255);
                            }
                            else
                            {
                                notifyIcon1.Icon = Properties.Resources.normal;
                                SendCommand(BaseCommands.LightLevel, 100);
                            }
                            break;
                    }
                }
            }
        }

        private void SendCommand(byte cmd, byte arg0 = 0x00, byte arg1 = 0x00, byte arg2 = 0x00)
        {
            byte[] send = new byte[] { cmd, arg0, arg1, arg2 };
            uart.uart.port.Write(send, 0, 4);
        }

        private void RunKeyCombo(string cmd)
        {
            var match = Regex.Match(cmd, "(?<cmd>[+\\-?.])(?<key>[0123456789]+)");
            while (match.Success)
            {
                string command = match.Groups["cmd"].Value;
                byte key = byte.Parse(match.Groups["key"].Value);
                switch (command)
                {
                    case "+":
                        Keypresser.KeyDown(key);
                        break;
                    case "-":
                        Keypresser.KeyUp(key);
                        break;
                    case ".":
                        Keypresser.KeyPress(key);
                        break;
                    case "?":
                        {
                            if (keystatus.ContainsKey(key))
                            {
                                if (keystatus[key])
                                {
                                    Keypresser.KeyUp(key);
                                }
                                else
                                {
                                    Keypresser.KeyDown(key);
                                }
                                keystatus[key] = !keystatus[key];
                            }
                        }
                        break;
                }
                match = match.NextMatch();
            }
        }

        Process cmd;
        private void initcmd()
        {
            cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.Start();
            cmd.StandardInput.AutoFlush = true;
        }


        private void RunCommand(string cmd)
        {
            if (this.cmd.HasExited)
            {
                initcmd();
            }
            //向cmd窗口发送输入信息
            this.cmd.StandardInput.WriteLine(cmd);
        }

        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            keycode = (int)e.KeyCode;
            label1.Text = keycode.ToString();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //File.WriteAllText("mapping.json", mapping.ToString());
            Hide();
            e.Cancel = true;
        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void Form1_Enter(object sender, EventArgs e)
        {
            Hide();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void 烧写配置ProgramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("您将要把本地配置文件烧写到设备，设备中的原有配置文件会被覆盖。\n" +
                "烧写成功后，部分功能可脱离本程序使用。对于存在配置文件的设备，将会优先使用设备中的配置文件。\n" +
                "部分无法编译成设备配置文件的操作仍将读取本地配置文件。", "将配置文件烧写到控制器", MessageBoxButtons.OKCancel);
            if (result == DialogResult.OK)
            {
                var bin = ConfigCompiler.GetBytesFromJsonConfig(mapping);
                if (bin.Count > 256)
                {
                    MessageBox.Show("您的本地配置文件转换为设备配置文件后太大。\n" +
                        "请尝试删减部分操作或使用Jump复用重复操作。\n" +
                        "配置文件：" + bin.Count + " 块\n" +
                        "设备可用容量：256 块", "无法写入配置文件", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                MessageBox.Show(ConfigCompiler.Log, "编译完成");
                WriteBinaryToEEPROM(bin);
                uart.uart.Abort();
                uart.uart.Init();
                notifyIcon1.ShowBalloonTip(2, "红外遥控器", "写入完成，重启本程序、重新插拔接收器来获得完整体验。\n共写入" + bin.Count + "个数据块", ToolTipIcon.None);
            }
        }

        private void WriteBinaryToEEPROM(List<Block> blocks)
        {
            SendCommand(BaseCommands.ProgramEEPROM);
            foreach (Block blk in blocks)
            {
                uart.uart.port.Write(blk.ByteArray, 0, 4);
            }
        }
    }
}
