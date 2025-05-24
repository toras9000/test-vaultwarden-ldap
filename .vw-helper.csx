#r "nuget: Lestaly, 0.81.0"
#r "nuget: Lestaly, 0.82.0"
#nullable enable
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Lestaly;

enum MembershipStatus
{
    Revoked = -1,
    Invited = 0,
    Accepted = 1,
    Confirmed = 2,
}

enum MembershipType
{
    Owner = 0,
    Admin = 1,
    User = 2,
    Manager = 3,
}

enum EditMembershipType
{
    Owner = 0,
    Admin = 1,
    User = 2,
    Manager = 3,
    Custom = 4,
}

record AdminToken(string token);

#region ConnectToken
enum ClientDeviceType
{
    Android = 0,
    iOS = 1,
    ChromeExtension = 2,
    FirefoxExtension = 3,
    OperaExtension = 4,
    EdgeExtension = 5,
    WindowsDesktop = 6,
    MacOsDesktop = 7,
    LinuxDesktop = 8,
    ChromeBrowser = 9,
    FirefoxBrowser = 10,
    OperaBrowser = 11,
    EdgeBrowser = 12,
    IEBrowser = 13,
    UnknownBrowser = 14,
    AndroidAmazon = 15,
    Uwp = 16,
    SafariBrowser = 17,
    VivaldiBrowser = 18,
    VivaldiExtension = 19,
    SafariExtension = 20,
    Sdk = 21,
    Server = 22,
    WindowsCLI = 23,
    MacOsCLI = 24,
    LinuxCLI = 25,
};
record ConnectTokenModel(string grant_type);
record RefreshConnectTokenModel(string refresh_token) : ConnectTokenModel("refresh_token");
record ScopedConnectTokenModel(string grant_type, string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier) : ConnectTokenModel(grant_type);
record PasswordConnectTokenModel(string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier, string username, string password)
    : ScopedConnectTokenModel("password", scope, client_id, device_type, device_name, device_identifier);
record ClientCredentialsConnectTokenModel(string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier, string client_secret)
    : ScopedConnectTokenModel("client_credentials", scope, client_id, device_type, device_name, device_identifier);

record ConnectTokenResult(string token_type, string access_token, long expires_in);

record PasswordConnectTokenMasterPasswordPolicy(string @object);
record PasswordConnectTokenUserDecryptionOptions(string Object, bool userDecryptionOptions);
record PasswordConnectTokenResult(
    string token_type, string access_token, long expires_in,
    string scope, string refresh_token,
    int Kdf, int KdfIterations, int? KdfMemory, int? KdfParallelism,
    string Key, string PrivateKey,
    bool ForcePasswordReset, bool ResetMasterPassword,
    PasswordConnectTokenMasterPasswordPolicy MasterPasswordPolicy,
    PasswordConnectTokenUserDecryptionOptions UserDecryptionOptions
) : ConnectTokenResult(token_type, access_token, expires_in);
#endregion

#region Prelogin
record PreloginArgs(string email);
record PreloginResult(int kdf, int kdfIterations, long? kdfMemory, long? kdfParallelism);
#endregion

record VwUser(
    long _status, string @object, string id, string name, string email, bool emailVerified,
    bool premium, bool premiumFromOrganization, string? masterPasswordHint,
    string culture, bool twoFactorEnabled, string key, string? privateKey, string securityStamp,
    VwOrganization[] organizations, object[] providers, object[] providerOrganizations,
    bool forcePasswordReset, string? avatarColor, bool usesKeyConnector, DateTime creationDate
);

record VwPermissions(
    bool accessEventLogs, bool accessImportExport, bool accessReports,
    bool createNewCollections, bool editAnyCollection, bool deleteAnyCollection,
    bool manageGroups, bool managePolicies, bool manageSso,
    bool manageUsers, bool manageResetPassword, bool manageScim
);

record VwOrganization(
    string @object, string id, string name, string organizationUserId,
    bool enabled, string userId, MembershipStatus status, MembershipType type, string key,
    int maxStorageGb, int planProductType, int productTierType,
    object? providerId, object? providerName, object? providerType,
    object? identifier, object? seats, object? maxCollections,
    bool hasPublicAndPrivateKeys, bool selfHost, bool usersGetPremium,
    bool use2fa, bool useActivateAutofillPolicy, bool useApi,
    bool useCustomPermissions, bool useDirectory, bool useEvents, bool useGroups,
    bool useKeyConnector, bool usePasswordManager, bool usePolicies, bool useResetPassword,
    bool useScim, bool useSecretsManager, bool useSso, bool useTotp,
    bool resetPasswordEnrolled, bool ssoBound, bool userIsManagedByOrganization,
    bool accessSecretsManager, bool allowAdminAccessToAllCollectionItems,
    bool familySponsorshipAvailable, object? familySponsorshipFriendlyName,
    object? familySponsorshipLastSyncDate, object? familySponsorshipToDelete, object? familySponsorshipValidUntil,
    bool limitCollectionCreation, bool limitCollectionCreationDeletion, bool limitCollectionDeletion,
    bool keyConnectorEnabled, object? keyConnectorUrl,
    VwPermissions? permissions
);

record VwCollection(string id, bool readOnly, bool hidePasswords, bool manage);

record VwApiKey(string @object, string apiKey, DateTime revisionDate);

record PasswordOrOtp(string? masterPasswordHash = default, string? otp = default);

record UserPublicKey(string @object, string userId, string publicKey);

record OrgPublicKey(string @object, string publicKey);

#region OrgMembers
record OrgMembersArgs(bool? includeCollections = default, bool? includeGroups = default);
record OrgMembersUser(
    string @object, string id, string name, string email, string userId, string externalId,
    MembershipStatus status, MembershipType type,
    string? avatarColor, string[]? groups, VwCollection[]? collections, VwPermissions? permissions,
    bool accessAll, bool twoFactorEnabled, bool resetPasswordEnrolled, bool hasMasterPassword,
    bool ssoBound, bool usesKeyConnector, bool accessSecretsManager
);
record OrgMembersResult(string @object, OrgMembersUser[] data);
#endregion

#region CreateOrg
record CreateOrgArgs(string name, string billingEmail, string key, string[]? keys, string collectionName, int planType);
#endregion

#region EditOrg
record EditOrgMemberPermissions(bool createNewCollections, bool editAnyCollection, bool deleteAnyCollection);
record EditOrgMemberArgs(EditMembershipType type, VwCollection[]? collections, string[]? groups, bool access_all, EditOrgMemberPermissions permissions);
#endregion

#region OrgImport
record ImportOrgGroup(string name, string externalId, string[]? memberExternalIds);
record ImportOrgMember(string externalId, string? email, bool deleted);
record ImportOrgArgs(bool overwriteExisting, ImportOrgMember[] members, ImportOrgGroup[] groups);
#endregion

record ConfirmMemberArgs(string key);

class VaultwardenHelper : IDisposable
{
    public VaultwardenHelper(Uri baseUri)
    {
        this.BaseUri = baseUri;
        this.http = new HttpClient(new HttpClientHandler() { UseCookies = false, });
    }

    public Uri BaseUri { get; }

    public TimeSpan Timeout
    {
        get => this.http.Timeout;
        set => this.http.Timeout = value;
    }

    public string CreatePasswordHash(string email, string password, int iteration)
    {
        var passBytes = Encoding.UTF8.GetBytes(password);
        var mailBytes = Encoding.UTF8.GetBytes(email);
        var preKey = Rfc2898DeriveBytes.Pbkdf2(passBytes, mailBytes, iteration, HashAlgorithmName.SHA256, 32);
        var passKey = Rfc2898DeriveBytes.Pbkdf2(preKey, passBytes, 1, HashAlgorithmName.SHA256, 32);
        var passHash = Convert.ToBase64String(passKey);
        return passHash;
    }

    public byte[] EncryptKey(string publicKey, byte[] data)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKey.DecodeBase64(), out _);
        var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
        return encrypted;
    }

    public byte[] GenerateKeyData(bool simple = false)
    {
        if (simple)
        {
            return Random.Shared.GetBytes(64);
        }

        // Imitation of web vault implementation.
        var key = new byte[64];
        using var aes = Aes.Create();
        aes.KeySize = 32 * 8;   // bits
        aes.GenerateKey();
        aes.Key.CopyTo(key.AsSpan(0, 32));
        aes.GenerateKey();
        aes.Key.CopyTo(key.AsSpan(32));
        return key;
    }

    public async Task<PreloginResult> PreloginAsync(PreloginArgs args, CancellationToken cancelToken = default)
    {
        using var request = createJsonRequest(HttpMethod.Post, "identity/accounts/prelogin", default, args);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PreloginResult>() ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<ConnectTokenResult> ConnectTokenAsync<TToken>(TToken data, CancellationToken cancelToken = default) where TToken : ConnectTokenModel
    {
        using var request = createUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ConnectTokenResult>() ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<PasswordConnectTokenResult> ConnectTokenAsync(PasswordConnectTokenModel data, CancellationToken cancelToken = default)
    {
        using var request = createUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PasswordConnectTokenResult>() ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<AdminToken> AdminTokenAsync(string password, CancellationToken cancelToken = default)
    {
        using var request = createUrlEncodedRequest(HttpMethod.Post, "admin", default, new { token = password, });
        using var response = await this.http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) throw new Exception("failed to get token");
        var token = default(AdminToken);
        foreach (var cookie in cookies)
        {
            var scan = cookie.AsSpan();
            while (!scan.IsEmpty)
            {
                var entry = scan.TakeSkipFirstToken(out scan, delimiter: ';');
                var key = entry.SplitAt('=', out var value);
                if (key.Trim().Equals("VW_ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    token = new(value.Trim().ToString());
                    break;
                }
            }
            if (token != null) break;
        }
        return token ?? throw new Exception("failed to get token");
    }

    public async Task<VwUser> AdminInviteAsync(AdminToken token, string email, CancellationToken cancelToken = default)
    {
        using var request = createJsonRequest(HttpMethod.Post, "admin/invite", default, new { email, });
        request.Headers.Add("Cookie", [$"VW_ADMIN={token.token}"]);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUser>(cancelToken) ?? throw new Exception("failed to request");
        return result;
    }

    public async Task<VwUser> GetUserProfile(ConnectTokenResult token, CancellationToken cancelToken = default)
    {
        using var request = createRequest(HttpMethod.Get, $"api/accounts/profile", token);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUser>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwApiKey> GetUserApiKey(ConnectTokenResult token, PasswordOrOtp args, CancellationToken cancelToken = default)
    {
        using var request = createJsonRequest(HttpMethod.Post, $"api/accounts/api-key", token, args);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwApiKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<UserPublicKey> GetUserPublicKey(ConnectTokenResult token, string userId, CancellationToken cancelToken = default)
    {
        var encUserId = Uri.EscapeDataString(userId);
        using var request = createRequest(HttpMethod.Get, $"api/users/{encUserId}/public-key", token);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<UserPublicKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwOrganization> GetOrg(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = createRequest(HttpMethod.Get, $"api/organizations/{encOrgId}", token);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwOrganization>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgMembersResult> CreateOrg(ConnectTokenResult token, CreateOrgArgs? args = default, CancellationToken cancelToken = default)
    {
        using var request = createJsonRequest(HttpMethod.Post, $"api/organizations", token, args);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgMembersResult> GetOrgMembers(ConnectTokenResult token, string orgId, OrgMembersArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = createUrlEncodedRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/users", token, args);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgMembersUser> GetOrgMember(ConnectTokenResult token, string orgId, string memberId, OrgMembersArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = createRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/users/{encMemberId}", token);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersUser>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgPublicKey> GetOrgPublicKey(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = createRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/public-key", token);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgPublicKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwApiKey> GetOrgApiKey(ConnectTokenResult token, string orgId, PasswordOrOtp args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = createJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/api-key", token, args);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwApiKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task EditOrgMember(ConnectTokenResult token, string orgId, string memberId, EditOrgMemberArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = createJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/users/{encMemberId}", token, args);
        using var response = await this.http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }

    public async Task<UserPublicKey> ConfirmOrgMember(ConnectTokenResult token, string orgId, string memberId, ConfirmMemberArgs args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = createRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/users/{encMemberId}/confirm", token);
        using var response = await this.http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<UserPublicKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task PublicOrgImportAsync(ConnectTokenResult token, ImportOrgArgs data, CancellationToken cancelToken = default)
    {
        using var request = createJsonRequest(HttpMethod.Post, "api/public/organization/import", token, data);
        using var response = await this.http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.http?.Dispose();
                this.http = null!;
            }

            this.disposed = true;
        }
    }

    private HttpClient http;
    private bool disposed;
    private readonly JsonSerializerOptions apiSerializeOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private HttpRequestMessage createRequest(HttpMethod method, string endpoint, ConnectTokenResult? token, HttpContent? content = default)
    {
        var apiEndpoint = new Uri(this.BaseUri, endpoint);
        var message = new HttpRequestMessage(method, apiEndpoint);
        if (token != null) message.Headers.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
        if (content != null) message.Content = content;
        return message;
    }

    private HttpRequestMessage createJsonRequest<TData>(HttpMethod method, string endpoint, ConnectTokenResult? token, TData? data = default)
    {
        var content = default(HttpContent);
        if (data != null) content = JsonContent.Create(data, options: this.apiSerializeOptions);
        return createRequest(method, endpoint, token, content);
    }

    private HttpRequestMessage createUrlEncodedRequest<TData>(HttpMethod method, string endpoint, ConnectTokenResult? token, TData? data = default)
    {
        var content = default(HttpContent);
        if (data != null) content = FormUrlEncoded.CreateContent(data);
        return createRequest(method, endpoint, token, content);
    }
}
