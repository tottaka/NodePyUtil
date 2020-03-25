using System;
using System.Threading;
using System.Windows.Forms;

namespace NodePyUtil
{
    public partial class ReplForm : Form
    {
        private Action<string, Action<string>, Action, ManualResetEventSlim> Execute;
        private bool CanClose = true;
        private ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

        public ReplForm(Action<string, Action<string>, Action, ManualResetEventSlim> exeCallback)
        {
            Execute = exeCallback;
            InitializeComponent();
        }

        private void ReplForm_Load(object sender, EventArgs e)
        {

        }

        private void ReplForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = e.CloseReason == CloseReason.UserClosing && !CanClose;
            resetEvent.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
                return;

            RunCommand(textBox1.Text.Trim());
            textBox1.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = ">>>";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            resetEvent.Set();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                button1_Click(this, EventArgs.Empty);
            }
        }

        public void Append(string text)
        {
            Invoke((MethodInvoker)delegate {
                richTextBox1.AppendText(text);
                if(checkBox1.Checked)
                {
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                }
            });
        }

        public void RunCommand(string command)
        {
            try
            {
                textBox1.Enabled = false;
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = true;
                CanClose = false;
                richTextBox1.AppendText(command + Environment.NewLine);
                Execute(command, Append, PostRunCommand, resetEvent);
            }
            catch (InvalidOperationException ex)
            {
                Append(ex.Message + Environment.NewLine);
            }
        }

        private void PostRunCommand()
        {
            textBox1.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = false;
            CanClose = true;
            resetEvent.Reset();
            Append(Environment.NewLine + ">>>");
        }
    }
}
