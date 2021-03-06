﻿using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TROPHYParser;

namespace PS3TrophyIsGood
{
    public partial class Form1 : Form {
        TROPCONF tconf;
        TROPTRNS tpsn;
        TROPUSR tusr;
        string path;
        DateTimePickForm dtpForm = null;
        DateTimePickForm dtpfForInstant = null;
        bool haveBeenEdited = false;

        DateTime ps3Time = new DateTime(2008,1,1);
        DateTime randomEndTime = DateTime.Now;
        bool isOpen = false;

        public Form1() {
            CultureInfo curinfo = null;
            switch (Properties.Settings.Default.Language) { 
                case 0:
                    curinfo = new CultureInfo("zh-TW");
                    break;
                default:
                    curinfo = CultureInfo.CreateSpecificCulture("en");
                    break;
            }

            Thread.CurrentThread.CurrentCulture = curinfo;
            Thread.CurrentThread.CurrentUICulture = curinfo;
            InitializeComponent();
            toolStripComboBox1.SelectedIndexChanged -= toolStripComboBox1_SelectedIndexChanged;
            toolStripComboBox1.SelectedIndex = Properties.Settings.Default.Language;
            toolStripComboBox1.SelectedIndexChanged += toolStripComboBox1_SelectedIndexChanged;
            dtpForm = new DateTimePickForm();
            dtpfForInstant = new DateTimePickForm();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            System.Diagnostics.Process.Start(linkLabel1.Text.ToString());
        }

        private void 關閉ToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void 開啟ToolStripMenuItem_Click(object sender, EventArgs e) {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
                OpenFile(folderBrowserDialog1.SelectedPath);
            }
        }

        private void RefreashCompoment() {
            EmptyAllCompoment();
            listViewEx1.BeginUpdate();
            for (int i = 0; i < tconf.Count; i++) {
                listViewEx1.LargeImageList.Images.Add("", Image.FromFile(path + @"\TROP" + string.Format("{0:000}", tconf[i].id) + ".PNG"));
                ListViewItem lvi = new ListViewItem();
                lvi.ImageIndex = i; // 在這裡imageid其實等於trophy ID   ex 白金0號, 1...
                lvi.Text = tconf[i].name;
                lvi.SubItems.Add(tconf[i].detail);
                lvi.SubItems.Add(tconf[i].ttype);
                lvi.SubItems.Add(tconf[i].hidden);
                lvi.SubItems.Add((tpsn[i].HasValue) ? Properties.strings.yes : Properties.strings.no);
                lvi.SubItems.Add((tpsn[i].HasValue && tpsn[i].Value.IsSync) ? Properties.strings.yes : Properties.strings.no);
                if (tpsn[i].HasValue) {
                    lvi.SubItems.Add(tpsn[i].Value.Time.ToString("yyyy/M/dd  HH:mm:ss"));
                    if (tpsn[i].Value.IsSync) lvi.BackColor = Color.LightPink;
                } else {
                    lvi.SubItems.Add(tusr.trophyTimeInfoTable[i].Time.ToString("yyyy/M/dd  HH:mm:ss"));
                    lvi.BackColor = Color.LightGray;
                }

                listViewEx1.Items.Add(lvi);
            }
            listViewEx1.EndUpdate();
            CompletionRates();
        }

        private void EmptyAllCompoment() {
            listViewEx1.Items.Clear();
            listViewEx1.LargeImageList.Images.Clear();
            listViewEx1.LargeImageList.ImageSize = new Size(50, 50);
            this.Text = Application.ProductName;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            label2.Text = "00/00";
            label4.Text = "000/000";
        }

        private void CompletionRates() {
            int totalGrade = 0, getGrade = 0, isGetTrophyNumber = 0;
            for (int i = 0; i < tconf.Count; i++) {
                switch (tusr.trophyTypeTable[i].Type) {
                    case 1:
                        totalGrade += 180;
                        getGrade += (tpsn[i].HasValue) ? 180 : 0;
                        break;
                    case 2:
                        totalGrade += 90;
                        getGrade += (tpsn[i].HasValue) ? 90 : 0;
                        break;
                    case 3:
                        totalGrade += 30;
                        getGrade += (tpsn[i].HasValue) ? 30 : 0;
                        break;
                    case 4:
                        totalGrade += 15;
                        getGrade += (tpsn[i].HasValue) ? 15 : 0;
                        break;
                }

                if (tpsn[i].HasValue) isGetTrophyNumber++;
            }
            progressBar1.Maximum = totalGrade;
            progressBar1.Value = getGrade;
            label2.Text = isGetTrophyNumber + "/" + tconf.Count;
            label4.Text = getGrade + "/" + totalGrade;
            this.Text = Application.ProductName + "-[" + tconf.title_name + "]";
        }

        private void listViewEx1_SubItemClicked(object sender, ListViewEx.SubItemEventArgs e) {
            int trophyID = e.Item.ImageIndex;// 在這裡imageid其實等於trophy ID   ex 白金0號, 1...
            if (e.SubItem == 6 && tpsn[trophyID].HasValue && !tpsn[trophyID].Value.IsSync) { // 已經取得且尚未同步的才可編輯
                listViewEx1.StartEditing(dateTimePicker1, e.Item, e.SubItem);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
                if (Directory.Exists(files[0])) {
                    e.Effect = DragDropEffects.All;
                }
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e) {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            OpenFile(files[0]);
        }

        private void 重新整理ToolStripMenuItem_Click(object sender, EventArgs e) {
            RefreashCompoment();
        }

        private void listViewEx1_SubItemEndEditing(object sender, ListViewEx.SubItemEndEditingEventArgs e) {
            try {
                tpsn.ChangeTime(e.Item.ImageIndex, Convert.ToDateTime(e.DisplayText));
                TROPUSR.TrophyTimeInfo tti = tusr.trophyTimeInfoTable[e.Item.ImageIndex];
                tti.Time = Convert.ToDateTime(e.DisplayText);
                tusr.trophyTimeInfoTable[e.Item.ImageIndex] = tti;
                haveBeenEdited = true;
            } catch (Exception ex) {
                e.Cancel = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void listViewEx1_DoubleClick(object sender, EventArgs e) {
            int trophyID = ((ListView)sender).SelectedItems[0].ImageIndex;// 在這裡imageid其實等於trophy ID   ex 白金0號, 1...
            ListViewItem lvi = ((ListView)sender).SelectedItems[0];
            if (tpsn[trophyID].HasValue && tpsn[trophyID].Value.IsSync) { // 尚未同步的才可編輯
                MessageBox.Show(Properties.strings.SyncedTrophyCanNotEdit);
            } else if (tpsn[trophyID].HasValue) { // 已經取得的獎杯，刪除之
                if (trophyID != 0 && (tpsn.Count == tusr.all_trophy_number )) {
                    MessageBox.Show(Properties.strings.CantLoclPlatinumBeforOther);
                } else if (MessageBox.Show(Properties.strings.DeleteTrophyConfirm, Properties.strings.Delete, MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    tpsn.DeleteTrophyByID(trophyID);
                    tusr.LockTrophy(trophyID);
                    lvi.SubItems[4].Text = Properties.strings.no;
                    lvi.BackColor = Color.LightGray;
                    lvi.SubItems[6].Text = new DateTime(0).ToString(dtpForm.dateTimePicker1.CustomFormat);
                    tusr.LockTrophy(trophyID);
                    CompletionRates();
                    haveBeenEdited = true;
                }
            } else {  // nonget
                if (trophyID == 0 && ( tpsn.Count != tusr.all_trophy_number-1 ) ) {
                    MessageBox.Show(Properties.strings.CantUnloclPlatinumBeforOther);
                } else if (dtpForm.ShowDialog(this) == DialogResult.OK) {
                    tpsn.PutTrophy(trophyID, tusr.trophyTypeTable[trophyID].Type, dtpForm.dateTimePicker1.Value);
                    tusr.UnlockTrophy(trophyID, dtpForm.dateTimePicker1.Value);
                    lvi.SubItems[4].Text = Properties.strings.yes;
                    lvi.BackColor = ((ListView)sender).BackColor;
                    lvi.SubItems[6].Text = dtpForm.dateTimePicker1.Value.ToString(dtpForm.dateTimePicker1.CustomFormat);
                    tusr.UnlockTrophy(trophyID, dtpForm.dateTimePicker1.Value);
                    CompletionRates();
                    haveBeenEdited = true;
                }
            }
        }

        private void 存檔ToolStripMenuItem_Click(object sender, EventArgs e) {
            if (isOpen) {
                tpsn.Save();
                tusr.Save();
                haveBeenEdited = false;
            }
        }

        private void 關閉檔案CToolStripMenuItem_Click(object sender, EventArgs e) {
            CloseFile();
        }

        private void OpenFile(string path_in) {
            try {
                if (isOpen) {
                    CloseFile();
                }
                path = path_in;
                Utility.decryptTrophy(path);
                tconf = new TROPCONF(path);
                tpsn = new TROPTRNS(path);
                tusr = new TROPUSR(path);
                //tpsn.PrintState();
                // tusr.PrintState();
                RefreashCompoment();
                isOpen = true;
                重新整理ToolStripMenuItem.Enabled = true;
                進階ToolStripMenuItem.Enabled = true;
            } catch (Exception ex) {
                tconf = null;
                tpsn = null;
                tusr = null;
                GC.Collect();
                Utility.encryptTrophy(path);
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show("Open Failed:" + ex.Message);
            }
        }

        public bool CloseFile() {
            if (haveBeenEdited) {
                DialogResult dr = MessageBox.Show(Properties.strings.CloseConfirm, Properties.strings.Close, MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Yes) {
                    // tpsn.PrintState();
                    // tusr.PrintState();
                    tpsn.Save();
                    tusr.Save();
                } else if (dr == DialogResult.No) {
                } else {
                    return false;
                }
            }

            tpsn = null;
            tusr = null;
            tconf = null;
            EmptyAllCompoment();
            haveBeenEdited = false;
            重新整理ToolStripMenuItem.Enabled = false;
            進階ToolStripMenuItem.Enabled = false;
            if (isOpen) {
                Utility.encryptTrophy(path);
                isOpen = false;
            }
            return true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (isOpen) {
                e.Cancel = !CloseFile();
            }
        }

        private void 瞬間白金ToolStripMenuItem_Click(object sender, EventArgs e) {
                Random rand = new Random((int)DateTime.Now.Ticks);
                for (int i = 1; i < tusr.trophyTimeInfoTable.Count; i++) {
                    if (!tpsn[i].HasValue) {
                        tusr.UnlockTrophy(i, new DateTime(Utility.LongRandom(ps3Time.Ticks, randomEndTime.Ticks, rand)));
                        tpsn.PutTrophy(i, tusr.trophyTypeTable[i].Type, new DateTime(Utility.LongRandom(ps3Time.Ticks, randomEndTime.Ticks, rand)));
                    }
                }
                if (!tpsn[0].HasValue) {
                    tusr.UnlockTrophy(0, tpsn.GetLastTrophyTime().AddSeconds(1));
                    tpsn.PutTrophy(0, tusr.trophyTypeTable[0].Type, tpsn.GetLastTrophyTime().AddSeconds(1));
                }
                haveBeenEdited = true;
                RefreashCompoment();
        }

        private void 清除獎杯ToolStripMenuItem_Click(object sender, EventArgs e) {
            TROPTRNS.TrophyInfo? ti = tpsn.PopTrophy();
            while (ti.HasValue) {
                tusr.LockTrophy(ti.Value.TrophyID);
                ti = tpsn.PopTrophy();
            }
            haveBeenEdited = true;
            RefreashCompoment();
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            Properties.Settings.Default.Language = toolStripComboBox1.SelectedIndex;
            Properties.Settings.Default.Save();
            MessageBox.Show(Properties.strings.RestartProgram);
        }

        private void setRandomStartTimeToolStripMenuItem_Click(object sender, EventArgs e) {
            dtpfForInstant.Title.Text = Properties.strings.RandomStartTime;
            if (dtpfForInstant.ShowDialog() == DialogResult.OK) {
                ps3Time = dtpfForInstant.dateTimePicker1.Value;
            }
        }

        private void setRandomEndTimeToolStripMenuItem_Click(object sender, EventArgs e) {
            dtpfForInstant.Title.Text = Properties.strings.RandomEndTime;
            if (dtpfForInstant.ShowDialog() == DialogResult.OK) {
                randomEndTime = dtpfForInstant.dateTimePicker1.Value;
            }
        }
    }
}
