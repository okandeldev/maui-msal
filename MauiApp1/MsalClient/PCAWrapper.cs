// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace MauiApp1.MsalClient;

/// <summary>
/// This is a wrapper for PCA. It is singleton and can be utilized by both application and the MAM callback
/// </summary>
public class PCAWrapper
{
    /// <summary>
    /// This is the singleton used by consumers
    /// </summary>
    public static PCAWrapper Instance { get; private set; } = new PCAWrapper();

    internal IPublicClientApplication PCA { get; }

    internal bool UseEmbedded { get; set; } = false;

    internal const string ClientId = "e9a9a6aa-c9c0-47af-a107-6fa9ef2df118";
    internal const string TenantId = "2e40e8c8-e681-4fe4-a867-983ba7eba8db";
    internal const string Authority = $"https://login.microsoftonline.com/{TenantId}";
    public static string[] Scopes = { $"api://{ClientId}/access_as_user" };

    // private constructor for singleton
    private PCAWrapper()
    {
        // Create PCA once. Make sure that all the config parameters below are passed
        PCA = PublicClientApplicationBuilder
                                    .Create(ClientId)
                                    .WithRedirectUri(PlatformConfig.Instance.RedirectUri)
                                    .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
                                    .Build();


    //    // Configure the token cache
    //    PCA.UserTokenCache.SetBeforeAccess(args =>
    //    {
    //        // Read the cache data from wherever it is persisted (e.g., file, database)
    //        byte[] cacheData = // Read cache data

    //// If cache data exists, load it into the token cache
    //if (cacheData != null && cacheData.Length > 0)
    //        {
    //            args.TokenCache.DeserializeMsalV3(cacheData);
    //        }
    //    });
    }

    /// <summary>
    /// Acquire the token silently
    /// </summary>
    /// <param name="scopes">desired scopes</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes)
    {
        var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
        var acct = accts.FirstOrDefault();

        var authResult = await PCA.AcquireTokenSilent(scopes, acct)
                                    .ExecuteAsync().ConfigureAwait(false);
        return authResult;

    }

    /// <summary>
    /// Perform the interactive acquisition of the token for the given scope
    /// </summary>
    /// <param name="scopes">desired scopes</param>
    /// <returns></returns>
    internal async Task<AuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes)
    {

#if IOS
        // Hide the privacy prompt in iOS
        var systemWebViewOptions = new SystemWebViewOptions();
        systemWebViewOptions.iOSHidePrivacyPrompt = true;

        return await PCA.AcquireTokenInteractive(scopes)
                                .WithAuthority(Authority)
                                .WithTenantId(TenantId)
                                .WithParentActivityOrWindow(PlatformConfig.Instance.ParentWindow)
                                .WithUseEmbeddedWebView(UseEmbedded)
                                .WithSystemWebViewOptions(systemWebViewOptions)
                                .ExecuteAsync()
                                .ConfigureAwait(false);
#else
        return await PCA.AcquireTokenInteractive(scopes)
                                .WithAuthority(Authority)
                                .WithTenantId(TenantId)
                                .WithParentActivityOrWindow(PlatformConfig.Instance.ParentWindow)
                                .WithUseEmbeddedWebView(true)
                                .ExecuteAsync()
                                .ConfigureAwait(false);
#endif

        throw new Exception("Platform not supported.");
    }

    /// <summary>
    /// Signout may not perform the complete signout as company portal may hold
    /// the token.
    /// </summary>
    /// <returns></returns>
    internal async Task SignOutAsync()
    {
        var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
        foreach (var acct in accounts)
        {
            await PCA.RemoveAsync(acct).ConfigureAwait(false);
        }
    }
}