#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    await "dotnet".args("script", ThisSource.RelativeFile("01-containers-volume-delete.csx"), "--no-pause").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("02-containers-restart.csx"), "--no-pause").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("11-ldap-setup-memberof.csx"), "--no-pause").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("12-ldap-setup-config-access.csx"), "--no-pause").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("21-vw-create-test-entities.csx"), "--no-pause").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("31-vwli-update-config.csx"), "--no-pause").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("32-vwac-update-config.csx"), "--no-pause").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("@@show-service.csx"), "--no-pause").echo().result().success();
});
