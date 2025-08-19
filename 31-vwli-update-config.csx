#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.3-rev.1"
#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: JsonPath.Net, 2.1.1"
#load ".settings.csx"
#nullable enable
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Json.Path;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector;
using VwConnector.Agent;

var settings = new
{
    VwliConfigFile = ThisSource.RelativeFile("./assets/vwli/settings.json"),
};

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    var testUser = vwSettings.Setup.TestUser;
    var testOrg = vwSettings.Setup.TestOrg;

    WriteLine("Prepare test user");
    using var agent = await VaultwardenAgent.CreateAsync(vwSettings.Service.Url, new(testUser.Mail, testUser.Password), signal.Token);
    var userProfile = agent.Profile;
    var orgProfile = agent.Profile.organizations.First(o => o.name == testOrg.Name);
    WriteLine($".. Created - {agent.Profile.id}");

    WriteLine("Get entities information");
    var userPassHash = agent.Connector.Utility.CreatePasswordHash(testUser.Mail, testUser.Password, agent.Kdf, hashIterations: 1);
    var orgApiKey = await agent.Connector.Organization.GetApiKeyAsync(agent.Token, orgProfile.id, new(userPassHash.EncodeBase64()), signal.Token);

    WriteLine("Update vaultwarden-ldap-import config");
    var config = await settings.VwliConfigFile.ReadJsonAsync<JsonNode>() ?? throw new Exception("Cannot load config");
    var orgPath = JsonPath.Parse("$.Vaultwarden.Organization");
    var orgMatch = orgPath.Evaluate(config).Matches[0]?.Value ?? throw new Exception("Unknown JSON structure");
    orgMatch["OrgId"] = orgProfile.id;
    orgMatch["ClientId"] = $"organization.{orgProfile.id}";
    orgMatch["ClientSecret"] = orgApiKey.apiKey;

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    await settings.VwliConfigFile.WriteJsonAsync(config, options);

    WriteLine("Restart containers.");
    var composeFile = ThisSource.RelativeFile("./compose.yml");
    await "docker".args("compose", "--file", composeFile, "down", "importer");
    await "docker".args("compose", "--file", composeFile, "up", "-d", "--wait", "importer").result().success();

});
