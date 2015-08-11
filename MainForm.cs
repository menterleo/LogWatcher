using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogWatcher
{
    public partial class MainForm : Form
    {
        public MainForm(string getfilenmae)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(getfilenmae))
            {
                filename = getfilenmae;
            }
        }

        static long oldlenght = 0;
        string filename
        {
            get { return textBox2.Text; }

            set
            {
                textBox2.Text = value;
                try
                {
                    this.Text = System.IO.Path.GetFileName(value) + "-LogWatcher";
                }
                catch { }
            }
        }
        Encoding enc = System.Text.Encoding.GetEncoding("UTF-8");
        //Encoding enc
        //{
        //    get
        //    {
        //        return ;
        //    }
        //}

        System.IO.FileSystemWatcher fsw = new System.IO.FileSystemWatcher();

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            fsw.Changed += new System.IO.FileSystemEventHandler(fsw_Changed);
            if (!string.IsNullOrEmpty(filename))
            {
                StartRead();
                if (textBox1.Text.Length > 1)
                {
                    textBox1.Select(textBox1.Text.Length - 1, 0);
                }
                checkBox1.Checked = false;
            }
        }

        void fsw_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            read();
            StartFlashWindow(this, false);
            //StartFlashWindow(this.Handle, true);  //一闪闪
            //StartFlashWindow(this.Handle, false); //保持高亮橘黄色 
        }

        private void read()
        {
            try
            {
                System.IO.FileStream fs = null;
                bool canread = false;
                long fsLength = 0;
                if (!System.IO.File.Exists(filename))
                {
                    SetText("文件不存在。");
                    SetText("\n");
                    return;
                }

                int trycount = 0;
                while (!canread)
                {
                    try
                    {
                        fs = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                        fsLength = fs.Length;
                        canread = true;
                    }
                    catch (Exception ex)
                    {
                        if (!System.IO.File.Exists(filename))
                        {
                            SetText("文件不存在。");
                            SetText("\n");
                            return;
                        }
                        if (trycount++ > 100)
                        {
                            SetText("打开文件失败：");
                            SetText(ex.Message);
                            SetText("\n");
                            return;
                        }
                        System.Threading.Thread.Sleep(1);
                    }
                }
                try
                {
                    if (null != fs)
                    {
                        if (fsLength <= oldlenght) oldlenght = fsLength;
                        if (fsLength - oldlenght > 10485760) oldlenght = fsLength - 10485760;//最大读取10M数据
                        if (oldlenght < 0) oldlenght = 0;
                        fs.Seek(oldlenght, System.IO.SeekOrigin.Begin);
                        byte[] readdata = new byte[fsLength - oldlenght];
                        fs.Read(readdata, 0, readdata.Length);
                        string newstr = enc.GetString(readdata);
                        SetText(newstr);
                        oldlenght = fsLength;
                    }
                    fs.Close();
                }
                catch (Exception ex)
                {
                    SetText("日志查看器本身错误：" + ex.Message);
                    SetText("\n");
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                SetText("日志查看器本身错误：" + ex.Message);
                SetText("\n");
            }
        }

        private delegate void SetLabelDelegate(string value);

        private void SetText(string value)
        {
            if (this.InvokeRequired)
            {
                SetLabelDelegate d = new SetLabelDelegate(SetText);
                this.Invoke(d, new object[] { value });
            }
            else
            {
                textBox1.AppendText(value);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                StartRead();
                checkBox1.Checked = false;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32")]
        public static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        private delegate void StartFlashWindowDelegate(Form frm, bool bInvert);
        private void StartFlashWindow(Form frm, bool bInvert)
        {
            if (this.InvokeRequired)
            {
                StartFlashWindowDelegate d = new StartFlashWindowDelegate(StartFlashWindow);
                this.Invoke(d, new object[] { frm, bInvert });
            }
            else
            {
                FlashWindow(frm.Handle, bInvert);
            }
        }

        private void StartRead()
        {
            if (!System.IO.File.Exists(filename))
            {
                textBox1.Text = "未找到日志文件！";
                return;
            }
            //enc = EncodingType.GetType(filename);
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            fsw.EnableRaisingEvents = false;
            fsw.Path = fi.DirectoryName;
            fsw.Filter = fi.Name;
            textBox1.Clear();
            oldlenght = 0;
            read();
            fsw.EnableRaisingEvents = true;
            textBox1.Focus();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            enc = System.Text.Encoding.GetEncoding(comboBox1.Text);
            StartRead();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                read();
                checkBox1.Text = "Pause";
            }
            else
            {
                checkBox1.Text = "Start";
            }
            if (System.IO.File.Exists(filename))
                fsw.EnableRaisingEvents = !checkBox1.Checked;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            // 对文件拖拽事件做处理    
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var filePath = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in filePath)
            {
                filename = file;
            }
            StartRead();
            checkBox1.Checked = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBox2.Checked;
            //this.AllowTransparency = checkBox2.Checked;
            //this.Opacity = this.AllowTransparency ? 0.80 : 1;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.WordWrap = checkBox3.Checked;
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                StartRead();
                checkBox1.Checked = false;
            }
        }
    }
}
