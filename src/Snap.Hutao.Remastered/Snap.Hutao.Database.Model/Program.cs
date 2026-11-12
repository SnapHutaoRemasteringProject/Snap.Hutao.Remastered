using Microsoft.EntityFrameworkCore;
try
{
#if !HUTAO_NATIVEAOT
    if (args is ["--generate-model", var modelRoot]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.GenerateModel(modelRoot); return 0; }
    if (args is ["--check-model", var checkModelRoot]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.GenerateModel(checkModelRoot, check: true); return 0; }
    if (args is ["--generate-queries", var root]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.Generate(root); return 0; }
    if (args is ["--check-queries", var checkRoot]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.Generate(checkRoot, check: true); return 0; }
#endif
    DatabaseVerification.Run();
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Database model/query generation or verification failed:");
    Console.Error.WriteLine(exception);
    return 1;
}
