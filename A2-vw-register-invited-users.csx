#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.3-rev.1"
#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Kokuban, 0.2.0"
#load ".settings.csx"
#nullable enable
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector;
using VwConnector.Agent;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    var testUser = vwSettings.Setup.TestUser;
    var testOrg = vwSettings.Setup.TestOrg;

    WriteLine("Prepare client");
    var agent = await VaultwardenAgent.CreateAsync(vwSettings.Service.Url, new(testUser.Mail, testUser.Password), signal.Token);
    WriteLine($".. Created - {agent.Profile.id}");

    WriteLine("Confirm org members");
    var org = agent.Profile.organizations.FirstOrDefault(o => o.name == testOrg.Name) ?? throw new Exception($"Missing org '{testOrg.Name}'");
    var orgMembers = await agent.Connector.Organization.GetMembersAsync(agent.Token, org.id, new(false, false), signal.Token);
    foreach (var member in orgMembers.data)
    {
        WriteLine($"{member.email}: {member.id}");
        if (member.status != MembershipStatus.Invited)
        {
            WriteLine($".. Skip: Not target state");
            continue;
        }

        var uid = member.email.TakeToken('@');
        var passwd = $"{uid}-pass";
        await agent.Connector.Account.RegisterUserNoSmtpAsync(new(member.email, passwd));
        WriteLine($"..  Registered");
    }

});
