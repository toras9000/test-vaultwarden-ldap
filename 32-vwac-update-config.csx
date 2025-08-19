#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.3-rev.1"
#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: JsonPath.Net, 2.1.1"
#load ".settings.csx"
#nullable enable
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Path;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector;
using VwConnector.Agent;

var settings = new
{
    VwliConfigFile = ThisSource.RelativeFile("./assets/vwac/settings.json"),
};

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    var testUser = vwSettings.Setup.TestUser;
    var testOrg = vwSettings.Setup.TestOrg;

    WriteLine("Get info");
    using var agent = await VaultwardenAgent.CreateAsync(vwSettings.Service.Url, new(testUser.Mail, testUser.Password), signal.Token);
    var userProfile = agent.Profile;
    var orgProfile = agent.Profile.organizations.FirstOrDefault(o => o.name == testOrg.Name) ?? throw new Exception("Not found org");
    var collections = await agent.GetCollectionsAsync(orgProfile.id, signal.Token);
    if (collections.Length <= 0) throw new Exception("No collections");

    WriteLine("Update vaultwarden-ldap-import config");
    var config = await settings.VwliConfigFile.ReadJsonAsync<JsonNode>() ?? throw new Exception("Cannot load config");
    var orgPath = JsonPath.Parse("$.Vaultwarden.Organization");
    var orgMatch = orgPath.Evaluate(config).Matches[0]?.Value ?? throw new Exception("Unknown JSON structure");
    orgMatch["OrgId"] = orgProfile.id;
    var permPath = JsonPath.Parse("$.Vaultwarden.Permissions");
    var permMatch = permPath.Evaluate(config).Matches[0]?.Value ?? throw new Exception("Unknown JSON structure");
    permMatch["Collections"] = new JsonArray(
        collections.Select((c, i) =>
        {
            var node = new JsonObject();
            if (i % 2 == 0) node["Id"] = c.Id; else node["Name"] = c.Name;
            node["Privilege"] = (i % 3) switch { 1 => "Edit", 2 => "Manage", _ => "Show", };
            return node;
        }).ToArray()
    );

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    await settings.VwliConfigFile.WriteJsonAsync(config, options);

    WriteLine("Restart containers.");
    var composeFile = ThisSource.RelativeFile("./compose.yml");
    await "docker".args("compose", "--file", composeFile, "down", "confirmer");
    await "docker".args("compose", "--file", composeFile, "up", "-d", "--wait", "confirmer").result().success();

});
