using System;
using System.Windows.Forms;

namespace NodePyUtil
{
    public static class Prompt
    {
        /// <summary>
        /// https://stackoverflow.com/questions/5427020/prompt-dialog-in-windows-forms
        /// </summary>
        public static DialogResult ShowPrompt(this Form owner, string text, string caption, out string result, string defaultValue = "", string deniedChars = "")
        {
            using (CustomDialog dialog = new CustomDialog(owner, text, caption, defaultValue, deniedChars))
            {
                DialogResult r = dialog.ShowDialog();
                result = dialog.Result;
                return r;
            }
        }
    }

    public sealed class CustomDialog : Form
    {
        public string Result => InputField.Text.Trim();

        public string DeniedChars { get; set; }

        Label TextLabel;
        TextBox InputField;
        Button SubmitButton;
        Button CloseButton;
        string Title;

        public CustomDialog(Form owner, string text, string caption, string defaultValue = "", string deniedChars = "")
        {
            Owner = owner;
            Text = Title = caption;
            DeniedChars = deniedChars;
            Width = 256;
            Height = 128;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            TextLabel = new Label() { Left = 8, Top = 8, Text = text };
            InputField = new TextBox() { Left = 8, Top = 32, Width = 256 - 32, Text = defaultValue };
            SubmitButton = new Button() { Text = "OK", Left = 52, Width = 64, Top = 56, DialogResult = DialogResult.OK };
            CloseButton = new Button() { Text = "Cancel", Left = SubmitButton.Right + 4, Width = 64, Top = 56, DialogResult = DialogResult.Cancel };
            InputField.TextChanged += InputField_TextChanged;

            Controls.Add(TextLabel);
            Controls.Add(InputField);
            Controls.Add(SubmitButton);
            Controls.Add(CloseButton);
            AcceptButton = SubmitButton;
            CancelButton = CloseButton;
        }

        private void InputField_TextChanged(object sender, EventArgs e)
        {
            for(int i = 0; i < DeniedChars.Length; i++)
                InputField.Text = InputField.Text.Replace(DeniedChars[i].ToString(), string.Empty);

            Text = string.Format("{0}: {1}", Title, InputField.Text.Trim());
        }
    }
}
