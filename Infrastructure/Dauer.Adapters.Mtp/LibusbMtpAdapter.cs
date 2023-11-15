﻿using Dauer.Model;
using Dauer.Model.Mtp;
using Nmtp;

namespace Dauer.Adapters.Mtp;

public class LibUsbMtpAdapter : IMtpAdapter
{
  public LibUsbMtpAdapter()
  {
    _ = Task.Run(Scan);
  }

  public void Scan()
  {
    const ushort GARMIN = 0x091e;
    var deviceList = new RawDeviceList();
    var garminDevices = deviceList.Where(d => d.DeviceEntry.VendorId == GARMIN).ToList();
    foreach (RawDevice rawDevice in garminDevices)
    {
      using var device = new Device();
      RawDevice rd = rawDevice;
      if (!device.TryOpen(ref rd, cached: true)) { continue; }

      Log.Info($"Found Garmin device {device.GetModelName() ?? "(unknown)"}");
      Log.Info($"Found device serial # {device.GetSerialNumber() ?? "unknown"}");

      IEnumerable<Nmtp.DeviceStorage> storages = device.GetStorages();

      foreach (var storage in storages)
      {
        IEnumerable<Nmtp.Folder> folders = device.GetFolderList(storage.Id);
        var activityFolder = folders.FirstOrDefault(folder => folder.Name == "Activity");

        if (activityFolder.FolderId <= 0) { continue; }

        List<Nmtp.File> files = device
          .GetFiles(progress =>
          {
            Log.Info($"List files progress: {progress * 100:##.#}%");
            return true;
          })
          .Where(file => file.ParentId == activityFolder.FolderId)
          .Where(file => file.FileName.EndsWith(".fit"))
          .Where(file => DateTime.UnixEpoch + TimeSpan.FromSeconds(file.ModificationDate) > DateTime.UtcNow - TimeSpan.FromDays(7))
          .ToList();

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit-Data", "MTP");
        Directory.CreateDirectory(dir);

        foreach (Nmtp.File file in files)
        {
          Console.WriteLine($"Found file {file.FileName}");

          using var fs = new FileStream($"{dir}/{file.FileName}", FileMode.Create);
          device.GetFile(file.ItemId, progress =>
          {
            Log.Info($"Download progress {file.FileName} {progress * 100:##.#}%");
            return false;
          }, fs);
        }
      }
    }
  }
}