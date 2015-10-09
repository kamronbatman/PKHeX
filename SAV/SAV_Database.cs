﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PKHeX.Properties;

namespace PKHeX
{
    public partial class SAV_Database : Form
    {
        public SAV_Database(Main f1)
        {
            m_parent = f1;
            InitializeComponent();
            PKXBOXES = new[]
            {
                bpkx1, bpkx2, bpkx3, bpkx4, bpkx5, bpkx6,
                bpkx7, bpkx8, bpkx9, bpkx10,bpkx11,bpkx12,
                bpkx13,bpkx14,bpkx15,bpkx16,bpkx17,bpkx18,
                bpkx19,bpkx20,bpkx21,bpkx22,bpkx23,bpkx24,
                bpkx25,bpkx26,bpkx27,bpkx28,bpkx29,bpkx30,

                bpkx31,bpkx32,bpkx33,bpkx34,bpkx35,bpkx36,
                bpkx37,bpkx38,bpkx39,bpkx40,bpkx41,bpkx42,
                bpkx43,bpkx44,bpkx45,bpkx46,bpkx47,bpkx48,
                bpkx49,bpkx50,bpkx51,bpkx52,bpkx53,bpkx54,
                bpkx55,bpkx56,bpkx57,bpkx58,bpkx59,bpkx60,
                bpkx61,bpkx62,bpkx63,bpkx64,bpkx65,bpkx66,
            };

            // Enable Scrolling when hovered over
            var me = this;
            PAN_Box.MouseEnter += (sender, args) =>
            {
                if (ActiveForm == me)
                    SCR_Box.Focus();
            };
            foreach (var slot in PKXBOXES)
            {
                slot.MouseEnter += (sender, args) =>
                {
                    if (ActiveForm == me)
                        SCR_Box.Focus();
                };
                // Enable Click
                slot.MouseClick += (sender, args) =>
                {
                    if (ModifierKeys == Keys.Control)
                        clickView(sender, args);
                };
            }
            
            Counter = L_Count.Text;
            populateComboBoxes();

            ContextMenuStrip mnu = new ContextMenuStrip();
            ToolStripMenuItem mnuView = new ToolStripMenuItem("View");

            // Assign event handlers
            mnuView.Click += clickView;

            // Add to main context menu
            mnu.Items.AddRange(new ToolStripItem[] { mnuView });

            // Assign to datagridview
            foreach (PictureBox p in PKXBOXES)
                p.ContextMenuStrip = mnu;

            // Load initial database
            Database.Add(new DatabaseList
            {
                Version = 0,
                Title = "Misc",
                Description = "Individual pk6 files present in the db/sav.",
            });

            // Load databases
            foreach (string file in Directory.GetFiles(DatabasePath))
            {
                if ((new FileInfo(file)).Extension == ".pk6")
                    Database[0].Slot.Add(new PK6(File.ReadAllBytes(file), file));
                else
                    loadDatabase(File.ReadAllBytes(file));
            }
            for (int i = 0; i < 930; i++)
            {
                PK6 pk6 = new PK6(PKX.decryptArray(Main.savefile.Skip(Main.SaveGame.Box + 0xE8*i).Take(0xE8).ToArray()), "Boxes");
                if (pk6.Species != 0)
                    Database[0].Slot.Add(pk6);
            }

            // Prepare Database
            prepareDBForSearch();
        }
        private Main m_parent;
        private const string DatabasePath = "db";
        private List<DatabaseList> Database = new List<DatabaseList>();
        private List<PK6> Results;
        private List<PK6> RawDB;
        private string Counter;

        private int slotSelected = -1; // = null;
        private void clickView(object sender, EventArgs e)
        {
            string name = (sender is ToolStripItem)
                ? ((sender as ToolStripItem).Owner as ContextMenuStrip).SourceControl.Name
                : (sender as PictureBox).Name;

            int index = Array.FindIndex(PKXBOXES, p => p.Name == name);
            var dataArr = Results.Skip(SCR_Box.Value * RES_MIN).Take(RES_MAX).ToArray();
            if (index >= dataArr.Length)
                System.Media.SystemSounds.Exclamation.Play();
            else
            {
                m_parent.populateFields(dataArr[index].Data);
                slotSelected = index + SCR_Box.Value * RES_MIN;
                FillPKXBoxes(SCR_Box.Value);
            }
        }
        private void populateComboBoxes()
        {
            // Set the Text
            CB_HeldItem.DisplayMember =
            CB_Species.DisplayMember =
            CB_Ability.DisplayMember =
            CB_Nature.DisplayMember =
            CB_GameOrigin.DisplayMember =
            CB_HPType.DisplayMember = "Text";

            // Set the Value
            CB_HeldItem.ValueMember =
            CB_Species.ValueMember =
            CB_Ability.ValueMember =
            CB_Nature.ValueMember =
            CB_GameOrigin.ValueMember =
            CB_HPType.ValueMember = "Value";

            var Any = new Util.cbItem {Text = "Any", Value = -1};

            var DS_Species = new List<Util.cbItem>(Main.SpeciesDataSource);
            DS_Species.RemoveAt(0); DS_Species.Insert(0, Any); CB_Species.DataSource = DS_Species;

            var DS_Item = new List<Util.cbItem>(Main.ItemDataSource);
            DS_Item.Insert(0, Any); CB_HeldItem.DataSource = DS_Item;

            var DS_Nature = new List<Util.cbItem>(Main.NatureDataSource);
            DS_Nature.Insert(0, Any); CB_Nature.DataSource = DS_Nature;

            var DS_Ability = new List<Util.cbItem>(Main.AbilityDataSource);
            DS_Ability.Insert(0, Any); CB_Ability.DataSource = DS_Ability;

            var DS_Version = new List<Util.cbItem>(Main.VersionDataSource);
            DS_Version.Insert(0, Any); CB_GameOrigin.DataSource = DS_Version;
            
            string[] hptypes = new string[Main.types.Length - 2]; Array.Copy(Main.types, 1, hptypes, 0, hptypes.Length);
            var DS_Type = Util.getCBList(hptypes, null); 
            DS_Type.Insert(0, Any); CB_HPType.DataSource = DS_Type;

            // Set the Move ComboBoxes too..
            var DS_Move = new List<Util.cbItem>(Main.MoveDataSource);
            DS_Move.RemoveAt(0); DS_Move.Insert(0, Any);
            {
                foreach (ComboBox cb in new[] { CB_Move1, CB_Move2, CB_Move3, CB_Move4 })
                {
                    cb.DisplayMember = "Text"; cb.ValueMember = "Value";
                    cb.DataSource = new BindingSource(DS_Move, null);
                }
            }

            // Trigger a Reset
            resetFilters(null, null);
        }
        private void resetFilters(object sender, EventArgs e)
        {
            CHK_Shiny.Checked = CHK_IsEgg.Checked = true;
            CHK_Shiny.CheckState = CHK_IsEgg.CheckState = CheckState.Indeterminate;
            MT_ESV.Text = "";
            CB_HeldItem.SelectedIndex = 0;
            CB_Species.SelectedIndex = 0;
            CB_Ability.SelectedIndex = 0;
            CB_Nature.SelectedIndex = 0;
            CB_HPType.SelectedIndex = 0;

            CB_Level.SelectedIndex = 0;
            TB_Level.Text = "";
            CB_EVTrain.SelectedIndex = 0;
            CB_IV.SelectedIndex = 0;

            CB_Move1.SelectedIndex = CB_Move2.SelectedIndex = CB_Move3.SelectedIndex = CB_Move4.SelectedIndex = 0;

            CB_GameOrigin.SelectedIndex = 0;
        }

        private void loadDatabase(byte[] data)
        {
            var db = new DatabaseList(data);
            if (db.Slot.Count > 0)
                Database.Add(db);
        }
        private class DatabaseList
        {
            public List<PK6> Slot = new List<PK6>();
            public int Version;
            private bool Unicode;
            public byte[] Unused;
            public string Title;
            public string Description;

            public DatabaseList() { }
            public DatabaseList(byte[] data)
            {
                if ((data.Length < 0x100 + 0xE8) || (data.Length - 0x100)%0xE8 != 0)
                    return;

                Version = BitConverter.ToInt32(data, 0);
                Unicode = data[0x5] == 1;
                Unused = data.Skip(4).Take(0xB).ToArray();

                if (Unicode)
                {
                    Title = Encoding.Unicode.GetString(data, 0x10, 0x30).Trim();
                    Description = Encoding.Unicode.GetString(data, 0x40, 0x60).Trim();
                }
                else
                {
                    Title = Encoding.ASCII.GetString(data, 0x10, 0x30).Trim();
                    Description = Encoding.ASCII.GetString(data, 0x40, 0x60).Trim();
                }

                int count = (data.Length - 0x100)/0xE8;
                for (int i = 0; i < count; i++)
                    Slot.Add(new PK6(data.Skip(0x100 + i * 0xE8).Take(0xE8).ToArray()));
            }
            public byte[] Write()
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(Version);
                    bw.Write(Unused);

                    byte[] title = Unicode ? Encoding.Unicode.GetBytes(Title) : Encoding.ASCII.GetBytes(Title);
                    Array.Resize(ref title, 0x30);
                    bw.Write(title);
                    
                    byte[] desc = Unicode ? Encoding.Unicode.GetBytes(Description) : Encoding.ASCII.GetBytes(Description);
                    Array.Resize(ref title, 0x60);
                    bw.Write(desc);

                    foreach (var pk6 in Slot)
                        bw.Write(pk6.Data.Take(0xE8).ToArray());

                    return ms.ToArray();
                }
            }
        }
        private void openDB(object sender, EventArgs e)
        {
            if (Directory.Exists(DatabasePath))
                Process.Start("explorer.exe", @DatabasePath);
        }

        private void B_Search_Click(object sender, EventArgs e)
        {
            // Util.Alert("I don't do everything!", "For now...");

            // Populate Search Query Result
            IEnumerable<PK6> res = RawDB;

            // Primary Searchables
            int species = Util.getIndex(CB_Species);
            int ability = Util.getIndex(CB_Ability);
            int nature = Util.getIndex(CB_Nature);
            int item = Util.getIndex(CB_HeldItem);
            if (species != -1) res = res.Where(pk => pk.Species == species);
            if (ability != -1) res = res.Where(pk => pk.Ability == ability);
            if (nature != -1) res = res.Where(pk => pk.Nature == nature);
            if (item != -1) res = res.Where(pk => pk.HeldItem == item);

            // Secondary Searchables
            int move1 = Util.getIndex(CB_Move1);
            int move2 = Util.getIndex(CB_Move2);
            int move3 = Util.getIndex(CB_Move3);
            int move4 = Util.getIndex(CB_Move4);
            if (move1 != -1) res = res.Where(pk => pk.Moves.Contains(move1));
            if (move2 != -1) res = res.Where(pk => pk.Moves.Contains(move2));
            if (move3 != -1) res = res.Where(pk => pk.Moves.Contains(move3));
            if (move4 != -1) res = res.Where(pk => pk.Moves.Contains(move4));
            int vers = Util.getIndex(CB_GameOrigin);
            if (vers != -1) res = res.Where(pk => pk.Version == vers);
            int hptype = Util.getIndex(CB_HPType);
            if (hptype != -1) res = res.Where(pk => pk.HPType == hptype);
            if (CHK_Shiny.CheckState == CheckState.Checked) res = res.Where(pk => pk.IsShiny);
            if (CHK_Shiny.CheckState == CheckState.Unchecked) res = res.Where(pk => !pk.IsShiny);
            if (CHK_IsEgg.CheckState == CheckState.Checked) res = res.Where(pk => pk.IsEgg);
            if (CHK_IsEgg.CheckState == CheckState.Unchecked) res = res.Where(pk => !pk.IsEgg);
            if (CHK_IsEgg.CheckState == CheckState.Checked && MT_ESV.Text != "")
                res = res.Where(pk => pk.PSV == Convert.ToInt16(MT_ESV.Text));

            // Tertiary Searchables
            if (TB_Level.Text != "") // Level
            {
                int level = Convert.ToInt16(TB_Level.Text);
                if (level <= 100)
                switch (CB_Level.SelectedIndex)
                {
                    case 0: break; // Any 
                    case 1: // <=
                        res = res.Where(pk => pk.Stat_Level <= level);
                        break;
                    case 2: // == 
                        res = res.Where(pk => pk.Stat_Level == level);
                        break;
                    case 3: // >=
                        res = res.Where(pk => pk.Stat_Level >= level);
                        break;
                }
            }
            switch (CB_IV.SelectedIndex)
            {
                case 0: break; // Do nothing
                case 1: // <= 90
                    res = res.Where(pk => pk.IVs.Sum() <= 90);
                    break;
                case 2: // 91-120
                    res = res.Where(pk => pk.IVs.Sum() > 90 && pk.IVs.Sum() <= 120);
                    break;
                case 3: // 121-150
                    res = res.Where(pk => pk.IVs.Sum() > 120 && pk.IVs.Sum() <= 150);
                    break;
                case 4: // 151-179
                    res = res.Where(pk => pk.IVs.Sum() > 150 && pk.IVs.Sum() < 180);
                    break;
                case 5: // 180+
                    res = res.Where(pk => pk.IVs.Sum() >= 180);
                    break;
                case 6: // == 186
                    res = res.Where(pk => pk.IVs.Sum() == 186);
                    break;
            }
            switch (CB_EVTrain.SelectedIndex)
            {
                case 0: break; // Do nothing
                case 1: // None (0)
                    res = res.Where(pk => pk.EVs.Sum() == 0);
                    break;
                case 2: // Some (127-0)
                    res = res.Where(pk => pk.EVs.Sum() < 128);
                    break;
                case 3: // Half (128-507)
                    res = res.Where(pk => pk.EVs.Sum() >= 128 && pk.EVs.Sum() < 508);
                    break;
                case 4: // Full (508+)
                    res = res.Where(pk => pk.EVs.Sum() >= 508);
                    break;
            }

            var results = res.ToArray();
            if (results.Length == 0)
                Util.Alert("No results found!");

            setResults(new List<PK6>(results)); // updates Count Label as well.
            System.Media.SystemSounds.Asterisk.Play();
        }
        private void updateScroll(object sender, ScrollEventArgs e)
        {
            if (e.OldValue != e.NewValue)
                FillPKXBoxes(e.NewValue);
        }

        private const int RES_MAX = 66;
        private const int RES_MIN = 6;
        private void FillPKXBoxes(int start)
        {
            if (Results == null)
                for (int i = 0; i < RES_MAX; i++)
                    PKXBOXES[i].Image = null;
            PK6[] data = (Results).Skip(start * RES_MIN).Take(RES_MAX).ToArray();
            for (int i = 0; i < data.Length; i++)
                PKXBOXES[i].Image = data[i].Sprite;
            for (int i = data.Length; i < RES_MAX; i++)
                PKXBOXES[i].Image = null;

            for (int i = 0; i < RES_MAX; i++)
                PKXBOXES[i].BackgroundImage = Resources.slotTrans;
            if (slotSelected != -1 && slotSelected >= RES_MIN * start && slotSelected < RES_MIN * start + RES_MAX)
                PKXBOXES[slotSelected - start * RES_MIN].BackgroundImage = Resources.slotView;
        }
        private PictureBox[] PKXBOXES;

        // Misc Update Methods
        private void toggleESV(object sender, EventArgs e)
        {
            L_ESV.Visible = MT_ESV.Visible = CHK_IsEgg.CheckState == CheckState.Checked;
        }
        private void changeLevel(object sender, EventArgs e)
        {
            if (CB_Level.SelectedIndex == 0)
                TB_Level.Text = "";
        }

        // View Updates
        private void prepareDBForSearch()
        {
            RawDB = new List<PK6>();

            foreach (var db in Database)
                RawDB.AddRange(db.Slot);

            RawDB = new List<PK6>(RawDB.Where(pk => pk.Checksum == pk.CalculateChecksum()));
            RawDB = new List<PK6>(RawDB.Distinct());
            setResults(RawDB);
        }
        private void setResults(List<PK6> res)
        {
            Results = new List<PK6>(res);

            SCR_Box.Maximum = (int)Math.Ceiling((decimal)Results.Count / RES_MIN);
            if (SCR_Box.Maximum > 0) SCR_Box.Maximum -= 1;

            if (SCR_Box.Value != 0)
                SCR_Box.Value = 0;
            else 
                FillPKXBoxes(0);

            L_Count.Text = String.Format(Counter, Results.Count);
        }

        // Debug
        private void testQuery()
        {
            var query = from db in Database
                        select db.Slot.Where(p => p.Move1 == 1).ToArray();

            var result = query.ToArray();
            if (!result[0].Any())
                return;

            var any = result[0][0].Data;
            m_parent.populateFields(any);
        }
        private void testUnique()
        {
            var query = from db in Database
                        select db.Slot.GroupBy(p => (p.Checksum + p.EncryptionConstant + p.Species)) // Unique criteria
                        .Select(grp => grp.First()).ToArray();

            var result = query.ToArray();
            if (!result[0].Any())
                return;

            var any = result[0][0].Data;
            m_parent.populateFields(any);
        }
    }
}
