using Microsoft.EntityFrameworkCore;
#if !HUTAO_NATIVEAOT
if (args is ["--generate-queries", var root]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.Generate(root); return; }
if (args is ["--generate-fixtures", var fixtureRoot]) { Snap.Hutao.Remastered.DatabaseTooling.FixtureBuilder.Generate(fixtureRoot); return; }
#endif
DatabaseVerification.Run();
