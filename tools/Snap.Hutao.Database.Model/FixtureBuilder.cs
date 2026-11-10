using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.SpiralAbyss;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.DatabaseTooling;

// Managed design-time fixture construction only; excluded from native compilation.
internal static class FixtureBuilder
{
    public static void Generate(string root, bool check = false)
    {
        string output = Path.Combine(Path.GetFullPath(root), "tools/Snap.Hutao.Database.Model/Fixtures");
        Directory.CreateDirectory(output);
        Dictionary<string, string> samples = new()
        {
            ["avatar"] = SerializeCompatible<DetailedCharacter>(),
            ["daily-note"] = SerializeCompatible<DailyNote>(),
            ["spiral-abyss"] = SerializeCompatible<SpiralAbyss>(),
            ["role-combat"] = SerializeCompatible<RoleCombatData>(),
            ["hard-challenge"] = SerializeCompatible<HardChallengeData>()
        };
        foreach ((string name, string json) in samples)
        {
            string path = Path.Combine(output, name + ".json");
            if (check)
            {
                if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != json)
                    throw new InvalidOperationException("Outdated persistence fixture: " + path);
            }
            else File.WriteAllText(path, json, new UTF8Encoding(false));
        }
        foreach (string path in Directory.GetFiles(output, "*.json"))
        {
            if (!samples.ContainsKey(Path.GetFileNameWithoutExtension(path)))
            {
                if (check) throw new InvalidOperationException("Obsolete persistence fixture: " + path);
                File.Delete(path);
            }
        }
        Console.WriteLine($"{(check ? "Verified" : "Generated")} five populated real persistence fixtures against the reflection wire format.");
    }

    private static string SerializeCompatible<T>()
    {
        T value = (T)Create(typeof(T));
        JsonSerializerOptions legacy = JsonWireOptions.Create();
        legacy.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        string json = JsonSerializer.Serialize(value, legacy);
        if (PersistenceJson.Serialize(value) != json
            || PersistenceJson.Serialize(PersistenceJson.Deserialize<T>(json)) != json
            || JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(PersistenceJson.Serialize(value), legacy), legacy) != json)
            throw new InvalidOperationException("Persistence wire format differs for " + typeof(T));
        return json.Replace("\r\n", "\n");
    }

    private static object Create(Type type)
    {
        if (type == typeof(string)) return "AOT 中文 <&> \" '";
        if (type == typeof(Uri)) return new Uri("https://example.invalid/aot");
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.UnixEpoch.AddDays(20000);
        if (type == typeof(DateTime)) return DateTime.UnixEpoch.AddDays(20000);
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0)!;
        if (type == typeof(bool)) return true;
        if (type.IsPrimitive || type == typeof(decimal)) return Convert.ChangeType(7, type);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            return typeof(FixtureBuilder).GetMethod(nameof(CreateImmutable), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type.GenericTypeArguments).Invoke(null, null)!;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>))
            return typeof(FixtureBuilder).GetMethod(nameof(CreateImmutableDictionary), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type.GenericTypeArguments).Invoke(null, null)!;
        if (Nullable.GetUnderlyingType(type) is Type underlying) return Create(underlying);
        if (type.IsValueType)
        {
            ConstructorInfo? identity = type.GetConstructor([typeof(uint)]);
            return identity is not null ? identity.Invoke([(uint)123]) : Activator.CreateInstance(type)!;
        }
        if (type.IsArray)
        {
            Array array = Array.CreateInstance(type.GetElementType()!, 1);
            array.SetValue(Create(type.GetElementType()!), 0);
            return array;
        }
        object value = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Cannot create " + type);
        if (value is IList list) { list.Add(Create(type.GenericTypeArguments[0])); return value; }
        if (value is IDictionary dictionary) { dictionary.Add(Create(type.GenericTypeArguments[0]), Create(type.GenericTypeArguments[1])); return value; }
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanWrite && property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always) property.SetValue(value, Create(property.PropertyType));
        }
        return value;
    }

    private static ImmutableArray<T> CreateImmutable<T>() => [(T)Create(typeof(T))];

    private static ImmutableDictionary<TKey, TValue> CreateImmutableDictionary<TKey, TValue>() where TKey : notnull
        => ImmutableDictionary<TKey, TValue>.Empty.Add((TKey)Create(typeof(TKey)), (TValue)Create(typeof(TValue)));
}
