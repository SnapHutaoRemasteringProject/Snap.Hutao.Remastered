using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.SpiralAbyss;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Snap.Hutao.Remastered.DatabaseTooling;

// Managed design-time fixture construction only; excluded from native compilation.
internal static class FixtureBuilder
{
    public static void Generate(string root)
    {
        string output = Path.Combine(Path.GetFullPath(root), "tools/Snap.Hutao.Database.Model/Fixtures");
        Directory.CreateDirectory(output);
        Dictionary<string, string> samples = new()
        {
            ["avatar"] = PersistenceJson.Serialize((DetailedCharacter)Create(typeof(DetailedCharacter))),
            ["daily-note"] = PersistenceJson.Serialize((DailyNote)Create(typeof(DailyNote))),
            ["spiral-abyss"] = PersistenceJson.Serialize((SpiralAbyss)Create(typeof(SpiralAbyss))),
            ["role-combat"] = PersistenceJson.Serialize((RoleCombatData)Create(typeof(RoleCombatData))),
            ["hard-challenge"] = PersistenceJson.Serialize((HardChallengeData)Create(typeof(HardChallengeData)))
        };
        foreach ((string name, string json) in samples) File.WriteAllText(Path.Combine(output, name + ".json"), json, new UTF8Encoding(false));
        Console.WriteLine("Generated five real persistence fixtures.");
    }

    private static object Create(Type type)
    {
        if (type == typeof(string)) return "AOT";
        if (type == typeof(Uri)) return new Uri("https://example.invalid/aot");
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.UnixEpoch.AddDays(20000);
        if (type == typeof(DateTime)) return DateTime.UnixEpoch.AddDays(20000);
        if (type.IsEnum) return Enum.ToObject(type, 0);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>)) return type.GetField("Empty", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        if (type.IsValueType) return Activator.CreateInstance(type)!;
        if (type.IsArray) return Array.CreateInstance(type.GetElementType()!, 0);
        object value = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Cannot create " + type);
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>))) return value;
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanWrite && property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always) property.SetValue(value, Create(property.PropertyType));
        }
        return value;
    }
}
