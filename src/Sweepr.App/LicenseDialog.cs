using System.Reflection;
using System.IO;

namespace Sweepr.App;

internal sealed class LicenseDialog : Form
{
    private readonly TextBox _text = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9) };
    private readonly Button _accept = new() { Text = "Accept", DialogResult = DialogResult.OK, Width = 90 };
    private readonly Button _decline = new() { Text = "Decline", DialogResult = DialogResult.Cancel, Width = 90 };
    private readonly Label _title = new() { Text = "License Agreement", Dock = DockStyle.Top, Height = 30, Padding = new Padding(8, 8, 8, 0), Font = new Font("Segoe UI", 11, FontStyle.Bold) };

    public LicenseDialog()
    {
        Text = "Sweepr - License Agreement";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(600, 400);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _text.Text = LoadLicenseText();
        _text.Select(0, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _decline, _accept });

        Controls.Add(_text);
        Controls.Add(_title);
        Controls.Add(buttons);
        AcceptButton = _accept;
        CancelButton = _decline;
    }

    private static string LoadLicenseText()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (name.EndsWith("LICENSE.txt", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = asm.GetManifestResourceStream(name);
                    if (stream is not null)
                    {
                        using var reader = new StreamReader(stream);
                        return reader.ReadToEnd();
                    }
                }
            }
        }
        catch { }

        // Fallback if embedded resource is missing.
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "LICENSE");
            if (File.Exists(path)) return File.ReadAllText(path);
        }
        catch { }

        return "GNU General Public License v3.0\n\nCopyright (c) 2026 Juicy-Beer\n\nThis program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.\n\nThis program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.";
    }
}