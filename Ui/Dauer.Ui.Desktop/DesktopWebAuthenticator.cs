﻿using Dauer.Model.Data;
using Dauer.Ui.Desktop.Oidc;
using Dauer.Ui.Infra;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;
using ReactiveUI;
using Serilog;

namespace Dauer.Ui.Desktop;

public class DesktopWebAuthenticator : IWebAuthenticator
{
  private readonly string redirectUri_ = $"https://www.fitedit.io/login-redirect.html";
  private readonly string authority_ = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_nqQT8APwr";
  private readonly string api_ = "https://api.fitedit.io/";
  private readonly string clientId_ = "5n3lvp2jfo1c2kss375jvkhvod";
  private readonly IDatabaseAdapter db_;
  private readonly Dauer.Model.Authorization auth_ = new() { Id = "Dauer.Api" };
  private CancellationTokenSource refreshCts_ = new();

  private OidcClient? oidcClient_;
  private HttpClient? httpClient_;

  public DesktopWebAuthenticator(IDatabaseAdapter db)
  {
    _ = Task.Run(InitAsync);
    db_ = db;
    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ => await LoadCachedAuthorization());
  }

  private async Task LoadCachedAuthorization()
  {
    if (db_ == null) { return; }
    if (!db_.Ready) { return; }

    Dauer.Model.Authorization result = await db_.GetAuthorizationAsync(auth_.Id);
    if (result == null) { return; }

    auth_.AccessToken = result.AccessToken;
    auth_.RefreshToken = result.RefreshToken;
    auth_.Expiry = result.Expiry;
    await AuthenticateAsync();
  }

  private void InitAsync()
  {
    var browser = new DesktopBrowser();

    var options = new OidcClientOptions
    {
      Authority = authority_,
      ClientId = clientId_,
      RedirectUri = redirectUri_,
      Scope = "email openid",
      FilterClaims = false,
      Browser = browser,

      // JS on the redirect page will split the state on the .
      // to get the local listen port
      AdditionalState = $".port={browser.Port}"
    };
    options.Policy.Discovery.ValidateEndpoints = false;

    var serilog = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
        .CreateLogger();

    options.LoggerFactory.AddSerilog(serilog);

    oidcClient_ = new OidcClient(options);
    httpClient_ = new HttpClient { BaseAddress = new Uri(api_) };
  }

  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    // First try to refresh the token
    if (await RefreshTokenAsync(auth_.RefreshToken, ct)) { return true; }

    // Refresh token didn't work. We need to reuthenticate
    StopRefreshLoop();

    if (oidcClient_ == null) { return false; }
    LoginResult result = await oidcClient_.LoginAsync(cancellationToken: ct);

    if (!await TryParse(result)) { return false; }

    StartRefreshLoop();
    return true;
  }

  /// <summary>
  /// Stop the automatic refresh loop (no-op if it's not running)
  /// </summary>
  private void StopRefreshLoop()
  {
    refreshCts_.Cancel();
    refreshCts_ = new();
  }

  private void StartRefreshLoop()
  {
    CancellationToken refreshCt = refreshCts_.Token;
    _ = Task.Run(async () => await RunRefreshLoop(refreshCt), refreshCt);
  }

  /// <summary>
  /// Run a loop that refreshes the access token just before expiration.
  /// </summary>
  private async Task RunRefreshLoop(CancellationToken ct = default)
  {
    var margin = TimeSpan.FromMinutes(5); // e.g. refresh 5 minutes before expiry

    while (!ct.IsCancellationRequested)
    {
      // Run an initial check before delaying for a long time
      if (!await GetIsAuthenticated(auth_.AccessToken, ct)) { return; }

      TimeSpan delay = auth_.Expiry - DateTimeOffset.UtcNow - margin;
      await Task.Delay(delay, ct);

      // The token is almost expired. Are we still authenticated? We need it to refresh.
      // If not, reauthenticate and exit the loop. On reauth, a new loop will be started.
      if (!await GetIsAuthenticated(auth_.AccessToken, ct)) { await AuthenticateAsync(ct); return; }

      // Could we refresh the access token?
      // If not, reauthenticate and exit the loop. On reauth, a new loop will be started.
      if (!await RefreshTokenAsync(auth_.RefreshToken, ct)) { await AuthenticateAsync(ct); return; }
    }
  }

  private async Task<bool> RefreshTokenAsync(string? refreshToken, CancellationToken ct = default)
  {
    Dauer.Model.Log.Info($"{nameof(DesktopWebAuthenticator)}.{nameof(RefreshTokenAsync)}({refreshToken}, ...)");

    if (refreshToken == null) { return false; }
    if (oidcClient_ == null) { return false; }

    var result = await oidcClient_.RefreshTokenAsync(refreshToken, cancellationToken: ct);
    return await TryParse(result);
  }

  private async Task<bool> TryParse(LoginResult result)
  {
    if (result.IsError)
    {
      Dauer.Model.Log.Error($"Login error: {result.Error}: {result.ErrorDescription}");
      return false;
    }

    Dauer.Model.Log.Info($"LoginResult:");

    Dauer.Model.Log.Info("Claims:");
    foreach (var claim in result.User.Claims)
    {
      Dauer.Model.Log.Info($"{claim.Type}: {claim.Value}");
    }

    Dauer.Model.Log.Info($"Identity token: {result.IdentityToken}");
    Dauer.Model.Log.Info($"Access token:   {result.AccessToken}");
    Dauer.Model.Log.Info($"  Expires:  {result.AccessTokenExpiration}");
    Dauer.Model.Log.Info($"Refresh token:  {result.RefreshToken}");

    auth_.AccessToken = result.AccessToken;
    auth_.RefreshToken = result.RefreshToken;
    auth_.Expiry = result.AccessTokenExpiration;
    await db_.InsertAsync(auth_);

    return true;
  }

  private async Task<bool> TryParse(RefreshTokenResult result)
  {
    if (result.IsError)
    {
      Dauer.Model.Log.Error($"Refresh token error: {result.Error}: {result.ErrorDescription}");
      return false;
    }

    Dauer.Model.Log.Info($"RefreshTokenResult:");
    Dauer.Model.Log.Info($"Identity token: {result.IdentityToken}");
    Dauer.Model.Log.Info($"Access token:   {result.AccessToken}");
    Dauer.Model.Log.Info($"  Expires:  {result.AccessTokenExpiration}");
    Dauer.Model.Log.Info($"Refresh token:  {result.RefreshToken}");

    auth_.AccessToken = result.AccessToken;
    auth_.RefreshToken = result.RefreshToken;
    auth_.Expiry = result.AccessTokenExpiration;
    await db_.InsertAsync(auth_);

    return true;
  }

  private async Task<bool> GetIsAuthenticated(string? accessToken, CancellationToken ct = default)
  {
    if (accessToken == null) { return false; }
    if (httpClient_ == null) { return false; }

    httpClient_.SetBearerToken(accessToken);
    var response = await httpClient_.GetAsync("auth", cancellationToken: ct);

    if (response.IsSuccessStatusCode)
    {
      Dauer.Model.Log.Info("Successfully authenticated");
      return true;
    }

    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
      Dauer.Model.Log.Error($"Not authenticated: 401 Unauthorized");
      return false;
    }

    Dauer.Model.Log.Error($"Not authenticated: {response.ReasonPhrase}");
    return false;
  }
}