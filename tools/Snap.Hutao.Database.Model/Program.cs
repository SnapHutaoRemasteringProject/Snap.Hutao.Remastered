using Microsoft.EntityFrameworkCore;
#if !HUTAO_NATIVEAOT
if (args is ["--generate-model", var modelRoot]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.GenerateModel(modelRoot); return; }
if (args is ["--check-model", var checkModelRoot]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.GenerateModel(checkModelRoot, check: true); return; }
if (args is ["--generate-queries", var root]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.Generate(root); return; }
if (args is ["--check-queries", var checkRoot]) { Snap.Hutao.Remastered.DatabaseTooling.QueryCompiler.Generate(checkRoot, check: true); return; }
if (args is ["--generate-fixtures", var fixtureRoot]) { Snap.Hutao.Remastered.DatabaseTooling.FixtureBuilder.Generate(fixtureRoot); return; }
if (args is ["--check-fixtures", var checkFixtureRoot]) { Snap.Hutao.Remastered.DatabaseTooling.FixtureBuilder.Generate(checkFixtureRoot, check: true); return; }
#endif
DatabaseVerification.Run();
