#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.3-rev.1"
#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Kokuban, 0.2.0"
#load ".settings.csx"
#nullable enable
using System.Text.Json;
using Kokuban;
using Lestaly;
using VwConnector;
using VwConnector.Agent;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    var testUser = vwSettings.Setup.TestUser;
    var testOrg = vwSettings.Setup.TestOrg;

    WriteLine("Prepare test user");
    using var connector = new VaultwardenConnector(vwSettings.Service.Url);
    var adminToken = await connector.Admin.GetTokenAsync(vwSettings.Setup.Admin.Password, signal.Token);
    var users = await connector.Admin.UsersAsync(adminToken, signal.Token);
    if (users.Any(u => u.email == testUser.Mail))
    {
        WriteLine(".. Already exists");
        return;
    }

    WriteLine("Register test user");
    await connector.Account.RegisterUserNoSmtpAsync(new(testUser.Mail, testUser.Password));
    var agent = await VaultwardenAgent.CreateAsync(connector, new(testUser.Mail, testUser.Password), signal.Token);
    WriteLine($".. Created - {agent.Profile.id}");

    WriteLine("Prepare test org");
    var org = await agent.Affect.CreateOrganizationAsync(testOrg.Name, "DefaultCollection", signal.Token);
    WriteLine($".. Created - {org.Id}");

    WriteLine("Prepare test collections");
    foreach (var name in testOrg.Collections)
    {
        WriteLine($"  {name}");
        var collection = await agent.Affect.CreateCollectionAsync(org.Id, name, signal.Token);
        WriteLine($"  .. Created - {collection.Id}");
    }

});
