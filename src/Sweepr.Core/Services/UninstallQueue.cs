using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// a simple queue of programs to uninstall one after another.
public sealed class UninstallQueue
{
    private readonly List<InstalledProgram> _items = new();
    public IReadOnlyList<InstalledProgram> Items => _items;

    public void Add(InstalledProgram program)
    {
        if (program is null) return;
        if (_items.Any(p => p.RegistryKeyPath == program.RegistryKeyPath && p.Name == program.Name)) return;
        _items.Add(program);
    }

    public void Remove(InstalledProgram program) => _items.Remove(program);

    public void Clear() => _items.Clear();

    public bool Contains(InstalledProgram program) => _items.Contains(program);
}