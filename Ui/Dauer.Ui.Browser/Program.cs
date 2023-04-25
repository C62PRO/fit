﻿using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Dauer.Ui;
using Dauer.Ui.Adapters;
using Dauer.Ui.Adapters.Storage;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using Dauer.Fuse.Secure;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
  [RequiresUnreferencedCode("Calls Dauer.Fuse.Fuse.Init(String)")]
  private static void Main(string[] args)
  {
    _ = JSHost
      .ImportAsync(WebStorageAdapterImpl.ModuleName, "./store.js")
      .ContinueWith(_ =>
      {
        WebConsoleAdapter.Log($"{WebStorageAdapterImpl.ModuleName} ready");
        string key = "testKey";
        WebStorageAdapterImpl.SetLocalStorage(key, "{ \"jsonKey\" : \"jsonValue\" }");
        string data = WebStorageAdapterImpl.GetLocalStorage(key);
        WebConsoleAdapter.Log($"Got from storage: {key} => {data}");
      });

    _ = JSHost
      .ImportAsync(WebConsoleAdapter.ModuleName, "./console.js")
      .ContinueWith(_ =>
      {
        WebConsoleAdapter.Log($"{WebConsoleAdapter.ModuleName} ready");
        WebConsoleAdapter.SetMessage();
      });

    try
    {
      AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
      {
        return args.Name.StartsWith("Dauer")
          ? Defuse.Redirect(args.Name, "/Dauer.Fuse.dll")
          : null;
      };
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
    }

    BuildAvaloniaApp()
      .UseReactiveUI()
      .SetupBrowserApp("out");
  }

  [RequiresUnreferencedCode("Calls System.Reflection.Assembly.LoadFile(String)")]
  private static Assembly? Redirect(object? sender, ResolveEventArgs args)
  {
    if (!args.Name.StartsWith("Dauer")) return null;
    string dll = "/Dauer.Fuse.dll";
    Console.WriteLine($"Redirecting {args.Name} => {dll}");
    try
    {
      var assem = Assembly.LoadFile(dll);
      Console.WriteLine($"Got assembly: {assem.FullName}");
      return assem;
    }
    catch (Exception e)
    {
      Console.WriteLine($"{e}");
      return null;
    }
  }

  public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .LogToTrace(LogEventLevel.Debug);
}