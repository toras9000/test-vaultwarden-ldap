#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Playwright, 1.52.0"
#r "nuget: Lestaly, 0.84.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#load ".vw-helper.csx"
#nullable enable
using System.Text.Json;
using Microsoft.Playwright;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using System.Threading;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Invite test user");
    var userInfo = vwSettings.Setup.TestUser;
    var orgInfo = vwSettings.Setup.TestOrg;
    using var helper = new VaultwardenHelper(new(vwSettings.Service.Url));
    var adminToken = await helper.Admin.GetTokenAsync(vwSettings.Setup.Admin.Password, signal.Token);
    var testUser = await helper.Admin.InviteAsync(adminToken, userInfo.Mail, signal.Token);

    WriteLine("Detection invite mail");
    var joinUri = default(Uri);
    var mailDir = ThisSource.RelativeDirectory("maildump");
    using (var breaker = signal.Token.CreateLink(TimeSpan.FromSeconds(30)))
    {
        var encUser = Uri.EscapeDataString(userInfo.Mail);
        while (true)
        {
            var lastMail = mailDir.GetFiles("*-text.txt").OrderByDescending(f => f.Name).FirstOrDefault();
            var joinLine = Try.Func(() => lastMail?.ReadAllLines().FirstOrDefault(l => l.StartsWith("Click here to join:") && l.Contains(encUser)), _ => default);
            if (joinLine != null) { joinUri = new Uri(joinLine.SkipFirstToken(':').Trim().ToString()); break; }
            await Task.Delay(TimeSpan.FromMilliseconds(500), breaker.Token);
        }
    }

    WriteLine("Prepare playwright");
    var packageVer = typeof(Microsoft.Playwright.Program).Assembly.GetName()?.Version?.ToString(3) ?? "*";
    var packageDir = SpecialFolder.UserProfile().FindPathDirectory([".nuget", "packages", "Microsoft.Playwright", packageVer], MatchCasing.CaseInsensitive);
    Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", packageDir?.FullName);
    Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);

    WriteLine("Register test user");
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, });
    var page = await browser.NewPageAsync();
    {
        var response = await page.GotoAsync(joinUri.AbsoluteUri) ?? throw new PavedMessageException("Cannot access register page");
        await page.Locator("input[id='input-password-form_new-password']").FillAsync(userInfo.Password);
        await page.Locator("input[id='input-password-form_confirm-new-password']").FillAsync(userInfo.Password);
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    WriteLine("Create test organization");
    {
        // This access does not return a response object because of a different hash of the same URL
        await page.GotoAsync("http://localhost:8180/#/create-organization");
        await page.Locator("app-org-info input[type='text']").FillAsync(orgInfo.Name);
        await page.Locator("app-organization-plans button[type='submit']").ClickAsync();
    }

    WriteLine("Get test entities information");
    var userPrelogin = await helper.Identity.PreloginAsync(new(userInfo.Mail), signal.Token);
    var userPassHsah = helper.Utility.CreatePasswordHash(userInfo.Mail, userInfo.Password, userPrelogin);
    var userPassHashB64 = userPassHsah.EncodeBase64();
    var userPassReq = new PasswordConnectTokenModel(
        scope: "api offline_access",
        client_id: "web",
        device_type: ClientDeviceType.UnknownBrowser,
        device_name: Environment.MachineName,
        device_identifier: Environment.MachineName,
        username: userInfo.Mail,
        password: userPassHashB64
    );
    var userToken = await helper.Identity.ConnectTokenAsync(userPassReq, signal.Token);
    var userApiKey = await helper.User.GetApiKey(userToken, new(masterPasswordHash: userPassHashB64), signal.Token);
    var userProfile = default(VwUser);
    for (var i = 0; i < 3; i++)
    {
        var user = await helper.User.GetProfile(userToken, signal.Token);
        if (0 < user.organizations?.Length)
        {
            userProfile = user;
            break;
        }
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    if (userProfile == null) throw new PavedMessageException("Cannot access register page");

    var orgProfile = userProfile.organizations.First(o => o.name == orgInfo.Name);
    var orgApiKey = await helper.Organization.GetApiKey(userToken, orgProfile.id, new(masterPasswordHash: userPassHashB64), signal.Token);

    WriteLine("Create test collections");
    var ownerMember = new VwCollectionMembership(orgProfile.organizationUserId, readOnly: false, hidePasswords: false, manage: true);
    var collections = new List<TestCollection>();
    /*
    foreach (var colName in vwSettings.Setup.TestOrg.Collections)
    {
        var collection = await helper.Organization.CreateCollection(userToken, orgProfile.id, new(name: colName, users: [ownerMember], groups: []));
        collections.Add(new(collection.id, collection.name));
    }
    */

    WriteLine("Save test entities info");
    var testEntities = new TestEntities(
        new TestOrganization(
            Id: orgProfile.id,
            ClientId: $"organization.{orgProfile.id}",
            ClientSecret: orgApiKey.apiKey
        ),
        collections.ToArray(),
        new TestConfirmer(
            Id: userProfile.id,
            ClientId: $"user.{userProfile.id}",
            ClientSecret: userApiKey.apiKey
        )
    );
    await vwSettings.TestEntitiesFile.WriteJsonAsync(testEntities, new JsonSerializerOptions { WriteIndented = true, });
});
