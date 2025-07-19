#r "nuget: Lestaly.General, 0.100.0"
#nullable enable
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using Lestaly;

public enum MembershipStatus
{
    Revoked = -1,
    Invited = 0,
    Accepted = 1,
    Confirmed = 2,
}

public enum MembershipType
{
    Owner = 0,
    Admin = 1,
    User = 2,
    Manager = 3,
}

public enum EditMembershipType
{
    Owner = 0,
    Admin = 1,
    User = 2,
    Manager = 3,
    Custom = 4,
}

public enum EncryptionType
{
    AesCbc256 = 0,
    AesCbc128_HmacSha256 = 1,
    AesCbc256_HmacSha256 = 2,
    Rsa2048_OaepSha256 = 3,
    Rsa2048_OaepSha1 = 4,
    Rsa2048_OaepSha256_HmacSha256 = 5,
    Rsa2048_OaepSha1_HmacSha256 = 6,
}

public record AdminToken(string token);

#region ConnectToken
public enum ClientDeviceType
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
public record ConnectTokenModel(string grant_type);
public record RefreshConnectTokenModel(string refresh_token) : ConnectTokenModel("refresh_token");
public record ScopedConnectTokenModel(string grant_type, string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier) : ConnectTokenModel(grant_type);
public record PasswordConnectTokenModel(string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier, string username, string password)
    : ScopedConnectTokenModel("password", scope, client_id, device_type, device_name, device_identifier);
public record ClientCredentialsConnectTokenModel(string scope, string client_id, ClientDeviceType device_type, string device_name, string device_identifier, string client_secret)
    : ScopedConnectTokenModel("client_credentials", scope, client_id, device_type, device_name, device_identifier);

public record ConnectTokenResult(string token_type, string access_token, long expires_in, string scope);

public record ClientCredentialsConnectTokenResult(
    string token_type, string access_token, long expires_in, string scope,
    KdfType Kdf, int KdfIterations, int? KdfMemory, int? KdfParallelism,
    string Key, string PrivateKey,
    bool ResetMasterPassword
) : ConnectTokenResult(token_type, access_token, expires_in, scope)
{
    public KdfConfig ToKdfConfig() => new KdfConfig(this.Kdf, this.KdfIterations, this.KdfMemory, this.KdfParallelism);
}

public record PasswordConnectTokenMasterPasswordPolicy(string @object);
public record PasswordConnectTokenUserDecryptionOptions(string Object, bool userDecryptionOptions);
public record PasswordConnectTokenResult(
    string token_type, string access_token, long expires_in, string scope,
    KdfType Kdf, int KdfIterations, int? KdfMemory, int? KdfParallelism,
    string Key, string PrivateKey,
    string refresh_token,
    bool ForcePasswordReset, bool ResetMasterPassword,
    PasswordConnectTokenMasterPasswordPolicy MasterPasswordPolicy,
    PasswordConnectTokenUserDecryptionOptions UserDecryptionOptions
) : ConnectTokenResult(token_type, access_token, expires_in, scope);
#endregion

#region Prelogin
public enum KdfType
{
    Pbkdf2 = 0,
    Argon2id = 1,
}
public record PreloginArgs(string email);
public record KdfConfig(KdfType kdf, int kdfIterations, long? kdfMemory, long? kdfParallelism);
public record PreloginResult(KdfType kdf, int kdfIterations, long? kdfMemory, long? kdfParallelism) : KdfConfig(kdf, kdfIterations, kdfMemory, kdfParallelism);
#endregion

public record VwUser(
    long _status, string @object, string id, string name, string email, bool emailVerified,
    bool premium, bool premiumFromOrganization, string? masterPasswordHint,
    string culture, bool twoFactorEnabled, string key, string? privateKey, string securityStamp,
    VwOrganization[] organizations, object[] providers, object[] providerOrganizations,
    bool forcePasswordReset, string? avatarColor, bool usesKeyConnector, DateTime creationDate
);

public record VwPermissions(
    bool accessEventLogs, bool accessImportExport, bool accessReports,
    bool createNewCollections, bool editAnyCollection, bool deleteAnyCollection,
    bool manageGroups, bool managePolicies, bool manageSso,
    bool manageUsers, bool manageResetPassword, bool manageScim
);

public record VwOrganization(
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

public record VwCollection(string id, bool readOnly, bool hidePasswords, bool manage);
public record VwCollectionGroup(string id, bool readOnly, bool hidePasswords, bool manage);
public record VwCollectionMembership(string id, bool readOnly, bool hidePasswords, bool manage);

public record VwApiKey(string @object, string apiKey, DateTime revisionDate);
public record PasswordOrOtp(string? masterPasswordHash = default, string? otp = default);
public record UserPublicKey(string @object, string userId, string publicKey);
public record OrgPublicKey(string @object, string publicKey);

#region OrgMembers
public record OrgMembersArgs(bool? includeCollections = default, bool? includeGroups = default);
public record OrgMembersUser(
    string @object, string id, string name, string email, string userId, string externalId,
    MembershipStatus status, MembershipType type,
    string? avatarColor, string[]? groups, VwCollection[]? collections, VwPermissions? permissions,
    bool accessAll, bool twoFactorEnabled, bool resetPasswordEnrolled, bool hasMasterPassword,
    bool ssoBound, bool usesKeyConnector, bool accessSecretsManager
);
public record OrgMembersResult(string @object, OrgMembersUser[] data);
#endregion

#region OrgCollections
public record OrgCollectionDetail(string @object, string id, string organizationId, string name, string[] externalId, bool readOnly, bool hidePasswords, bool manage);
public record CreateCollectionArgs(string name, VwCollectionMembership[] users, VwCollectionGroup[] groups, string? id = default, string? external_id = default);
public record OrgCollection(string @object, string id, string organizationId, string name, string[] externalId);
public record OrgCollectionsResult(string @object, OrgCollection[] data);
#endregion

#region CreateOrg
public record CreateOrgArgs(string name, string billingEmail, string key, string[]? keys, string collectionName, int planType);
#endregion

#region EditOrg
public record EditOrgMemberPermissions(bool createNewCollections, bool editAnyCollection, bool deleteAnyCollection);
public record EditOrgMemberArgs(EditMembershipType type, VwCollection[]? collections, string[]? groups, bool access_all, EditOrgMemberPermissions permissions);
#endregion

#region OrgImport
public record ImportOrgGroup(string name, string externalId, string[]? memberExternalIds);
public record ImportOrgMember(string externalId, string? email, bool deleted);
public record ImportOrgArgs(bool overwriteExisting, ImportOrgMember[] members, ImportOrgGroup[] groups);
#endregion

#region OrgConfirm
public record ConfirmMemberArgs(string key);
#endregion

#region Ciphers
public enum CipherType
{
    Login = 1,
    SecureNote = 2,
    Card = 3,
    Identity = 4,
    SshKey = 5,
}
public enum RepromptType
{
    None = 0,
    Password = 1,
}
public record CipherItem(
    string @object, string @id, CipherType type,
    DateTime creationDate, DateTime revisionDate, DateTime? deletedDate,
    string organizationId, bool organizationUseTotp,
    string name, string key, RepromptType reprompt,
    bool favorite, bool edit, bool viewPassword,
    string? folderId, string? notes,
    JsonElement? data,
    string[] collectionIds,
    JsonElement? attachments,
    JsonElement? fields,
    JsonElement? passwordHistory,
    JsonElement? login,
    JsonElement? secureNote,
    JsonElement? card,
    JsonElement? identity,
    JsonElement? sshKey
);
public record CipherItemsResult(string @object, CipherItem[] data);
#endregion

public record EncryptedData(EncryptionType Type, byte[] Data, byte[]? IV = default, byte[]? MAC = default)
{
    public static EncryptedData Parse(ReadOnlySpan<char> encryptedString)
        => EncryptedData.TryParse(encryptedString, out var key) ? key : throw new Exception("cannot parse encrypted key");

    public static bool TryParse(ReadOnlySpan<char> encryptedString, [NotNullWhen(true)] out EncryptedData? value)
    {
        value = default;
        if (encryptedString.IsWhiteSpace()) return false;
        var scan = encryptedString;
        var typeNum = scan.TakeSkipToken(out scan, '.').TryParseNumber<int>();
        if (!typeNum.HasValue) return false;
        var type = (EncryptionType)typeNum.Value;
        if (!Enum.IsDefined(type)) return false;
        var part1 = scan.TakeSkipToken(out scan, '|');
        var part2 = scan.TakeSkipToken(out scan, '|');
        var part3 = scan.TakeSkipToken(out scan, '|');

        static bool tryConstruct(EncryptionType type, ReadOnlySpan<char> data, ReadOnlySpan<char> iv, ReadOnlySpan<char> mac, [NotNullWhen(true)] out EncryptedData? value)
        {
            value = default;
            var dataBytes = data.DecodeBase64();
            if (dataBytes == null) return false;
            var ivBytes = iv.DecodeBase64();
            var macBytes = mac.DecodeBase64();
            value = new(type, Data: dataBytes, IV: ivBytes, MAC: macBytes);
            return true;
        }

        switch (type)
        {
            case EncryptionType.AesCbc256:
                return tryConstruct(type, data: part2, iv: part1, mac: null, out value);
            case EncryptionType.AesCbc128_HmacSha256:
            case EncryptionType.AesCbc256_HmacSha256:
                return tryConstruct(type, data: part2, iv: part1, mac: part3, out value);
            case EncryptionType.Rsa2048_OaepSha1:
            case EncryptionType.Rsa2048_OaepSha256:
                return tryConstruct(type, data: part1, iv: null, mac: null, out value);
            case EncryptionType.Rsa2048_OaepSha1_HmacSha256:
            case EncryptionType.Rsa2048_OaepSha256_HmacSha256:
                return tryConstruct(type, data: part1, iv: null, mac: part2, out value);
            default:
                return false;
        }
    }

    public string BuildString()
    {
        var builder = new StringBuilder();
        builder.Append((int)this.Type).Append('.');
        switch (this.Type)
        {
            case EncryptionType.AesCbc256:
                builder.Append(this.IV.EncodeBase64()).Append('|');
                builder.Append(this.Data.EncodeBase64());
                break;
            case EncryptionType.AesCbc128_HmacSha256:
            case EncryptionType.AesCbc256_HmacSha256:
                builder.Append(this.IV.EncodeBase64()).Append('|');
                builder.Append(this.Data.EncodeBase64()).Append('|');
                builder.Append(this.MAC.EncodeBase64());
                break;
            case EncryptionType.Rsa2048_OaepSha1:
            case EncryptionType.Rsa2048_OaepSha256:
                builder.Append(this.Data.EncodeBase64());
                break;
            case EncryptionType.Rsa2048_OaepSha1_HmacSha256:
            case EncryptionType.Rsa2048_OaepSha256_HmacSha256:
                builder.Append(this.Data.EncodeBase64()).Append('|');
                builder.Append(this.MAC.EncodeBase64());
                break;
            default:
                throw new NotImplementedException();
        }
        return builder.ToString();
    }

    private static bool TryParseWithIv(EncryptionType type, ReadOnlySpan<char> data, ReadOnlySpan<char> iv, [NotNullWhen(true)] out EncryptedData? value)
    {
        value = default;
        var dataBytes = data.DecodeBase64();
        var ivBytes = iv.DecodeBase64();
        if (dataBytes == null || ivBytes == null) return false;
        value = new(type, Data: dataBytes, IV: ivBytes);
        return true;
    }

    private static bool TryParseWithIvMac(EncryptionType type, ReadOnlySpan<char> data, ReadOnlySpan<char> iv, ReadOnlySpan<char> mac, [NotNullWhen(true)] out EncryptedData? value)
    {
        value = default;
        var dataBytes = data.DecodeBase64();
        var ivBytes = iv.DecodeBase64();
        var macBytes = mac.DecodeBase64();
        if (dataBytes == null || ivBytes == null || macBytes == null) return false;
        value = new(type, Data: dataBytes, IV: ivBytes);
        return true;
    }

}

public record SymmetricCryptoKey(EncryptionType Type, byte[] EncKey, byte[]? AuthKey)
{
    public static SymmetricCryptoKey From(byte[] data)
    {
        if (data.Length == 32) return new(EncryptionType.AesCbc256, data, null);
        if (data.Length == 64) return new(EncryptionType.AesCbc256_HmacSha256, data[..32], data[32..]);
        throw new InvalidDataException();
    }
}

public class VaultwardenHelper : IDisposable
{
    public VaultwardenHelper(Uri baseUri)
    {
        this.BaseUri = baseUri;
        this.http = new HttpClient(new HttpClientHandler() { UseCookies = false, });

        this.scopeUtility = new VmUtility(this);
        this.scopeIdentity = new VwIdentity(this);
        this.scopeAdmin = new VwAdmin(this);
        this.scopeUser = new VwUser(this);
        this.scopeOrg = new VwOrganization(this);
        this.scopeCipher = new VwCipher(this);
        this.scopePublic = new VwPublic(this);
    }

    public Uri BaseUri { get; }

    public TimeSpan Timeout
    {
        get => this.http.Timeout;
        set => this.http.Timeout = value;
    }

    public IVwUtility Utility => this.scopeUtility;
    public IVwIdentity Identity => this.scopeIdentity;
    public IVwAdmin Admin => this.scopeAdmin;
    public IVwUser User => this.scopeUser;
    public IVwOrganization Organization => this.scopeOrg;
    public IVwCipher Cipher => this.scopeCipher;
    public IVwPublic Public => this.scopePublic;

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

    private class VwScopeBase(VaultwardenHelper outer) : IVwScope
    {
        Uri IVwScope.BaseUri => outer.BaseUri;
        JsonSerializerOptions IVwScope.SerializeOptions => outer.apiSerializeOptions;
        HttpClient IVwScope.Http => outer.http;
    }

    private class VmUtility(VaultwardenHelper outer) : VwScopeBase(outer), IVwUtility;
    private class VwIdentity(VaultwardenHelper outer) : VwScopeBase(outer), IVwIdentity;
    private class VwAdmin(VaultwardenHelper outer) : VwScopeBase(outer), IVwAdmin;
    private class VwUser(VaultwardenHelper outer) : VwScopeBase(outer), IVwUser;
    private class VwOrganization(VaultwardenHelper outer) : VwScopeBase(outer), IVwOrganization;
    private class VwCipher(VaultwardenHelper outer) : VwScopeBase(outer), IVwCipher;
    private class VwPublic(VaultwardenHelper outer) : VwScopeBase(outer), IVwPublic;

    private HttpClient http;
    private bool disposed;
    private readonly JsonSerializerOptions apiSerializeOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, };

    private IVwUtility scopeUtility;
    private IVwIdentity scopeIdentity;
    private IVwAdmin scopeAdmin;
    private IVwUser scopeUser;
    private IVwOrganization scopeOrg;
    private IVwCipher scopeCipher;
    private IVwPublic scopePublic;
}

public interface IVwScope
{
    internal Uri BaseUri { get; }
    internal JsonSerializerOptions SerializeOptions { get; }
    internal HttpClient Http { get; }

    internal HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, ConnectTokenResult? token, HttpContent? content = default)
    {
        var apiEndpoint = new Uri(this.BaseUri, endpoint);
        var message = new HttpRequestMessage(method, apiEndpoint);
        if (token != null) message.Headers.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
        if (content != null) message.Content = content;
        return message;
    }

    internal HttpRequestMessage CreateJsonRequest<TData>(HttpMethod method, string endpoint, ConnectTokenResult? token, TData? data = default)
    {
        var content = default(HttpContent);
        if (data != null) content = JsonContent.Create(data, options: this.SerializeOptions);
        return CreateRequest(method, endpoint, token, content);
    }

    internal HttpRequestMessage CreateUrlEncodedRequest<TData>(HttpMethod method, string endpoint, ConnectTokenResult? token, TData? data = default)
    {
        var content = default(HttpContent);
        if (data != null) content = FormUrlEncoded.CreateContent(data);
        return CreateRequest(method, endpoint, token, content);
    }
}

public interface IVwUtility : IVwScope
{
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

    public byte[] CreatePasswordHash(string email, string password, KdfConfig config)
    {
        if (config.kdf != KdfType.Pbkdf2) throw new NotSupportedException();
        var passBytes = Encoding.UTF8.GetBytes(password);
        var mailBytes = Encoding.UTF8.GetBytes(email);
        var masterKey = Rfc2898DeriveBytes.Pbkdf2(passBytes, mailBytes, config.kdfIterations, HashAlgorithmName.SHA256, 32);
        var passHash = Rfc2898DeriveBytes.Pbkdf2(masterKey, passBytes, 1, HashAlgorithmName.SHA256, 32);
        return passHash;
    }

    public byte[] CreateMasterKey(string email, string password, KdfConfig config)
    {
        if (config.kdf != KdfType.Pbkdf2) throw new NotSupportedException();
        var passBytes = Encoding.UTF8.GetBytes(password);
        var mailBytes = Encoding.UTF8.GetBytes(email);
        var masterKey = Rfc2898DeriveBytes.Pbkdf2(passBytes, mailBytes, config.kdfIterations, HashAlgorithmName.SHA256, 32);
        return masterKey;
    }

    public byte[] CreateExpandKey(byte[] key, string info)
    {
        var writer = new ArrayBufferWriter<byte>(info.Length + 1);
        Encoding.UTF8.GetBytes(info, writer);
        writer.Write<byte>([1]);
        return HMACSHA256.HashData(key, writer.WrittenSpan.ToArray());
    }

    public SymmetricCryptoKey CreateStretchKey(string email, string password, KdfConfig config)
    {
        var masterKey = this.CreateMasterKey(email, password, config);
        var stretchKey = this.CreateStretchKey(masterKey);
        return stretchKey;
    }

    public SymmetricCryptoKey CreateStretchKey(byte[] key)
    {
        var encKey = CreateExpandKey(key, "enc");
        var macKey = CreateExpandKey(key, "mac");
        var stretchKey = new SymmetricCryptoKey(EncryptionType.AesCbc256_HmacSha256, encKey, macKey);
        return stretchKey;
    }

    public byte[] EncryptPublicKey(byte[] publicKey, byte[] data)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
        var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
        return encrypted;
    }

    public byte[] DecryptPrivateKey(byte[] privateKey, byte[] data)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);
        var decrypted = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
        return decrypted;
    }

    public EncryptedData EncryptAes(SymmetricCryptoKey key, byte[] data, bool hmac, byte[]? iv = default)
    {
        static byte[] makeMac(byte[] key, byte[] iv, byte[] data)
        {
            byte[] macData = [.. iv, .. data];
            var mac = HMACSHA256.HashData(key, macData);
            return mac;
        }

        using var aes = Aes.Create();
        aes.Key = key.EncKey;
        if (iv == null) aes.GenerateIV(); else aes.IV = iv;
        var enc = aes.EncryptCbc(data, aes.IV);
        if (hmac)
        {
            var type = key.EncKey.Length switch { 16 => EncryptionType.AesCbc128_HmacSha256, 32 => EncryptionType.AesCbc256_HmacSha256, _ => throw new NotSupportedException(), };
            var mac = makeMac(key.AuthKey ?? [], aes.IV, enc);
            return new(type, enc, aes.IV, mac);
        }
        else
        {
            var type = key.EncKey.Length switch { 64 => EncryptionType.AesCbc256, _ => throw new NotSupportedException(), };
            return new(type, enc, aes.IV);
        }
    }

    public EncryptedData EncryptRsa(byte[] key, byte[] data, bool sha256 = true, byte[]? iv = default)
    {
        using (var rsa = RSA.Create())
        {
            rsa.ImportSubjectPublicKeyInfo(key, out _);
            var (type, padding) = sha256 ? (EncryptionType.Rsa2048_OaepSha256, RSAEncryptionPadding.OaepSHA256) : (EncryptionType.Rsa2048_OaepSha1, RSAEncryptionPadding.OaepSHA1);
            var enc = rsa.Encrypt(data, padding);
            return new(type, enc);
        }
    }

    public byte[] Decrypt(byte[] key, EncryptedData encripted)
    {
        switch (encripted.Type)
        {
            case EncryptionType.AesCbc256:
            case EncryptionType.AesCbc128_HmacSha256:
            case EncryptionType.AesCbc256_HmacSha256:
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    return aes.DecryptCbc(encripted.Data, encripted.IV!);
                }
            case EncryptionType.Rsa2048_OaepSha1:
            case EncryptionType.Rsa2048_OaepSha1_HmacSha256:
                using (var rsa = RSA.Create())
                {
                    rsa.ImportPkcs8PrivateKey(key, out _);
                    return rsa.Decrypt(encripted.Data, RSAEncryptionPadding.OaepSHA1);
                }
            case EncryptionType.Rsa2048_OaepSha256:
            case EncryptionType.Rsa2048_OaepSha256_HmacSha256:
                using (var rsa = RSA.Create())
                {
                    rsa.ImportPkcs8PrivateKey(key, out _);
                    return rsa.Decrypt(encripted.Data, RSAEncryptionPadding.OaepSHA256);
                }
            default:
                throw new InvalidDataException();
        }
    }
}

public interface IVwIdentity : IVwScope
{
    public async Task<ConnectTokenResult> ConnectTokenAsync<TToken>(TToken data, CancellationToken cancelToken = default) where TToken : ConnectTokenModel
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ConnectTokenResult>() ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<ClientCredentialsConnectTokenResult> ConnectTokenAsync(ClientCredentialsConnectTokenModel data, CancellationToken cancelToken = default)
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ClientCredentialsConnectTokenResult>() ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<PasswordConnectTokenResult> ConnectTokenAsync(PasswordConnectTokenModel data, CancellationToken cancelToken = default)
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PasswordConnectTokenResult>() ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<PreloginResult> PreloginAsync(PreloginArgs args, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, "identity/accounts/prelogin", default, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PreloginResult>() ?? throw new Exception("failed to get result");
        return result;
    }
}

public interface IVwAdmin : IVwScope
{
    public async Task<AdminToken> GetTokenAsync(string password, CancellationToken cancelToken = default)
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "admin", default, new { token = password, });
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) throw new Exception("failed to get token");
        var token = default(AdminToken);
        foreach (var cookie in cookies)
        {
            var scan = cookie.AsSpan();
            while (!scan.IsEmpty)
            {
                var entry = scan.TakeSkipToken(out scan, delimiter: ';');
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

    public async Task<VwUser> InviteAsync(AdminToken token, string email, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, "admin/invite", default, new { email, });
        request.Headers.Add("Cookie", [$"VW_ADMIN={token.token}"]);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUser>(cancelToken) ?? throw new Exception("failed to request");
        return result;
    }
}

public interface IVwUser : IVwScope
{
    public async Task<VwUser> GetProfile(ConnectTokenResult token, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/accounts/profile", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUser>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwApiKey> GetApiKey(ConnectTokenResult token, PasswordOrOtp args, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/accounts/api-key", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwApiKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<UserPublicKey> GetPublicKey(ConnectTokenResult token, string userId, CancellationToken cancelToken = default)
    {
        var encUserId = Uri.EscapeDataString(userId);
        using var request = CreateRequest(HttpMethod.Get, $"api/users/{encUserId}/public-key", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<UserPublicKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }
}

public interface IVwOrganization : IVwScope
{
    public async Task<OrgPublicKey> GetPublicKey(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/public-key", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgPublicKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwApiKey> GetApiKey(ConnectTokenResult token, string orgId, PasswordOrOtp args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/api-key", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwApiKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwOrganization> GetDetail(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwOrganization>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgMembersResult> Create(ConnectTokenResult token, CreateOrgArgs? args = default, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task ConfirmMember(ConnectTokenResult token, string orgId, string memberId, ConfirmMemberArgs args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/users/{encMemberId}/confirm", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }

    public async Task<OrgMembersResult> GetMembers(ConnectTokenResult token, string orgId, OrgMembersArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateUrlEncodedRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/users", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgMembersUser> GetMember(ConnectTokenResult token, string orgId, string memberId, OrgMembersArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/users/{encMemberId}", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersUser>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task EditMember(ConnectTokenResult token, string orgId, string memberId, EditOrgMemberArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/users/{encMemberId}", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }

    public async Task<OrgCollectionsResult> GetCollections(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/collections", token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgCollectionsResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgCollectionDetail> CreateCollection(ConnectTokenResult token, string orgId, CreateCollectionArgs? args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/collections", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgCollectionDetail>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }
}

public interface IVwCipher : IVwScope
{
    public async Task<CipherItemsResult> GetItems(ConnectTokenResult token, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/ciphers", token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CipherItemsResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<CipherItem> GetItem(ConnectTokenResult token, string id, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/ciphers/{id}", token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CipherItem>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }
}

public interface IVwPublic : IVwScope
{
    public async Task ImportOrgMembersAsync(ConnectTokenResult token, ImportOrgArgs data, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, "api/public/organization/import", token, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }
}

