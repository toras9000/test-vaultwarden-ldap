#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.102.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    var composeFile = ThisSource.RelativeFile("./compose.yml");
    await "docker".args(
        "compose", "--file", composeFile, "exec", "ldap",
        "ldapadd", "-Q", "-Y", "EXTERNAL", "-H", "ldapi:///", "-f", "/ldifs/memberof.ldif"
    );
});
