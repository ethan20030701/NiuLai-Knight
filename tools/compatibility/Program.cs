using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

// Checks the compiled assembly, including generated async state machines.
// Unlike a source build, this also detects calls compiled into BaseLib.
var input = args[0];
var dependencies = args.Skip(1).Select(p => new Api(p)).ToArray();
using var source = new Api(input);
var missing = new List<string>();
int checkedMembers = 0;
foreach (var handle in source.Reader.MemberReferences)
{
    var member = source.Reader.GetMemberReference(handle);
    string type = source.Provider.Entity(member.Parent);
    string rawType = type.Split('<')[0];
    var target = dependencies.FirstOrDefault(d => d.Types.ContainsKey(rawType));
    if (target == null) continue;
    checkedMembers++;
    var name = source.Reader.GetString(member.Name);
    var signature = member.GetKind() == MemberReferenceKind.Method
        ? Api.Format(member.DecodeMethodSignature(source.Provider, null))
        : member.DecodeFieldSignature(source.Provider, null);
    if (!target.Has(rawType, name, signature, dependencies))
        missing.Add($"{type}.{name} {signature}");
}
Console.WriteLine(JsonSerializer.Serialize(new { assembly = Path.GetFileName(input), checkedMembers, missing }, new JsonSerializerOptions { WriteIndented = true }));
foreach (var dep in dependencies) dep.Dispose();
return missing.Count == 0 ? 0 : 1;

sealed class Api : IDisposable
{
    readonly FileStream stream;
    readonly PEReader pe;
    public MetadataReader Reader { get; }
    public Sig Provider { get; }
    public Dictionary<string, TypeDefinitionHandle> Types { get; } = [];
    public Api(string path)
    {
        stream = File.OpenRead(path); pe = new PEReader(stream); Reader = pe.GetMetadataReader(); Provider = new Sig(Reader);
        foreach (var t in Reader.TypeDefinitions) Types[Provider.Entity(t)] = t;
    }
    public bool Has(string type, string name, string sig, Api[] deps, int depth = 0)
    {
        if (depth > 12 || !Types.TryGetValue(type, out var h)) return false;
        var t = Reader.GetTypeDefinition(h);
        foreach (var m in t.GetMethods())
        {
            var d = Reader.GetMethodDefinition(m);
            if (Reader.GetString(d.Name) == name && Format(d.DecodeSignature(Provider, null)) == sig) return true;
        }
        foreach (var f in t.GetFields())
        {
            var d = Reader.GetFieldDefinition(f);
            if (Reader.GetString(d.Name) == name && d.DecodeSignature(Provider, null) == sig) return true;
        }
        if (t.BaseType.IsNil) return false;
        string parent = Provider.Entity(t.BaseType).Split('<')[0];
        return deps.Any(d => d.Has(parent, name, sig, deps, depth + 1));
    }
    public static string Format(MethodSignature<string> s) => $"{(s.Header.IsInstance ? "instance" : "static")}<{s.GenericParameterCount}> {s.ReturnType}({string.Join(",", s.ParameterTypes)})";
    public void Dispose() { pe.Dispose(); stream.Dispose(); }
}

sealed class Sig(MetadataReader reader) : ISignatureTypeProvider<string, object?>
{
    public string Entity(EntityHandle h) => h.Kind switch
    {
        HandleKind.TypeDefinition => GetTypeFromDefinition(reader, (TypeDefinitionHandle)h, 0),
        HandleKind.TypeReference => GetTypeFromReference(reader, (TypeReferenceHandle)h, 0),
        HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(this, null),
        _ => h.Kind.ToString()
    };
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k)
    {
        var t = r.GetTypeDefinition(h);
        return t.IsNested ? Entity(t.GetDeclaringType()) + "+" + r.GetString(t.Name) : Join(r.GetString(t.Namespace), r.GetString(t.Name));
    }
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k)
    {
        var t = r.GetTypeReference(h);
        return t.ResolutionScope.Kind == HandleKind.TypeReference ? Entity(t.ResolutionScope) + "+" + r.GetString(t.Name) : Join(r.GetString(t.Namespace), r.GetString(t.Name));
    }
    static string Join(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;
    public string GetTypeFromSpecification(MetadataReader r, object? c, TypeSpecificationHandle h, byte k) => r.GetTypeSpecification(h).DecodeSignature(this, c);
    public string GetGenericInstantiation(string t, ImmutableArray<string> a) => t + "<" + string.Join(",", a) + ">";
    public string GetGenericMethodParameter(object? c, int i) => "!!" + i;
    public string GetGenericTypeParameter(object? c, int i) => "!" + i;
    public string GetArrayType(string t, ArrayShape s) => t + "[" + new string(',', s.Rank - 1) + "]";
    public string GetSZArrayType(string t) => t + "[]";
    public string GetByReferenceType(string t) => t + "&";
    public string GetPointerType(string t) => t + "*";
    public string GetPinnedType(string t) => t;
    public string GetModifiedType(string m, string t, bool required) => t;
    public string GetPrimitiveType(PrimitiveTypeCode c) => c.ToString();
    public string GetFunctionPointerType(MethodSignature<string> s) => Api.Format(s);
}
