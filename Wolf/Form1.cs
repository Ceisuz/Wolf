﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Celtic_Guardian;

namespace Wolf
{
    public partial class Form1 : Form
    {
        public static string InstallDir;
        public static StreamReader Reader;
        public static List<FileData> Data = new List<FileData>();

        public Form1()
        {
            InitializeComponent();
            FileQuickViewList.ImageList = NodeImages;
            MainFileView.SmallImageList = NodeImages;
        }
        private void Form1_Load(object Sender, EventArgs Args)
        {
            try
            {
                using (var Root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var Key = Root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 480650"))
                    {
                        InstallDir = Key.GetValue("InstallLocation").ToString();
                    }
                }
            }
            catch (Exception)
            {
                throw new FileNotFoundException("Can't Find Game");
            }

            GameLocLabel.ForeColor = Color.Red;
            GameLocLabel.Text = "Game Not Loaded";
        }

        private void ExitToolStripMenuItem_Click(object Sender, EventArgs Args)
        {
            if (Reader.BaseStream != null)
            {
                var Reply = MessageBox.Show(this, "The Yu-Gi-Oh! Data File Is Still Loaded, Are You Sure You Want To Quit?", "Data File Still In Use...", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (Reply != DialogResult.Yes) return;

                Reader.Dispose();
            }
            Application.Exit();
        }

        private void OpenToolStripMenuItem_Click(object Sender, EventArgs Args)
        {
            //Setup StreamReader for TOC File.
            Reader?.Close();
            Reader = new StreamReader(File.Open($"{InstallDir}\\YGO_DATA.TOC", FileMode.Open, FileAccess.Read));
            Reader.ReadLine();

            //UI Information.
            GameLocLabel.ForeColor = Color.Green;
            GameLocLabel.Text = "Game Loaded";

            //Read TOC File.
            var RootNode = new TreeNode("YGO_DATA");
            FileQuickViewList.Nodes.Add(RootNode);
            while (!Reader.EndOfStream)
            {
                var Line = Reader.ReadLine();
                if (Line == null) continue;
                Line = Line.TrimStart(' ');
                Line = Regex.Replace(Line, @"  +", " ", RegexOptions.Compiled);
                var LineData = Line.Split(' ');
                Data.Add(new FileData(Utilities.HexToDec(LineData[0]), Utilities.HexToDec(LineData[1]), LineData[2]));
                LineData[2].Split('\\').Aggregate(RootNode, (Current, File) => Current.Nodes.ContainsKey(File) ? Current.Nodes[File] : Current.Nodes.Add(File, File));

                LineData[2].Split('\\').Aggregate(RootNode, (Current, File) => Current.Nodes.ContainsKey(File) ? Current.Nodes[File] : Current.Nodes.Add(File, File));
            }
            GiveIcons(FileQuickViewList.Nodes[0]);
            FileQuickViewList.Nodes[0].Expand();


            //Load Side Bar AND Main Bar. (Seperate Thread???) (Eh...)
            //Do Things And Stuff.
        }

        private void CloseToolStripMenuItem_Click(object Sender, EventArgs Args)
        {
            Reader.Close();
            GameLocLabel.ForeColor = Color.Red;
            GameLocLabel.Text = "Game Not Loaded";
        }

        private void FileQuickViewList_NodeMouseClick(object Sender, TreeNodeMouseClickEventArgs Args)
        {
            if (Args.Node.Nodes.Count <= 0) return;
            MainFileView.Items.Clear();
            MainFileView.LargeImageList = NodeImages;
            switch (Args.Button)
            {
                case MouseButtons.Left:
                    for (var Node = 0; Node < Args.Node.Nodes.Count; Node++)
                    {
                        var Items = new ListViewItem(Args.Node.Nodes[Node].Name);
                        var FileSizeObject = Data.Where(Item => Item.Item3.Contains(Args.Node.Nodes[Node].Text)).Select(NodeSize => NodeSize.Item1).FirstOrDefault();

                        Items.SubItems.Add(Utilities.GiveFileSize(FileSizeObject));
                        MainFileView.Items.Add(Items);


                        MainFileView.Items[Node].ImageIndex = Args.Node.Nodes[Node].Nodes.Count == 0 ? 1 : 0;
                        if (Args.Node.Nodes[Node].Name.EndsWith(".png") || Args.Node.Nodes[Node].Name.EndsWith(".jpg"))
                        {
                            MainFileView.Items[Node].ImageIndex = 2;
                        }
                    }
                    break;
                case MouseButtons.Right:
                    break;
            }

            MainFileView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            MainFileView.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

        }

        private void MainFileView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (MainFileView.SelectedItems[0].ImageIndex != 0) return;

            var SelectMe = GetNode(FileQuickViewList.Nodes[0]);
            FileQuickViewList_NodeMouseClick(new object(),
                new TreeNodeMouseClickEventArgs(SelectMe, MouseButtons.Left, 1, 0, 0));
        }

        private TreeNode GetNode(TreeNode CurrentNode)
        {
            var EndNode = new TreeNode();
            foreach (TreeNode Node in CurrentNode.Nodes)
            {
                if (Node.Text == MainFileView.SelectedItems[0].Text)
                    return Node;

                GetNode(Node);
            }

            return EndNode;
        }

        private static void GiveIcons(TreeNode RootNode)
        {
            foreach (TreeNode Node in RootNode.Nodes)
            {
                if (Node.Nodes.Count != 0)
                {
                    Node.ImageIndex = 0;
                    Node.SelectedImageIndex = 0;
                }
                else if (Node.Text.EndsWith("jpg") || Node.Text.EndsWith("png"))
                {
                    Node.SelectedImageIndex = 2;
                    Node.ImageIndex = 2;
                }
                else
                {
                    Node.ImageIndex = 1;
                    Node.SelectedImageIndex = 1;
                }
                GiveIcons(Node);
            }
        }
    }
}