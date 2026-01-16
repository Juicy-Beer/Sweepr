namespace Sweepr.Core.Models;

public abstract record RegValueEntry(string Name);

public sealed record RegStringValue(string Name, string Data) : RegValueEntry(Name);

public sealed record RegExpandStringValue(string Name, string Data) : RegValueEntry(Name);

public sealed record RegMultiStringValue(string Name, IReadOnlyList<string> Data) : RegValueEntry(Name);

public sealed record RegDWordValue(string Name, uint Data) : RegValueEntry(Name);

public sealed record RegQWordValue(string Name, ulong Data) : RegValueEntry(Name);

public sealed record RegBinaryValue(string Name, byte[] Data) : RegValueEntry(Name);

public sealed record RegNoneValue(string Name, byte[] Data) : RegValueEntry(Name);

public sealed record RegKeyEntry(string Path, IReadOnlyList<RegValueEntry> Values);
