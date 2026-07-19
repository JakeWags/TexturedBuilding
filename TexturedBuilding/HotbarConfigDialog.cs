using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace TexturedBuilding
{
    public class HotbarConfigDialog : GuiDialog
    {
        private HotbarSlotSettings settings;
        private TexturedBuildingModSystem modSystem;

        public override string ToggleKeyCombinationCode => "texturedbuilding-config";

        public HotbarConfigDialog(ICoreClientAPI capi, TexturedBuildingModSystem modSystem) : base(capi)
        {
            this.modSystem = modSystem;
            this.settings = modSystem.HotbarSettings;
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            ComposeDialog();
        }

        private void ComposeDialog()
        {
            // Define dialog bounds
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            // Create compositor
            SingleComposer = capi.Gui.CreateCompo("texturedbuilding-hotbar-config", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Hotbar Slot Configuration", OnTitleBarClose)
                .BeginChildElements(bgBounds);

            // Add header explaining the feature
            ElementBounds headerBounds = ElementBounds.Fixed(0, 30, 500, 25);
            SingleComposer.AddStaticText("Configure which hotbar slots to include in randomization:", CairoFont.WhiteSmallText(), headerBounds);

            double yPos = 65;

            // Add slot controls (10 slots)
            for (int i = 0; i < 10; i++)
            {
                AddSlotControl(i, ref yPos);
            }

            // Add buttons at bottom
            yPos += 10;
            ElementBounds resetBounds = ElementBounds.Fixed(10, yPos, 100, 30);
            ElementBounds saveBounds = ElementBounds.Fixed(120, yPos, 100, 30);
            ElementBounds closeBounds = ElementBounds.Fixed(230, yPos, 100, 30);

            SingleComposer
                .AddSmallButton("Reset All", OnResetClicked, resetBounds, EnumButtonStyle.Normal, "reset-button")
                .AddSmallButton("Save", OnSaveClicked, saveBounds, EnumButtonStyle.Normal, "save-button")
                .AddSmallButton("Close", OnCloseClicked, closeBounds, EnumButtonStyle.Normal, "close-button");

            SingleComposer.EndChildElements().Compose();
        }

        private void AddSlotControl(int slotIndex, ref double yPos)
        {
            SlotConfig config = settings.Slots[slotIndex];

            // Slot number label
            ElementBounds labelBounds = ElementBounds.Fixed(10, yPos, 60, 25);
            SingleComposer.AddStaticText($"Slot {slotIndex + 1}:", CairoFont.WhiteSmallText(), labelBounds);

            // Enabled checkbox
            ElementBounds checkboxBounds = ElementBounds.Fixed(80, yPos, 25, 25);
            SingleComposer.AddSwitch(OnSlotEnabledToggle, checkboxBounds, $"enabled-{slotIndex}", 25);
            SingleComposer.GetSwitch($"enabled-{slotIndex}").SetValue(config.Enabled);

            // Weight label
            ElementBounds weightLabelBounds = ElementBounds.Fixed(120, yPos, 60, 25);
            SingleComposer.AddStaticText("Weight:", CairoFont.WhiteSmallText(), weightLabelBounds);

            // Weight slider (1-10)
            ElementBounds sliderBounds = ElementBounds.Fixed(190, yPos, 200, 25);
            SingleComposer.AddSlider(OnWeightChanged, sliderBounds, $"weight-{slotIndex}");
            SingleComposer.GetSlider($"weight-{slotIndex}").SetValues(config.Weight, 1, 10, 1);

            // Weight value display
            ElementBounds valueBounds = ElementBounds.Fixed(400, yPos, 40, 25);
            SingleComposer.AddDynamicText($"{config.Weight}", CairoFont.WhiteSmallText(), valueBounds, $"weight-value-{slotIndex}");

            yPos += 35;
        }

        private void OnSlotEnabledToggle(bool on)
        {
            // Find which slot was toggled
            for (int i = 0; i < 10; i++)
            {
                GuiElementSwitch toggle = SingleComposer.GetSwitch($"enabled-{i}");
                if (toggle != null)
                {
                    settings.Slots[i].Enabled = toggle.On;
                }
            }
        }

        private bool OnWeightChanged(int value)
        {
            // Update all slot weights
            for (int i = 0; i < 10; i++)
            {
                GuiElementSlider slider = SingleComposer.GetSlider($"weight-{i}");
                if (slider != null)
                {
                    settings.Slots[i].Weight = slider.GetValue();

                    // Update value display
                    GuiElementDynamicText valueText = SingleComposer.GetDynamicText($"weight-value-{i}");
                    if (valueText != null)
                    {
                        valueText.SetNewText($"{settings.Slots[i].Weight}");
                    }
                }
            }
            return true;
        }

        private bool OnResetClicked()
        {
            // Reset all slots to default
            for (int i = 0; i < 10; i++)
            {
                settings.Slots[i].Enabled = true;
                settings.Slots[i].Weight = 1;

                // Update UI
                SingleComposer.GetSwitch($"enabled-{i}")?.SetValue(true);
                SingleComposer.GetSlider($"weight-{i}")?.SetValues(1, 1, 10, 1);
                SingleComposer.GetDynamicText($"weight-value-{i}")?.SetNewText("1");
            }

            capi.ShowChatMessage("All slots reset to default");
            return true;
        }

        private bool OnSaveClicked()
        {
            // Save settings to file
            modSystem.SaveHotbarSettings();

            capi.ShowChatMessage("Hotbar configuration saved");
            return true;
        }

        private bool OnCloseClicked()
        {
            TryClose();
            return true;
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }
    }
}