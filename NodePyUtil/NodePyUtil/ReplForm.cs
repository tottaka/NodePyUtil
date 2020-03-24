using System;
using System.Windows.Forms;

namespace NodePyUtil
{
    public partial class ReplForm : Form
    {
        private SimpleRepl.ReplExecDelegate Execute;

        public ReplForm(SimpleRepl.ReplExecDelegate exeCallback)
        {
            Execute = exeCallback;
            InitializeComponent();
        }

        private void ReplForm_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
                return;


            richTextBox1.AppendText(textBox1.Text + Environment.NewLine);

            try
            {
                Append(Execute(textBox1.Text));
            }
            catch(InvalidOperationException ex)
            {
                Append(ex.Message);
            }

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
            richTextBox1.AppendText(text + Environment.NewLine + ">>>");
        }
    }
}
