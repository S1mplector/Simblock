using System;
using System.Drawing;
using System.Windows.Forms;
using SimBlock.Presentation.Configuration;
using SimBlock.Core.Domain.Enums;
using SimBlock.Presentation.Controls;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Theming
{
    /// <summary>
    /// Applies theme colors and styles across WinForms controls for the settings UI.
    /// </summary>
    public class ThemeApplier : IThemeApplier
    {
        public void Apply(Form form, UISettings settings)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            try
            {
                form.BackColor = settings.BackgroundColor;
                form.ForeColor = settings.TextColor;

                // Walk all controls and apply colors/styles
                ApplyToControls(form.Controls, settings);
            }
            catch (Exception)
            {
                // Let callers handle logging; avoid swallowing state issues here
                throw;
            }
        }

        public string GetThemeButtonText(UISettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return settings.CurrentTheme == Theme.Light ? "🌙 Dark" : "☀️ Light";
        }

        private void ApplyToControls(Control.ControlCollection controls, UISettings settings)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case TableLayoutPanel tablePanel:
                        tablePanel.BackColor = settings.BackgroundColor;
                        break;
                    case FlowLayoutPanel flowPanel:
                        flowPanel.BackColor = settings.BackgroundColor;
                        break;
                    case GroupBox groupBox:
                        groupBox.ForeColor = settings.TextColor;
                        break;
                    case Label label:
                        label.ForeColor = settings.TextColor;
                        break;
                    case CheckBox checkBox:
                        checkBox.ForeColor = settings.TextColor;
                        break;
                    case ComboBox comboBox:
                        comboBox.BackColor = settings.BackgroundColor;
                        comboBox.ForeColor = settings.TextColor;
                        break;
                    case RoundedButton rb:
                        rb.ForeColor = settings.PrimaryButtonTextColor;
                        var baseColor = rb.BackColor;
                        rb.HoverColor = ControlPaint.Light(baseColor, 0.15f);
                        rb.PressedColor = ControlPaint.Dark(baseColor, 0.15f);
                        rb.CornerRadius = 6;
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls, settings);
                }
            }
        }
    }
}
