using Snap.Hutao.Plugin.Packaging;

try
{
    switch (args)
    {
        case ["pack", string template, string staging, string output]:
            PluginPackage.Pack(template, staging, output);
            Console.WriteLine(PluginPackage.Verify(output).Sha256);
            return 0;
        case ["verify", string package]:
            VerifiedPackage verified = PluginPackage.Verify(package);
            Console.WriteLine($"{verified.Manifest.Id} {verified.Manifest.Version} SHA256={verified.Sha256}");
            return 0;
        default:
            Console.Error.WriteLine("PluginPack pack <manifest-template.json> <staging-dir> <output.hutao> | verify <package>");
            return 2;
    }
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
