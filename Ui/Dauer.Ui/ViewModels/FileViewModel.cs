﻿using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Interop;
using Avalonia.Threading;
using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Ui.Extensions;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Storage;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileViewModel
{
}

public class DesignFileViewModel : FileViewModel
{
  public DesignFileViewModel() : base(
    new FileService(),
    new NullDatabaseAdapter(),
    new NullStorageAdapter(),
    new NullWebAuthenticator(),
    new DesignLogViewModel()) { }
}

public class FileViewModel : ViewModelBase, IFileViewModel
{
  [Reactive] public int SelectedIndex { get; set; }

  public IFileService FileService { get; }
  private readonly IDatabaseAdapter db_;
  private readonly IStorageAdapter storage_;
  private readonly IWebAuthenticator auth_;
  private readonly ILogViewModel log_;

  public FileViewModel(
    IFileService fileService,
    IDatabaseAdapter db,
    IStorageAdapter storage,
    IWebAuthenticator auth,
    ILogViewModel log
  )
  {
    FileService = fileService;
    db_ = db;
    storage_ = storage;
    auth_ = auth;
    log_ = log;

    InitFilesList();
  }

  private void InitFilesList()
  {
    _ = Task.Run(async () =>
    {
      List<BlobFile> files = await db_.GetAllAsync().AnyContext();

      var sfs = files.Select(file => new SelectedFile
      {
        FitFile = null, // Don't parse the blobs, that would be too slow
        Blob = file,
      }).ToList();

      await Dispatcher.UIThread.InvokeAsync(() => FileService.Files.AddRange(sfs));

      foreach (var sf in sfs)
      {
        sf.SubscribeToIsLoaded(LoadOrUnload);
      }
    });
  }

  public async void HandleSelectFileClicked()
  {
    Log.Info("Select file clicked");

    // On macOS and iOS, the file picker must run on the main thread
    BlobFile? file = await storage_.OpenFileAsync();

    if (file == null)
    {
      Log.Info("No file selected in the file dialog");
      return;
    }

    SelectedFile sf = await Task.Run(async () =>
    {
      bool ok = await db_.InsertAsync(file).AnyContext();

      if (ok) { Log.Info($"Persisted file {file}"); }
      else { Log.Error($"Could not persist file {file}"); }

      var sf = new SelectedFile { Blob = file };
      sf.SubscribeToIsLoaded(LoadOrUnload);
      return sf;
    });

    FileService.Files.Add(sf);
  }

  public async void HandleForgetFileClicked()
  {
    int index = SelectedIndex;
    if (index < 0 || FileService.Files.Count == 0)
    {
      Log.Info("No file selected; cannot forget file");
      return;
    }

    var file = FileService.Files[index];
    if (file == null) { return; }

    await db_.DeleteAsync(file.Blob);
    FileService.Files.Remove(file);
    InitFilesList();

    SelectedIndex = Math.Min(index, FileService.Files.Count);
  }

  private void LoadOrUnload(SelectedFile sf)
  {
    if (sf.IsLoaded)
    {
      _ = Task.Run(async () => await LoadFile(sf).AnyContext());
    }
    else
    {
      UnloadFile(sf);
    }
  }

  private void UnloadFile(SelectedFile? sf)
  {
    if (sf == null) { return; }
    sf.Progress = 0;
    sf.FitFile = null;
  }

  private async Task LoadFile(SelectedFile? sf)
  {
    if (sf == null || sf.Blob == null)
    {
      Log.Info("Could not load null file");
      return;
    }

    if (sf.FitFile != null) 
    {
      Log.Info($"File {sf.Blob.Name} is already loaded");
      return;
    }

    BlobFile file = sf.Blob;

    try
    {
      Log.Info($"Got file {file.Name} ({file.Bytes.Length} bytes)");

      // Handle FIT files
      string extension = Path.GetExtension(file.Name);

      if (extension.ToLower() != ".fit")
      {
        Log.Info($"Unsupported extension {extension}");
        return;
      }

      using var ms = new MemoryStream(file.Bytes);
      await log_.Log($"Reading FIT file {file.Name}");

      var reader = new Reader();
      if (!reader.TryGetDecoder(file.Name, ms, out FitFile fit, out var decoder))
      {
        return;
      }

      long lastPosition = 0;
      long resolution = 5 * 1024; // report progress every 5 kB

      // Instead of reading all FIT messages at once,
      // Read just a few FIT messages at a time so that other tasks can run on the main thread e.g. in WASM
      sf.Progress = 0;
      while (await reader.ReadOneAsync(ms, decoder, 100))
      {
        if (ms.Position - resolution > lastPosition)
        {
          continue;
        }

        double progress = (double)ms.Position / ms.Length * 100;
        sf.Progress = progress;
        await TaskUtil.MaybeYield();
        lastPosition = ms.Position;
      }

      fit.ForwardfillEvents();

      // Do on the main thread because there are subscribers which update the UI
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        sf.FitFile = fit;
        FileService.FitFile = fit;
      });

      sf.Progress = 100;
      await log_.Log($"Done reading FIT file");
      Log.Info(fit.Print(showRecords: false));
    }
    catch (Exception e)
    {
      Log.Error($"{e}");
      return;
    }
  }

  public async void HandleDownloadFilesClicked()
  {
    Log.Info("Download files clicked...");
    foreach (var file in FileService.Files)
    {
      await DownloadFile(file);
    }
  }

  private async Task DownloadFile(SelectedFile file)
  { 
    if (file?.Blob == null) { return; }

    try
    {
      var ms = new MemoryStream();
      new Writer().Write(file.FitFile, ms);
      byte[] bytes = ms.ToArray();

      string name = Path.GetFileNameWithoutExtension(file.Blob.Name);
      string extension = Path.GetExtension(file.Blob.Name);
      // On macOS and iOS, the file save dialog must run on the main thread
      await storage_.SaveAsync(new BlobFile($"{name}_edit.{extension}", bytes));
    }
    catch (Exception e)
    {
      Log.Info($"{e}");
    }
  }

  public void HandleAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleAuthorizeClicked)}");
    Log.Info($"Starting {auth_.GetType()}.{nameof(IWebAuthenticator.AuthenticateAsync)}");

    auth_.AuthenticateAsync();
  }
}