﻿#nullable enable

using System.Runtime.CompilerServices;
using Dauer.Model.Extensions;

namespace Dauer.Model;

public static class CookieMapper
{
  public static System.Net.Cookie MapSystemCookie(this Cookie c) => new()
  {
    Name = c.Name,
    Value = c.Value,
    Domain = c.Domain,
    Path = c.Path,
    HttpOnly = c.HttpOnly,
    Secure = c.IsSecure,
    Expires = c.Expires,
  };

  public static Cookie MapModel(this System.Net.Cookie c) => new()
  {
    Name = c.Name,
    Value = c.Value,
    Domain = c.Domain,
    Path = c.Path,
    HttpOnly = c.HttpOnly,
    IsSecure = c.Secure,
    Expires = c.Expires,
  };
  
  public static System.Net.CookieContainer MapCookieContainer(this Dictionary<string, Cookie> cookies)
  { 
    var cookieContainer = new System.Net.CookieContainer();
    if (cookies == null) { return cookieContainer; }

    foreach (var cookie in cookies.Values)
    {
      cookieContainer.Add(cookie.MapSystemCookie());
    }

    return cookieContainer;
  }

  public static Dictionary<string, Cookie> MapModel(this System.Net.CookieContainer cookies) => cookies
    .GetAllCookies()
    .Select(c => c.MapModel())
    .ToDictionaryAllowDuplicateKeys(c => c.Name, c => c);
}
