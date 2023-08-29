﻿using System.Reactive.Linq;
using System.Reflection;
using Dauer.Model;
using Dauer.Services;
using Dauer.Ui.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{
  IMapViewModel Map { get; }
  bool IsPortrait { get; set; }
}

public class DesignMainViewModel : MainViewModel
{
  public DesignMainViewModel() : base(
    new NullWindowAdapter(),
    new DesignPlotViewModel(),
    new DesignLapViewModel(),
    new DesignRecordViewModel(),
    new DesignMapViewModel(),
    new DesignFileViewModel(),
    new DesignLogViewModel(),
    new DesignSettingsViewModel(),
    new NullFitEditService()
  )
  { 
  }
}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }
  public IFileViewModel File { get; }
  public ILogViewModel LogVm { get; }
  public ISettingsViewModel Settings { get; }
  public IFitEditService FitEdit { get; set; }

  private string? AppTitle_ => $"FitEdit | Training Data Editor | Version {Version} {Titlebar.Instance.Message}";
  [Reactive] public string? AppTitle { get; set; }

  public string? Version { get; set; }

  [Reactive] public int SelectedTabIndex { get; set; }

  // We presume a portrait display because Android and iOS don't
  // call window_.Resized at startup but Windows does
  [Reactive] public bool IsPortrait { get; set; } = true; 

  private readonly IWindowAdapter window_;

  public MainViewModel(
    IWindowAdapter window,
    IPlotViewModel plot,
    ILapViewModel laps,
    IRecordViewModel records,
    IMapViewModel map,
    IFileViewModel file,
    ILogViewModel logVm,
    ISettingsViewModel settings,
    IFitEditService fitEdit
  )
  {
    window_ = window;
    Plot = plot;
    Laps = laps;
    Records = records;
    Map = map;
    File = file;
    LogVm = logVm;
    Settings = settings;
    FitEdit = fitEdit;

    GetVersion();

    Titlebar.Instance.ObservableForProperty(x => x.Message).Subscribe(_ => AppTitle = AppTitle_);
    window_.Resized.Subscribe(tup =>
    {
      double width = tup.Item1;
      double height = tup.Item2;
      IsPortrait = width < height;
      Log.Info($"Window resized to {width} {height}");
    });
  }

  /// <summary>
  /// Get the assembly version that is displayed in the titlebar and update the titlebar with it
  /// </summary>
  private void GetVersion()
  {
    var assembly = Assembly.GetAssembly(typeof(App));
    var attr = assembly?.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
    Version = attr?.InformationalVersion ?? "Unknown Version";
    AppTitle = AppTitle_;
  }
}
