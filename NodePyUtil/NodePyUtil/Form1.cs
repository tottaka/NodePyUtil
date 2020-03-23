using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Media;
using System.Threading;
using System.Windows.Forms;

namespace NodePyUtil
{
    public partial class Form1 : Form
    {
        public string Port;
        public int Baud = 115200;
        public NodeMCU Device;

        public Dictionary<string, SafeFileSystemWatcher> OpenFiles = new Dictionary<string, SafeFileSystemWatcher>();

        private string TempPath => Path.Combine(Path.GetTempPath(), Application.ProductName);

        public string SelectedFilePath => treeView1.SelectedNode.FullPath.Remove(0, 1).Trim();

        public string SelectedFileName => treeView1.SelectedNode.Text.Trim();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            treeView1.TopNode.Tag = new NodeFile { IsDirectory = true, Name = "/" };
            toolStripComboBox1.Items.AddRange(SerialPort.GetPortNames());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Device?.Dispose();
        }

        private void toolStripComboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            Port = (string)toolStripComboBox1.SelectedItem;
        }

        private void toolStripComboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(((string)toolStripComboBox1.SelectedItem).Replace("(default)", string.Empty).Trim(), out Baud))
                MessageBox.Show("The specified baud rate is invalid.", "Unable to set baud rate", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Device = new NodeMCU(Port, Baud, 10000);
                EnableConnectedControls();
                List("/");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to open device", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List("/");
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node == treeView1.TopNode || (e.Node.Tag is NodeFile && (e.Node.Tag as NodeFile).IsDirectory))
                return;

            Enabled = false;
            
            try
            {
                string selectedFile = SelectedFilePath.Trim().Replace('\\', '/').Trim();
                string localFilePath = GetLocalPath(selectedFile);
                Download(selectedFile, localFilePath);

                if (!OpenFiles.ContainsKey(selectedFile)) {
                    SafeFileSystemWatcher watcher = new SafeFileSystemWatcher(Path.GetDirectoryName(localFilePath), Path.GetFileName(localFilePath));
                    watcher.Changed += (s, eventArgs) => {
                        Invoke((MethodInvoker)delegate {
                            Enabled = false;

                            Device.Upload(eventArgs.FullPath, selectedFile);
                            SystemSounds.Hand.Play();

                            Enabled = true;
                        });
                    };
                    OpenFiles.Add(selectedFile, watcher);
                }

                Thread.Sleep(1000);
                System.Diagnostics.Process.Start(localFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occured", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Enabled = true;
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag == null)
                List(e.Node.FullPath.Remove(0, 1).Replace("\\", "/").Trim());
        }

        private void treeView1_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node == treeView1.TopNode)
                e.CancelEdit = true;
        }

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            // rename the ding dong
            Console.WriteLine("Rename: {0}, {1}", e.Label, e.Node.Text);
            Enabled = false;

            string fullPath = e.Node.FullPath.Remove(0, 1).Replace("\\", "/").Trim();
            string newPath = fullPath.Remove(fullPath.Length - e.Node.Text.Length, e.Node.Text.Length) + e.Label;
            Device.Rename(fullPath, newPath);
            (e.Node.Tag as NodeFile).Name = e.Label;

            Enabled = true;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            bool isScript = Path.GetExtension(SelectedFileName).ToLower() == ".py";
            bool isRoot = SelectedFileName == "\\";
            runToolStripMenuItem.Visible = isScript;
            toolStripSeparator2.Visible = isScript;
            deleteToolStripMenuItem.Visible = !isRoot;
            renameToolStripMenuItem.Visible = !isRoot;
            Enabled = Device != null; 
        }

        private void newFileToolStripMenuItem_Click(object sender, EventArgs eventArgs)
        {
            Enabled = false;

            string selectedFile = SelectedFilePath.Trim();
            if (this.ShowPrompt("File name:", $"Create new file: \\{selectedFile}", out string fileName, allowedChars: "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ_-.1234567890") == DialogResult.OK)
            {
                NodeFile file = (NodeFile)treeView1.SelectedNode.Tag;
                if (file.IsDirectory)
                {
                    // create the new file in this directory
                    Device.CreateFile(string.Format("{0}/{1}", SelectedFilePath.Replace("\\", "/"), fileName));
                    treeView1.SelectedNode.Nodes.Add(new TreeNode(fileName) { Tag = new NodeFile { Name = fileName, IsDirectory = false } });
                }
                else
                {
                    // create a new file in this file's containing directory
                    string parentDir = SelectedFilePath.Substring(0, SelectedFilePath.Length - file.Name.Length);
                    Device.CreateFile(string.Format("{0}/{1}", parentDir.Replace("\\", "/"), fileName));
                    GetPathNode(parentDir).Nodes.Add(new TreeNode(fileName) { Tag = new NodeFile { Name = fileName, IsDirectory = false } });
                }
            }

            Enabled = true;
        }

        private void newFolderToolStripMenuItem_Click(object sender, EventArgs eventArgs)
        {
            Enabled = false;

            string selectedFile = SelectedFilePath.Trim();
            if (this.ShowPrompt("Folder name:", $"Create new folder: \\{selectedFile}", out string fileName, allowedChars: "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ_-1234567890") == DialogResult.OK)
            {
                NodeFile file = (NodeFile)treeView1.SelectedNode.Tag;
                if (file.IsDirectory)
                {
                    // create the new directory in this directory
                    Device.CreateDirectory(string.Format("{0}/{1}", treeView1.SelectedNode.FullPath.Replace("\\", "/").Trim(), fileName));
                    treeView1.SelectedNode.Nodes.Add(new TreeNode(fileName, new TreeNode[] { new TreeNode("") { Tag = null } }) { Tag = new NodeFile { Name = fileName, IsDirectory = true } });
                }
                else
                {
                    // create the new directory in this file's containing directory
                    string parentDir = treeView1.SelectedNode.FullPath.Substring(1, treeView1.SelectedNode.FullPath.Length - file.Name.Length - 1);
                    Device.CreateDirectory(string.Format("{0}/{1}", parentDir.Replace("\\", "/").Trim(), fileName));
                    GetPathNode(parentDir).Nodes.Add(new TreeNode(fileName, new TreeNode[] { new TreeNode("") { Tag = null } }) { Tag = new NodeFile { Name = fileName, IsDirectory = true } });
                }
            }

            Enabled = true;
        }

        private void refreshToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            NodeFile file = (NodeFile)treeView1.SelectedNode.Tag;
            string path = SelectedFilePath.Replace("\\", "/").Trim();
            if (file.IsDirectory)
                List(path);
            else List(path.Substring(0, path.Length - file.Name.Length));
        }

        private void resetDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;

            Device.Reset();

            Enabled = true;
            MessageBox.Show($"The device on selected port '{Port}' has been reset.", "Device reset successful", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // delete
            NodeFile file = (NodeFile)treeView1.SelectedNode.Tag;
            if (MessageBox.Show(string.Format("Are you sure you want to remove the {0} at '{1}'?\nYou can't undo this action.", file.IsDirectory ? "directory" : "file", SelectedFilePath.Replace("\\", "/").Trim()), "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                if (file.IsDirectory)
                    Device.DeleteDirectory(SelectedFilePath.Replace("\\", "/").Trim());
                else Device.DeleteFile(SelectedFilePath.Replace("\\", "/").Trim());
                treeView1.SelectedNode.Remove();
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.SelectedNode.BeginEdit();
        }

        private void uploadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // upload file
            using (OpenFileDialog openFile = new OpenFileDialog { AddExtension = true, CheckFileExists = true, Multiselect = true, Title = "Select file(s) to upload..." })
            {
                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    NodeFile node = (NodeFile)treeView1.SelectedNode.Tag;
                    string uploadDir = (node.IsDirectory ? SelectedFilePath.Replace("\\", "/").Trim() : SelectedFilePath.Substring(1, SelectedFilePath.Length - node.Name.Length - 1)) + "/";
                    foreach (string file in openFile.FileNames)
                    {
                        string fileName = Path.GetFileName(file);
                        Upload(file, uploadDir + fileName);
                    }
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // SAVE AS...
            Enabled = false;

            NodeFile node = (NodeFile)treeView1.SelectedNode.Tag;
            if (node.IsDirectory)
            {
                using (FolderBrowserDialog openFolder = new FolderBrowserDialog { ShowNewFolderButton = true, Description = "Select where to save the folder..." })
                {
                    if (openFolder.ShowDialog() == DialogResult.OK)
                    {
                        DownloadFolder(SelectedFilePath.Replace("\\", "/").Trim(), Path.Combine(openFolder.SelectedPath, node.Name == "/" ? "root" : node.Name));
                    }
                }
            }
            else
            {
                using (SaveFileDialog saveFile = new SaveFileDialog { Title = "Select where to save the file...", FileName = node.Name, OverwritePrompt = true })
                {
                    if (saveFile.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFile = SelectedFilePath.Trim().Replace('\\', '/').Trim();
                        Download(selectedFile, saveFile.FileName);
                    }
                }
            }

            Enabled = true;
        }

        private void uploadFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // upload folder
            using (FolderBrowserDialog openFolder = new FolderBrowserDialog { Description = "Select the folder to upload..." })
            {
                if (openFolder.ShowDialog() == DialogResult.OK)
                {
                    NodeFile file = (NodeFile)treeView1.SelectedNode.Tag;
                    string folderName = GetFolderName(openFolder.SelectedPath);
                    if (file.IsDirectory)
                    {
                        UploadFolder(openFolder.SelectedPath, SelectedFilePath.Replace("\\", "/").Trim() + folderName);
                    }
                    else
                    {
                        // create a new file in this file's containing directory
                        string parentDir = SelectedFilePath.Substring(0, SelectedFilePath.Length - file.Name.Length).Replace("\\", "/").Trim();
                        UploadFolder(openFolder.SelectedPath, parentDir + folderName);
                    }
                }
            }
        }
        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // create repl window and run script file in it
            // also display the script output in the repl window
            string scriptFile = SelectedFilePath.Replace("\\", "/").Trim();
            MessageBox.Show(Device.ExecuteFile(scriptFile).Trim(), $"Output from '{scriptFile}'");
        }

        public void EnableConnectedControls()
        {
            portSettingsToolStripMenuItem.Enabled = false;
            deviceToolStripMenuItem.Enabled = true;
            refreshToolStripMenuItem.Enabled = true;
            treeView1.TopNode.Nodes.Clear();
            treeView1.Enabled = true;
        }

        public void DisableConnectedControls()
        {
            portSettingsToolStripMenuItem.Enabled = true;
            deviceToolStripMenuItem.Enabled = false;
            refreshToolStripMenuItem.Enabled = false;
            treeView1.TopNode.Nodes.Clear();
            treeView1.Enabled = false;
        }

        public string GetLocalPath(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(Port))
                throw new InvalidOperationException("COM port is not selected.");

            remotePath = remotePath.Trim();
            string dir = Path.GetDirectoryName(remotePath);
            string absoluteDir = Path.Combine(TempPath, "_" + Port, dir.TrimStart('/', '\\').Trim());
            string fileName = Path.GetFileName(remotePath);
            return Path.Combine(absoluteDir, fileName.Trim());
        }

        public TreeNode GetPathNode(string path)
        {
            path = path.Replace("/", "\\").Trim();
            string[] dirs = path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (dirs.Length == 0)
                return treeView1.TopNode;

            TreeNode CurrentNode = treeView1.TopNode.GetNodeByName(dirs[0]);
            for (int i = 1; i < dirs.Length; i++)
                CurrentNode = CurrentNode.GetNodeByName(dirs[i]);
            return CurrentNode;
        }

        private void List(string dir)
        {
            Enabled = false;
            TreeNode node = GetPathNode(dir);
            List<NodeFile> nodes = Device.List(dir.Trim());
            node.Nodes.Clear();
            foreach (NodeFile file in nodes)
            {
                TreeNode n = new TreeNode(file.Name.Trim()) { Tag = file };
                if (file.IsDirectory)
                    n.Nodes.Add(new TreeNode("") { Tag = null });
                node.Nodes.Add(n);
            }

            node.Expand();
            Enabled = true;
        }

        public void Download(string path, string localPath, int chunkSize = 128)
        {
            string filePath = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            Device.Download(path, localPath, chunkSize);
        }

        public void Upload(string localPath, string path, int chunkSize = 128)
        {
            string fileName = Path.GetFileName(path);
            string uploadDir = Path.GetDirectoryName(path);
            Device.Upload(localPath, path);
            GetPathNode(uploadDir).Nodes.Add(new TreeNode(fileName) { Tag = new NodeFile { Name = fileName, IsDirectory = false } });
        }

        private void DownloadFolder(string path, string localPath, int chunkSize = 128)
        {
            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);

            List<NodeFile> entries = Device.List(path.Trim());
            foreach (NodeFile entry in entries)
            {
                string entryName = (path.EndsWith("/") ? path : path + "/") + entry.Name;
                if (entry.IsDirectory)
                    DownloadFolder(entryName, Path.Combine(localPath, entry.Name), chunkSize);
                else
                    Download(entryName, Path.Combine(localPath, entry.Name), chunkSize);
            }
        }

        private void UploadFolder(string localPath, string path, int chunkSize = 128)
        {
            Console.WriteLine("Upload Folder: {0}, {1}", localPath, path);
            if (!Device.Exists(path))
            {
                Device.CreateDirectory(path);
                string folderName = GetFolderName(path);
                GetPathNode(Path.GetDirectoryName(path)).Nodes.Add(new TreeNode(folderName) { Tag = new NodeFile { Name = folderName, IsDirectory = true } });
            }

            string[] entries = Directory.GetFileSystemEntries(localPath);
            foreach(string entry in entries)
            {
                string entryName = path.EndsWith("/") ? path : path + "/";

                FileAttributes attrs = File.GetAttributes(entry);
                if (attrs.HasFlag(FileAttributes.Directory))
                    UploadFolder(entry, entryName + GetFolderName(entry), chunkSize);
                else
                    Upload(entry, entryName + Path.GetFileName(entry), chunkSize);
            }
        }

        private static string GetFolderName(string path)
        {
            string[] tree = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            return tree[tree.Length - 1].TrimStart('\\', '/').TrimEnd('\\', '/').Trim();
        }
    }

    public static class TreeViewExtensions
    {
        public static TreeNode GetNodeByName(this TreeNode node, string name)
        {
            foreach (TreeNode n in node.Nodes)
                if (n.FullPath == node.FullPath + "\\" + name)
                    return n;
            return null;
        }
    }
}
