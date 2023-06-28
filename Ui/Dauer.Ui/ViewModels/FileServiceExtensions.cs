﻿using System.Reactive.Linq;
using DynamicData.Binding;
using System.Collections.Specialized;
using ReactiveUI;

namespace Dauer.Ui.ViewModels;

public static class FileServiceExtensions
{
  public static IDisposable SubscribeAdds(this IFileService fs, Action<SelectedFile> handle) =>
    fs.Files.ObserveCollectionChanges().Subscribe(x =>
     {
       if (x.EventArgs.Action != NotifyCollectionChangedAction.Add) { return; }
       if (x?.EventArgs?.NewItems == null) { return; }

       foreach (var file in x.EventArgs.NewItems.OfType<SelectedFile>())
       {
         file.SubscribeToFitFile(handle);
       }
     });

  public static IDisposable SubscribeRemoves(this IFileService fs, Action<SelectedFile> handle) =>
    fs.Files.ObserveCollectionChanges().Subscribe(x =>
     {
       if (x.EventArgs.Action != NotifyCollectionChangedAction.Remove) { return; }
       if (x?.EventArgs?.OldItems == null) { return; }

       foreach (var file in x.EventArgs.OldItems.OfType<SelectedFile>())
       {
         handle(file);
       }
     });
}

public static class SelectedFileExtensions
{ 
  public static IDisposable SubscribeToFitFile(this SelectedFile file, Action<SelectedFile> handle) =>
    file.ObservableForProperty(x => x.FitFile).Subscribe(property => handle(property.Sender));

  public static IDisposable SubscribeToIsLoaded(this SelectedFile file, Action<SelectedFile> handle) =>
    file.ObservableForProperty(x => x.IsVisible).Subscribe(property => handle(property.Sender));
}
