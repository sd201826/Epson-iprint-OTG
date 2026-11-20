using System.Management;
using System.Text.Json;

namespace EpsonL3210Monitor;

public sealed class MainForm : Form
{
    private readonly ComboBox printers = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
    private readonly Label connection = new() { AutoSize = true, ForeColor = Color.DarkSlateGray };
    private readonly NumericUpDown[] levels = new NumericUpDown[4];
    private readonly TextBox raw = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Width = 520, Height = 76 };
    private readonly Label wasteStatus = new() { AutoSize = true, MaximumSize = new Size(520, 0) };
    private readonly string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpsonL3210Monitor", "levels.json");
    private readonly string[] colors = ["Black", "Cyan", "Magenta", "Yellow"];

    public MainForm()
    {
        Text = "Epson L3210 Monitor";
        MinimumSize = new Size(700, 650);
        Size = new Size(760, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 248, 252);
        BuildUi();
        LoadSaved();
        RefreshPrinters();
    }

    private void BuildUi()
    {
        var root = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(22) };
        Controls.Add(root);
        root.Controls.Add(new Label { Text = "EPSON L3210 MONITOR", Font = new Font("Segoe UI", 19, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(20, 48, 75), Margin = new Padding(0, 0, 0, 4) });
        root.Controls.Add(new Label { Text = "Windows desktop · USB printer status and ink tracking", AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(0, 0, 0, 18) });

        var printerBox = Group("Printer connection");
        var pRow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Dock = DockStyle.Top };
        pRow.Controls.Add(printers);
        var refresh = new Button { Text = "Refresh printers", AutoSize = true };
        refresh.Click += (_, _) => RefreshPrinters();
        pRow.Controls.Add(refresh);
        printerBox.Controls.Add(pRow);
        printerBox.Controls.Add(connection);
        root.Controls.Add(printerBox);

        var inkBox = Group("Ink tank levels");
        inkBox.Controls.Add(new Label { Text = "The L3210 uses visible EcoTank reservoirs. Windows does not expose reliable electronic percentages for these four tanks. Set approximate levels after visually checking each tank.", AutoSize = true, MaximumSize = new Size(650, 0), ForeColor = Color.DimGray });
        for (int i = 0; i < colors.Length; i++)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
            row.Controls.Add(new Label { Text = colors[i], Width = 105, Padding = new Padding(0, 7, 0, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            var n = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 0, Width = 90, Suffix = "" };
            levels[i] = n;
            row.Controls.Add(n);
            row.Controls.Add(new Label { Text = "% visual estimate", AutoSize = true, Padding = new Padding(4, 7, 0, 0), ForeColor = Color.DimGray });
            inkBox.Controls.Add(row);
        }
        var save = new Button { Text = "Save ink estimates", AutoSize = true, BackColor = Color.FromArgb(25, 103, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 14, 0, 0) };
        save.Click += (_, _) => SaveLevels();
        inkBox.Controls.Add(save);
        root.Controls.Add(inkBox);

        var wasteBox = Group("Waste-ink pad counter");
        wasteBox.Controls.Add(new Label { Text = "Waste-ink percentage: NOT AVAILABLE", AutoSize = true, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.DarkOrange });
        wasteBox.Controls.Add(new Label { Text = "Your InkZero screenshot showed raw hexadecimal register values and stated that no percentage formula is available. This app will not guess a percentage or reset the counter.", AutoSize = true, MaximumSize = new Size(650, 0), ForeColor = Color.DimGray });
        wasteBox.Controls.Add(new Label { Text = "Optional raw register readings (paste only; no conversion):", AutoSize = true, Margin = new Padding(0, 10, 0, 4) });
        wasteBox.Controls.Add(raw);
        wasteStatus.Text = "No waste-ink percentage has been calculated.";
        wasteBox.Controls.Add(wasteStatus);
        root.Controls.Add(wasteBox);

        root.Controls.Add(new Label { Text = "Safety: This app only lists Windows printer entries and saves your manual estimates. It does not send print commands, modify firmware, reset counters, or require administrator access.", AutoSize = true, MaximumSize = new Size(650, 0), ForeColor = Color.DimGray, Margin = new Padding(0, 12, 0, 0) });
    }

    private static GroupBox Group(string title)
    {
        var g = new GroupBox { Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Width = 670, Padding = new Padding(14), Margin = new Padding(0, 0, 0, 14) };
        g.Controls.Add(new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true });
        // Use a table-like flow panel as the direct child container.
        return g;
    }

    private void RefreshPrinters()
    {
        printers.Items.Clear();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, PortName, WorkOffline FROM Win32_Printer");
            foreach (ManagementObject item in searcher.Get())
            {
                var name = item["Name"]?.ToString() ?? "Unknown printer";
                var port = item["PortName"]?.ToString() ?? "Unknown port";
                printers.Items.Add($"{name}  [{port}]");
            }
            if (printers.Items.Count > 0) printers.SelectedIndex = 0;
            connection.Text = printers.Items.Count == 0 ? "No Windows printer entries found." : $"Found {printers.Items.Count} Windows printer(s). This confirms Windows registration, not ink-counter access.";
        }
        catch (Exception ex)
        {
            connection.Text = "Could not query Windows printer list: " + ex.Message;
        }
    }

    private void LoadSaved()
    {
        try
        {
            if (!File.Exists(dataPath)) return;
            var values = JsonSerializer.Deserialize<int[]>(File.ReadAllText(dataPath));
            if (values?.Length == 4)
                for (int i = 0; i < 4; i++) levels[i].Value = Math.Clamp(values[i], 0, 100);
        }
        catch { /* Keep safe defaults if the local file is damaged. */ }
    }

    private void SaveLevels()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);
            File.WriteAllText(dataPath, JsonSerializer.Serialize(levels.Select(x => (int)x.Value).ToArray()));
            MessageBox.Show("Ink estimates saved on this PC.", "Epson L3210 Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not save estimates: " + ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}