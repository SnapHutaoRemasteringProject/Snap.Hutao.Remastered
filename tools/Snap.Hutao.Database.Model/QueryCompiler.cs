using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Design.Internal;
using Snap.Hutao.Remastered.Model.Entity.Database;
using System.Text;
using System.Security.Cryptography;

namespace Snap.Hutao.Remastered.DatabaseTooling;

#pragma warning disable EF1001 // Design-only adapter pinned to EF 11; same internal services used by dotnet-ef. Excluded from native compilation.

[EntityFrameworkInternal]
internal static class QueryCompiler
{
    public static void GenerateModel(string repositoryRoot, bool check = false)
    {
        using AppDbContext context = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);
        ServiceCollection services = new();
        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(context);
        new SqliteDesignTimeServices().ConfigureDesignTimeServices(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        // Generate from the design model, never from the previously compiled runtime model.
        IModel model = context.GetService<IDesignTimeModel>().Model;
        byte[] modelHash = SHA256.HashData(Encoding.UTF8.GetBytes(model.ToDebugString(MetadataDebugStringOptions.LongDefault).Replace("\r\n", "\n")));
        ((Microsoft.EntityFrameworkCore.Metadata.Internal.Model)model).ModelId = new Guid(modelHash.AsSpan(0, 16));
        CompiledModelCodeGenerationOptions options = new()
        {
            ContextType = typeof(AppDbContext),
            ModelNamespace = "Snap.Hutao.Remastered.Model.Entity.Database.Compiled",
            Language = LanguageNames.CSharp,
            ForNativeAot = true,
            UseNullableReferenceTypes = false,
            ProviderName = context.Database.ProviderName,
            GeneratedFileNames = new HashSet<string>(StringComparer.Ordinal)
        };
        IReadOnlyCollection<ScaffoldedFile> files = provider.GetRequiredService<ICompiledModelCodeGeneratorSelector>().Select(options).GenerateModel(model, options);
        WriteFiles(repositoryRoot, files, interceptors: false, check);
    }

    public static void Generate(string repositoryRoot, bool check = false)
    {
        string app = Path.Combine(Path.GetFullPath(repositoryRoot), "src/Snap.Hutao.Remastered/Snap.Hutao.Remastered");
        string output = Path.Combine(app, "Model/Entity/Database/Compiled");
        string[] sources = Directory.GetFiles(Path.Combine(app, "Core/Database"), "*Queries.cs").Order(StringComparer.Ordinal).ToArray();
        string[] references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("Use the managed generation host.")).Split(Path.PathSeparator);
        SyntaxTree[] trees = sources.Select(source => CSharpSyntaxTree.ParseText(File.ReadAllText(source), new CSharpParseOptions(LanguageVersion.Preview), source, Encoding.UTF8)).ToArray();
        CSharpCompilation compilation = CSharpCompilation.Create("HutaoQueryGeneration", trees, references.Select(path => MetadataReference.CreateFromFile(path)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Diagnostic[] errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        using AppDbContext context = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);
        ServiceCollection services = new();
        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(context);
        new SqliteDesignTimeServices().ConfigureDesignTimeServices(services);
        using ServiceProvider provider = services.BuildServiceProvider();
        IPrecompiledQueryCodeGenerator generator = provider.GetRequiredService<IPrecompiledQueryCodeGeneratorSelector>().Select(LanguageNames.CSharp);
        using AdhocWorkspace workspace = new();
        List<PrecompiledQueryCodeGenerator.QueryPrecompilationError> queryErrors = [];
        HashSet<string> names = Directory.GetFiles(output, "*.cs").Select(Path.GetFileName).OfType<string>().Where(name => !name.Contains("EFInterceptors", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<ScaffoldedFile> files = generator.GeneratePrecompiledQueries(compilation, SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp), context, ((IRuntimeModel)context.Model).GetUnsafeAccessors(), queryErrors, names, typeof(AppDbContext).Assembly);
        if (queryErrors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, queryErrors));
        if (files.Count == 0) throw new InvalidOperationException("No query interceptors were generated.");
        List<ScaffoldedFile> rewritten = [];
        foreach (ScaffoldedFile file in files)
        {
            // rc.3 generates interface tests/casts directly on sealed entities. Cast via
            // object to preserve the expression-tree semantics in legal C# source.
            SyntaxNode syntax = CSharpSyntaxTree.ParseText(file.Code, new CSharpParseOptions(LanguageVersion.Preview)).GetRoot();
            string code = "#nullable enable annotations\n#nullable disable warnings\n" + new InjectableServiceCastRewriter().Visit(syntax)!.NormalizeWhitespace(eol: "\n").ToFullString();
            rewritten.Add(new ScaffoldedFile(file.Path, code));
        }
        WriteFiles(repositoryRoot, rewritten, interceptors: true, check);
    }

    private static void WriteFiles(string repositoryRoot, IReadOnlyCollection<ScaffoldedFile> files, bool interceptors, bool check)
    {
        string output = Path.Combine(Path.GetFullPath(repositoryRoot), "src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Entity/Database/Compiled");
        Directory.CreateDirectory(output);
        HashSet<string> expected = files.Select(file => file.Path).ToHashSet(StringComparer.Ordinal);
        foreach (ScaffoldedFile file in files)
        {
            string path = Path.Combine(output, file.Path);
            string code = file.Code.Replace("\r\n", "\n");
            if (check)
            {
                if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != code)
                    throw new InvalidOperationException("Outdated generated EF file: " + path);
            }
            else
            {
                File.WriteAllText(path, code, new UTF8Encoding(false));
            }
        }
        foreach (string path in Directory.GetFiles(output, "*.cs").Where(path => Path.GetFileName(path).Contains("EFInterceptors", StringComparison.Ordinal) == interceptors))
        {
            if (!expected.Contains(Path.GetFileName(path)))
            {
                if (check) throw new InvalidOperationException("Obsolete generated EF file: " + path);
                File.Delete(path);
            }
        }
        Console.WriteLine($"{(check ? "Verified" : "Generated")} {files.Count} official EF {(interceptors ? "query interceptor" : "native model/accessor")} files.");
    }

    private sealed class InjectableServiceCastRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitCastExpression(CastExpressionSyntax node)
        {
            CastExpressionSyntax visited = (CastExpressionSyntax)base.VisitCastExpression(node)!;
            return visited.Type.ToString() == "IInjectableService" ? visited.WithExpression(AsObject(visited.Expression)) : visited;
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            BinaryExpressionSyntax visited = (BinaryExpressionSyntax)base.VisitBinaryExpression(node)!;
            return visited.IsKind(SyntaxKind.IsExpression) && visited.Right.ToString() == "IInjectableService" ? visited.WithLeft(AsObject(visited.Left)) : visited;
        }

        private static ExpressionSyntax AsObject(ExpressionSyntax expression) => SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), expression.WithoutTrivia()));
    }
}
