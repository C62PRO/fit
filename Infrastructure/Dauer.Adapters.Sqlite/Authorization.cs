﻿using SQLite;

namespace Dauer.Adapters.Sqlite;
#nullable enable

public class Authorization
{
  [PrimaryKey]
  public string? Id { get; set; }
  public string? AccessToken { get; set; }
  public string? RefreshToken { get; set; }
  public string? IdentityToken { get; set; }
  public DateTimeOffset Expiry { get; set; }

  public string? Username { get; set; }
}