﻿namespace Dauer.Model.Data;

public class NullDatabaseAdapter : HasProperties, IDatabaseAdapter
{
  public bool Ready => true;

  public Task<bool> InsertAsync(Authorization t) => Task.FromResult(true);
  public Task DeleteAsync(Authorization t) => Task.CompletedTask;
  public Task<Authorization> GetAuthorizationAsync(string id) => Task.FromResult(new Authorization { Id = id });

  public Task<bool> InsertAsync(MapTile t) => Task.FromResult(true);
  public Task DeleteAsync(MapTile t) => Task.CompletedTask;
  public Task<MapTile> GetMapTileAsync(string id) => Task.FromResult(new MapTile { Id = id });

  public Task<bool> InsertAsync(DauerActivity t) => Task.FromResult(true);
  public Task DeleteAsync(DauerActivity t) => Task.CompletedTask;
  public Task<DauerActivity> GetActivityAsync(string id) => Task.FromResult(new DauerActivity { Id = id });

  public Task DeleteAsync(BlobFile t) => Task.CompletedTask;
  public Task<List<BlobFile>> GetAllAsync() => Task.FromResult(new List<BlobFile>());
  public Task<bool> InsertAsync(BlobFile t) => Task.FromResult(true);
  public Task UpdateAsync(BlobFile t) => Task.CompletedTask;

}
