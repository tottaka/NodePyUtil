using System;
using System.Windows.Forms;

namespace NodePyUtil
{
    public partial class ReplForm : Form
    {
        private Action<string, Action<string>, Action, int> Execute;
        private bool CanClose = false;

        public ReplForm(Action<string, Action<string>, Action, int> exeCallback)
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
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
                return;

            RunCommand(textBox1.Text.Trim());
            textBox1.Text = string.Empty;
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
            });
        }

        public void RunCommand(string command, int timeout = int.MaxValue)
        {
            try
            {
                textBox1.Enabled = false;
                CanClose = false;
                richTextBox1.AppendText(command + Environment.NewLine);
                Execute(command, Append, PostRunCommand, timeout);
            }
            catch (InvalidOperationException ex)
            {
                Append(ex.Message + Environment.NewLine);
            }
        }

        private void PostRunCommand()
        {
            textBox1.Enabled = true;
            CanClose = true;
            Append(Environment.NewLine + ">>>");
        }
    }
}
