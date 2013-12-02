﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using RocksmithToolkitLib;
using RocksmithToolkitLib.Extensions;
using RocksmithToolkitLib.ToolkitTone;
using RocksmithToolkitLib.DLCPackage.Tone;
using RocksmithToolkitLib.DLCPackage.Manifest.Tone;

namespace RocksmithToolkitGUI.DLCPackageCreator
{
    public partial class ToneControl : UserControl
    {
        private bool _refreshingCombos = false;
        public GameVersion CurrentGameVersion;

        private dynamic tone;
        public dynamic Tone
        {
            set
            {
                tone = value;
                if (value != null)
                    RefreshControls();
            }
            get
            {
                return tone;
            }
        }

        private string loopOrRackSlot;
        private string LoopOrRackSlot {
            get {
                loopOrRackSlot = "LoopPedal";
                if (CurrentGameVersion == GameVersion.RS2014)
                    loopOrRackSlot = "Rack";
                return loopOrRackSlot;
            }
        }

        public ToneControl() { }

        public ToneControl(GameVersion gameVersion)
        {
            CurrentGameVersion = gameVersion;
            InitializeComponent();
            InitializeToneInformation();
            InitializeComboBoxes();

            descriptorLabel.Enabled = CurrentGameVersion == GameVersion.RS2014;
            descriptorCombo.Enabled = CurrentGameVersion == GameVersion.RS2014;
            gbLoopPedalAndRacks.Text = (CurrentGameVersion == GameVersion.RS2012) ? "Loop Pedal" : "Rack";
            gbPostPedal.Text = (CurrentGameVersion == GameVersion.RS2012) ? "Post Pedal" : "Loop Pedal";
            loopPedalRack4Box.Enabled = CurrentGameVersion == GameVersion.RS2014;
            loopPedalRack4KnobButton.Enabled = CurrentGameVersion == GameVersion.RS2014;
            prePedal4Box.Enabled = CurrentGameVersion == GameVersion.RS2014;
            prePedal4KnobButton.Enabled = CurrentGameVersion == GameVersion.RS2014;
            postPedal4Box.Enabled = CurrentGameVersion == GameVersion.RS2014;
            postPedal4KnobButton.Enabled = CurrentGameVersion == GameVersion.RS2014;
        }

        public void RefreshControls()
        {
            _refreshingCombos = true;
            toneNameBox.Text = tone.Name ?? "";
            volumeBox.Value = Decimal.Round((decimal)tone.Volume, 2);

            UpdateComboSelection(ampBox, ampKnobButton, "Amp");
            UpdateComboSelection(cabinetBox, cabinetKnobButton, "Cabinet");

            UpdateComboSelection(prePedal1Box, prePedal1KnobButton, "PrePedal1");
            UpdateComboSelection(prePedal2Box, prePedal2KnobButton, "PrePedal2");
            UpdateComboSelection(prePedal3Box, prePedal3KnobButton, "PrePedal3");
            UpdateComboSelection(prePedal4Box, prePedal4KnobButton, "PrePedal4");

            UpdateComboSelection(loopPedalRack1Box, loopPedalRack1KnobButton, LoopOrRackSlot + "1");
            UpdateComboSelection(loopPedalRack2Box, loopPedalRack2KnobButton, LoopOrRackSlot + "2");
            UpdateComboSelection(loopPedalRack3Box, loopPedalRack3KnobButton, LoopOrRackSlot + "3");
            UpdateComboSelection(loopPedalRack4Box, loopPedalRack4KnobButton, LoopOrRackSlot + "4");

            UpdateComboSelection(postPedal1Box, postPedal1KnobButton, "PostPedal1");
            UpdateComboSelection(postPedal2Box, postPedal2KnobButton, "PostPedal2");
            UpdateComboSelection(postPedal3Box, postPedal3KnobButton, "PostPedal3");
            UpdateComboSelection(postPedal4Box, postPedal4KnobButton, "PostPedal4");
            _refreshingCombos = false;

            if (CurrentGameVersion == GameVersion.RS2014)
            {
                if (tone.ToneDescriptors.Count > 0)
                {
                    if (ToneDescriptor.List().Any<ToneDescriptor>(t => t.Descriptor == tone.ToneDescriptors[0]))
                        descriptorCombo.SelectedIndex = ToneDescriptor.List().TakeWhile(t => t.Descriptor != tone.ToneDescriptors[0]).Count();
                }
                else
                    UpdateToneDescription(descriptorCombo, false);
            }
        }

        private void UpdateComboSelection(ComboBox box, Control knobSelectButton, string pedalSlot)
        {
            switch (CurrentGameVersion) {
                case GameVersion.RS2012:
                    box.SelectedItem = tone.PedalList.ContainsKey(pedalSlot) ? box.Items.OfType<ToolkitPedal>().First(p => p.Key == tone.PedalList[pedalSlot].PedalKey) : null;
                    knobSelectButton.Enabled = tone.PedalList.ContainsKey(pedalSlot) ? ((Pedal)tone.PedalList[pedalSlot]).KnobValues.Any() : false;
                    break;
                case GameVersion.RS2014:
                    box.SelectedItem = tone.GearList[pedalSlot] != null ? box.Items.OfType<ToolkitPedal>().First(p => p.Key == tone.GearList[pedalSlot].PedalKey) : null;
                    knobSelectButton.Enabled = tone.GearList[pedalSlot] != null ? ((Pedal2014)tone.GearList[pedalSlot]).KnobValues.Any() : false;
                    break;
            }
        }

        private void InitializeToneInformation()
        {
            // NAME
            toneNameBox.TextChanged += (sender, e) =>
            {
                var toneName = toneNameBox.Text;
                tone.Key = toneName.GetValidName();
                tone.Name = toneName;
            };

            // VOLUME
            volumeBox.ValueChanged += (sender, e) =>
                tone.Volume = (float)volumeBox.Value;

            // TONE DESCRIPTOR
            if (CurrentGameVersion == GameVersion.RS2014)
            {
                var tonedesclist = ToneDescriptor.List().ToList();
                descriptorCombo.DisplayMember = "Name";
                descriptorCombo.ValueMember = "Descriptor";
                descriptorCombo.DataSource = tonedesclist;

                descriptorCombo.SelectedValueChanged += (sender, e) =>
                    UpdateToneDescription((ComboBox)sender);
            }
        }

        private void Tone_Volume_Tip(object sender, EventArgs f)
        {
            ToolTip tvt = new ToolTip();
            tvt.IsBalloon = true;
            tvt.InitialDelay = 0;
            tvt.ShowAlways = true;
            tvt.SetToolTip(volumeBox, "LOWEST 0 , -1 , -2, -3, ..... AVERAGE -12 .... ,-20, -21 HIGHER,...");
        }

        private void UpdateToneDescription(ComboBox combo, bool updateName = true) {
            if (_refreshingCombos)
                return;

            var descriptor = combo.SelectedItem as ToneDescriptor;
            tone.ToneDescriptors.Clear();
            tone.ToneDescriptors.Add(descriptor.Descriptor);

            if (updateName)
            {
                string toneName = tone.Name;
                var descIndex = toneName.LastIndexOf("_");
                if (descIndex > 0)
                    toneName = toneName.Substring(0, descIndex);

                toneNameBox.Text = String.Format("{0}_{1}", toneName, descriptor.ShortName);
            }
        }

        private void InitializeComboBoxes()
        {
            var allPedals = ToolkitPedal.LoadFromResource(CurrentGameVersion);

            var amps = allPedals
                .Where(p => p.TypeEnum == PedalType.Amp)
                .OrderBy(p => p.DisplayName)
                .ToArray();
            var cabinets = allPedals
                .Where(p => p.TypeEnum == PedalType.Cabinet)
                .OrderBy(p => p.DisplayName)
                .ToArray();
            var loopRackPedals = allPedals
                .Where(p => (CurrentGameVersion == GameVersion.RS2014) ? p.TypeEnum == PedalType.Rack : p.TypeEnum == PedalType.Pedal && p.AllowLoop)
                .OrderBy(p => p.DisplayName)
                .ToArray();
            var prePedals = allPedals
                .Where(p => (CurrentGameVersion == GameVersion.RS2014) ? p.TypeEnum == PedalType.Pedal : p.TypeEnum == PedalType.Pedal && p.AllowPre)
                .OrderBy(p => p.DisplayName)
                .ToArray();
            var postPedals = allPedals
                .Where(p => (CurrentGameVersion == GameVersion.RS2014) ? p.TypeEnum == PedalType.Pedal : p.TypeEnum == PedalType.Pedal && p.AllowPost)
                .OrderBy(p => p.DisplayName)
                .ToArray();

            InitializeSelectedPedal(ampBox, ampKnobButton, "Amp", amps, false);
            InitializeSelectedPedal(cabinetBox, cabinetKnobButton, "Cabinet", cabinets, false);

            InitializeSelectedPedal(loopPedalRack1Box, loopPedalRack1KnobButton, LoopOrRackSlot + "1", loopRackPedals, true);
            InitializeSelectedPedal(loopPedalRack2Box, loopPedalRack2KnobButton, LoopOrRackSlot + "2", loopRackPedals, true);
            InitializeSelectedPedal(loopPedalRack3Box, loopPedalRack3KnobButton, LoopOrRackSlot + "3", loopRackPedals, true);
            InitializeSelectedPedal(loopPedalRack4Box, loopPedalRack4KnobButton, LoopOrRackSlot + "4", loopRackPedals, true);

            InitializeSelectedPedal(prePedal1Box, prePedal1KnobButton, "PrePedal1", prePedals, true);
            InitializeSelectedPedal(prePedal2Box, prePedal2KnobButton, "PrePedal2", prePedals, true);
            InitializeSelectedPedal(prePedal3Box, prePedal3KnobButton, "PrePedal3", prePedals, true);
            InitializeSelectedPedal(prePedal4Box, prePedal4KnobButton, "PrePedal4", prePedals, true);

            InitializeSelectedPedal(postPedal1Box, postPedal1KnobButton, "PostPedal1", postPedals, true);
            InitializeSelectedPedal(postPedal2Box, postPedal2KnobButton, "PostPedal2", postPedals, true);
            InitializeSelectedPedal(postPedal3Box, postPedal3KnobButton, "PostPedal3", postPedals, true);
            InitializeSelectedPedal(postPedal4Box, postPedal4KnobButton, "PostPedal4", postPedals, true);           
        }

        private void InitializeSelectedPedal(ComboBox box, Control knobSelectButton, string pedalSlot, ToolkitPedal[] pedals, bool allowNull)
        {
            knobSelectButton.Enabled = false;
            knobSelectButton.Click += (sender, e) =>
            {
                dynamic pedal = (CurrentGameVersion == GameVersion.RS2012) ? tone.PedalList[pedalSlot] : tone.GearList[pedalSlot];
                using (var form = new ToneKnobForm())
                {
                    form.Init(pedal, pedals.Single(p => p.Key == pedal.PedalKey).Knobs);
                    form.ShowDialog();
                }
            };

            box.Items.Clear();
            box.DisplayMember = "DisplayName";
            if (allowNull)
                box.Items.Add(string.Empty);
            box.Items.AddRange(pedals);
            box.SelectedValueChanged += (sender, e) =>
            {
                if (_refreshingCombos)
                    return;

                var pedal = box.SelectedItem as ToolkitPedal;
                if (pedal == null)
                {
                    switch (CurrentGameVersion) {
                        case GameVersion.RS2012:
                            tone.PedalList.Remove(pedalSlot);
                            break;
                        case GameVersion.RS2014:
                            tone.GearList[pedalSlot] = null;
                            break;
                        default:
                            break;
                    }                    
                    knobSelectButton.Enabled = false;
                }
                else
                {
                    string pedalKey = "";
                    switch (CurrentGameVersion) {
                        case GameVersion.RS2012:
                            if (tone.PedalList.ContainsKey(pedalSlot))
                                pedalKey = tone.PedalList[pedalSlot].PedalKey;
                            break;
                        case GameVersion.RS2014:
                            if (tone.GearList[pedalSlot] != null)
                                pedalKey = tone.GearList[pedalSlot].PedalKey;
                            break;
                    }

                    if (pedal.Key != pedalKey)
                    {
                        var pedalSetting = pedal.MakePedalSetting(CurrentGameVersion);
                        switch (CurrentGameVersion) {
                            case GameVersion.RS2012:
                                tone.PedalList[pedalSlot] = pedalSetting;
                                knobSelectButton.Enabled = ((Pedal)pedalSetting).KnobValues.Any();
                                break;
                            case GameVersion.RS2014:
                                tone.GearList[pedalSlot] = pedalSetting;
                                knobSelectButton.Enabled = ((Pedal2014)pedalSetting).KnobValues.Any();
                                break;
                        }
                    }
                    else {
                        bool knobEnabled = false;
                        switch (CurrentGameVersion) {
                            case GameVersion.RS2012:
                                knobEnabled = ((Pedal)tone.PedalList[pedalSlot]).KnobValues.Any();
                                break;
                            case GameVersion.RS2014:
                                knobEnabled = ((Pedal2014)tone.GearList[pedalSlot]).KnobValues.Any();
                                break;
                        }
                        knobSelectButton.Enabled = knobEnabled;
                    }
                }
            };
        }

        private void toneNameBox_Leave(object sender, EventArgs e) {
            TextBox control = (TextBox)sender;
            control.Text = control.Text.Trim().GetValidName();
        }
    }
}
