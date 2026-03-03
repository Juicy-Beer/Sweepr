using Sweepr.Core.Services;
using Microsoft.Win32;

namespace Sweepr.App;

internal sealed class LeftoverTreeViewDialog : Form
{
    private readonly string _programName;
    private readonly IReadOnlyList<Leftover> _leftovers;
    private readonly TreeView _tree = new() { Dock = DockStyle.Fill, CheckBoxes = true };
    private readonly Label _pathLabel = new() { Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 4, 4, 0) };
    private readonly Label _keysLabel = new() { Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 0, 4, 0) };
    private readonly Label _valuesLabel = new() { Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 0, 4, 0) };
    private readonly Button _deselectAll = new() { Text = "Deselect All", Width = 100 };
    private readonly Button _delete = new() { Text = "Delete", Width = 80 };
    private readonly Button _finish = new() { Text = "Finish", Width = 80, DialogResult = DialogResult.OK };
    private readonly Button _cancel = new() { Text = "Cancel", Width = 80, DialogResult = DialogResult.Cancel };

    public LeftoverTreeViewDialog(string programName, IReadOnlyList<Leftover> leftovers)
    {
        _programName = programName;
        _leftovers = leftovers;

        Text = $"Leftovers - {programName}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 520);
        MinimumSize = new Size(600, 400);

        var root = new TreeNode("My Computer");
        _tree.Nodes.Add(root);

        var regRoot = new TreeNode("Registry");
        root.Nodes.Add(regRoot);
        foreach (var lv in leftovers.Where(l => l.Kind == LeftoverKind.RegistryKey))
        {
            var node = regRoot;
            foreach (var seg in lv.Path.Split('\\'))
            {
                var existing = node.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == seg);
                if (existing is null)
                {
                    existing = new TreeNode(seg) { Checked = true };
                    node.Nodes.Add(existing);
                }
                node = existing;
            }
            node.Tag = lv;
        }

        var folderRoot = new TreeNode("Folders");
        root.Nodes.Add(folderRoot);
        foreach (var lv in leftovers.Where(l => l.Kind == LeftoverKind.Folder))
        {
            var node = new TreeNode(lv.Path) { Tag = lv, Checked = true };
            folderRoot.Nodes.Add(node);
        }

        root.ExpandAll();

        var infoPanel = new Panel { Dock = DockStyle.Bottom, Height = 70 };
        infoPanel.Controls.Add(_valuesLabel);
        infoPanel.Controls.Add(_keysLabel);
        infoPanel.Controls.Add(_pathLabel);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _cancel, _finish, _delete, _deselectAll });

        _tree.AfterSelect += (_, _) => UpdateInfo();
        _tree.AfterCheck += (_, e) => { if (e.Node is not null) PropagateCheck(e.Node, e.Node.Checked); };

        _deselectAll.Click += (_, _) => SetAllChecked(false);
        _delete.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        Controls.Add(_tree);
        Controls.Add(infoPanel);
        Controls.Add(buttons);
        CancelButton = _cancel;
        AcceptButton = _finish;

        UpdateInfo();
    }

    private void PropagateCheck(TreeNode node, bool check)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = check;
            PropagateCheck(child, check);
        }
    }

    private void SetAllChecked(bool value)
    {
        foreach (TreeNode n in _tree.Nodes) { n.Checked = value; PropagateCheck(n, value); }
    }

    private void UpdateInfo()
    {
        var node = _tree.SelectedNode;
        if (node is null) { _pathLabel.Text = "Path:"; _keysLabel.Text = "Keys:"; _valuesLabel.Text = "Values:"; return; }
        var parts = new List<string>();
        var cur = node;
        while (cur is not null && cur.Parent is not null)
        {
            parts.Insert(0, cur.Text);
            cur = cur.Parent;
        }
        _pathLabel.Text = "Path: " + string.Join("\\", parts);

        if (node.Tag is Leftover lv && lv.Kind == LeftoverKind.RegistryKey)
        {
            try
            {
                if (RegistryPath.TryParse(lv.Path, out var hive, out var segments))
                {
                    using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                    using var key = root.OpenSubKey(string.Join('\\', segments));
                    if (key is not null)
                    {
                        _keysLabel.Text = "Keys: " + key.SubKeyCount;
                        _valuesLabel.Text = "Values: " + key.ValueCount;
                    }
                    else { _keysLabel.Text = "Keys: 0"; _valuesLabel.Text = "Values: 0"; }
                }
            }
            catch { _keysLabel.Text = "Keys: -"; _valuesLabel.Text = "Values: -"; }
        }
        else
        {
            _keysLabel.Text = "Keys: -";
            _valuesLabel.Text = "Values: -";
        }
    }

    public IEnumerable<Leftover> SelectedLeftovers =>
        CollectChecked(_tree.Nodes).Where(n => n.Tag is Leftover).Select(n => (Leftover)n.Tag!);

    public IEnumerable<Leftover> SelectedFolders =>
        SelectedLeftovers.Where(l => l.Kind == LeftoverKind.Folder);

    public IEnumerable<Leftover> SelectedRegistryKeys =>
        SelectedLeftovers.Where(l => l.Kind == LeftoverKind.RegistryKey);

    private static IEnumerable<TreeNode> CollectChecked(TreeNodeCollection nodes)
    {
        foreach (TreeNode n in nodes)
        {
            if (n.Checked) yield return n;
            foreach (var c in CollectChecked(n.Nodes)) yield return c;
        }
    }
}