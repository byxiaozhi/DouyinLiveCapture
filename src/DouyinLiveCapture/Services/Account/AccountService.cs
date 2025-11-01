using System.Text.Json;
using System.Text.Json.Serialization;
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Utilities;
using DouyinLiveCapture.Serialization;

namespace DouyinLiveCapture.Services.Account;

/// <summary>
/// 账号服务实现
/// </summary>
public class AccountService : IAccountService
{
    private readonly string _accountStoragePath;
    private readonly string _tokenStoragePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly HashSet<string> _supportedAccountPlatforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "sooplive", "flextv", "popkontv", "twitcasting"
    };

    public AccountService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "DouyinLiveCapture");
        _accountStoragePath = Path.Combine(appFolder, "accounts.json");
        _tokenStoragePath = Path.Combine(appFolder, "tokens.json");

        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // 确保存储目录存在
        Directory.CreateDirectory(appFolder);
    }

    public async Task<IReadOnlyList<AccountInfo>> GetAccountsAsync(string platform, string? username = null, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAccountsAsync(cancellationToken);
            var filtered = accounts.Where(a =>
                a.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(username) || a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            return filtered.AsReadOnly();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddAccountAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAccountsAsync(cancellationToken);

            // 检查是否已存在
            var existing = accounts.FirstOrDefault(a =>
                a.Platform.Equals(account.Platform, StringComparison.OrdinalIgnoreCase) &&
                a.Username.Equals(account.Username, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                throw new InvalidOperationException($"Account for {account.Platform}/{account.Username} already exists");
            }

            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            accounts.Add(account);
            await SaveAccountsAsync(accounts, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateAccountAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAccountsAsync(cancellationToken);

            var existing = accounts.FirstOrDefault(a =>
                a.Platform.Equals(account.Platform, StringComparison.OrdinalIgnoreCase) &&
                a.Username.Equals(account.Username, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                throw new InvalidOperationException($"Account for {account.Platform}/{account.Username} not found");
            }

            // 更新属性
            existing.Password = account.Password;
            existing.ExtraParams = account.ExtraParams;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsEnabled = account.IsEnabled;
            existing.Remarks = account.Remarks;

            await SaveAccountsAsync(accounts, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteAccountAsync(string platform, string username, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAccountsAsync(cancellationToken);
            var account = accounts.FirstOrDefault(a =>
                a.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase) &&
                a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (account != null)
            {
                accounts.Remove(account);
                await SaveAccountsAsync(accounts, cancellationToken);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<AccountInfo>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAccountsAsync(cancellationToken);
            return accounts.AsReadOnly();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAuthTokenAsync(AuthToken token, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var tokens = await LoadTokensAsync(cancellationToken);

            // 移除旧的令牌
            tokens.RemoveAll(t => t.Platform.Equals(token.Platform, StringComparison.OrdinalIgnoreCase));

            // 添加新令牌
            token.CreatedAt = DateTime.UtcNow;
            tokens.Add(token);

            await SaveTokensAsync(tokens, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AuthToken?> GetAuthTokenAsync(string platform, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var tokens = await LoadTokensAsync(cancellationToken);
            return tokens.FirstOrDefault(t =>
                t.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase) &&
                !t.IsExpired);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteAuthTokenAsync(string platform, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var tokens = await LoadTokensAsync(cancellationToken);
            var count = tokens.RemoveAll(t => t.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));

            if (count > 0)
            {
                await SaveTokensAsync(tokens, cancellationToken);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IReadOnlyList<string> GetSupportedAccountPlatforms()
    {
        return _supportedAccountPlatforms.ToList().AsReadOnly();
    }

    private async Task<List<AccountInfo>> LoadAccountsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_accountStoragePath))
            {
                return new List<AccountInfo>();
            }

            var json = await File.ReadAllTextAsync(_accountStoragePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<AccountInfo>();
            }

            var accounts = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListAccountInfo);
            return accounts ?? new List<AccountInfo>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load accounts from {_accountStoragePath}: {ex.Message}", ex);
        }
    }

    private async Task SaveAccountsAsync(List<AccountInfo> accounts, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(accounts, AppJsonSerializerContext.Default.ListAccountInfo);
            await File.WriteAllTextAsync(_accountStoragePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save accounts to {_accountStoragePath}: {ex.Message}", ex);
        }
    }

    private async Task<List<AuthToken>> LoadTokensAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_tokenStoragePath))
            {
                return new List<AuthToken>();
            }

            var json = await File.ReadAllTextAsync(_tokenStoragePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<AuthToken>();
            }

            var tokens = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.ListAuthToken);
            return tokens ?? new List<AuthToken>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load tokens from {_tokenStoragePath}: {ex.Message}", ex);
        }
    }

    private async Task SaveTokensAsync(List<AuthToken> tokens, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(tokens, AppJsonSerializerContext.Default.ListAuthToken);
            await File.WriteAllTextAsync(_tokenStoragePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save tokens to {_tokenStoragePath}: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}