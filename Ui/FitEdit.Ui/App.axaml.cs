using Avalonia;
using Avalonia.Markup.Xaml;
using FitEdit.Model;
using FitEdit.Ui.Infra;
using FitEdit.Ui.ViewModels;
using FitEdit.Ui.Views;

namespace FitEdit.Ui;

public class App : Application
{
  public static ICompositionRoot? Root { get; set; }
  public static Action<ICompositionRoot>? DidStart { get; set; }

  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
    StyleExtensions.LoadStyles();
    AppDomain.CurrentDomain.UnhandledException += HandleException;
  }

  private void HandleException(object sender, UnhandledExceptionEventArgs e)
  {
    Log.Debug($"Unhandled Exception: {e}");
  }

  public override void OnFrameworkInitializationCompleted()
  {
    if (Root is null) { return; }

    bool isDesktop = ApplicationLifetime.IsDesktop(out var desktop);
    bool isMobile = ApplicationLifetime.IsMobile(out var mobile);

    Root.RegisterModule(new FitEditModule(ApplicationLifetime, Root.Config));
    Root.RegisterModule(new UiModule(isMobile));

    object dataContext = Root
      .Build()
      .Get<IMainViewModel>();

    DataContext = dataContext;
    
    if (isDesktop)
    {
      desktop!.MainWindow = new MainWindow
      {
        DataContext = dataContext
      };
    }

    else if (isMobile)
    {
      mobile!.MainView = new MainView
      {
        DataContext = dataContext
      };
    }

    base.OnFrameworkInitializationCompleted();
    
    DidStart?.Invoke(Root);
  }
}