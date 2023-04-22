﻿using Dauer.Model;
using Dauer.Model.Units;
using Dauer.Model.Workouts;
using Dynastream.Fit;
using Newtonsoft.Json;

namespace Dauer.Data.Fit
{
  public static class FitFileExtensions
  {
    /// <summary>
    /// Fill modified Session, Records, Laps, etc, into Events
    /// </summary>
    public static FitFile BackfillEvents(this FitFile f, int resolution = 100, Action<int, int> handleProgress = null)
    {
      handleProgress ??= (_, _) => { };

      int li = 0;
      int ri = 0;
      int si = 0;

      int i = 0;

      var sessions = f.Sessions;
      var laps = f.Laps;
      var records = f.Records;
      var events = f.Events.OfType<MesgEventArgs>().ToList();

      foreach (MesgEventArgs e in events)
      {
        if (i % resolution == 0)
        {
          handleProgress(i, f.Events.Count);
        }
        i++;

        if (!MessageFactory.Types.TryGetValue(e.mesg.Num, out Type t))
        {
          continue;
        }

        if (t == typeof(SessionMesg))
        {
          e.mesg = sessions[si++];
        }
        else if (t == typeof(LapMesg))
        {
          e.mesg = laps[li++];
        }
        else if (t == typeof(RecordMesg))
        {
          e.mesg = records[ri++];
        }
      }

      return f;
    }

    public static float? TotalDistance(this IEnumerable<SessionMesg> sessions) => sessions.Sum(sess => sess.GetTotalDistance());
    public static float? TotalElapsedTime(this IEnumerable<SessionMesg> sessions) => sessions.Sum(sess => sess.GetTotalElapsedTime());

    /// <summary>
    /// Return a one-line description of a fit file
    /// </summary>
    public static string OneLine(this FitFile f) => f.Sessions.Count switch
    {
      1 => $"From {f.Sessions[0].Start()} to {f.Sessions[0].End()}: {f.Sessions[0].GetTotalDistance()} m in {f.Sessions[0].GetTotalElapsedTime()}s ({f.Sessions[0].GetEnhancedAvgSpeed():0.##} m/s)",
      _ when f.Sessions.Count > 1 => $"From {f.Sessions.First().Start()} to {f.Sessions.Last().End()}: {f.Sessions.TotalDistance()} m in {f.Sessions.TotalElapsedTime()}s",
      _ => "No sessions",
    };

    /// <summary>
    /// Pretty-print useful information from a fit file: Session, Laps, and Records
    /// </summary>
    public static FitFile Print(this FitFile f, Action<string> print, bool showRecords)
    {
      if (f == null)
      {
        return f;
      }

      var sessions = f.Sessions;
      var laps = f.Laps;
      var records = f.Records;

      print($"Fit File: ");
      print($"  {records.Count} {(laps.Count == 1 ? "record" : "records")}");
      print($"  {sessions.Count} {(sessions.Count == 1 ? "session" : "sessions")}:");

      foreach (var sess in sessions)
      {
        print($"    From {sess.Start()} to {sess.End()}: {sess.GetTotalDistance()} m in {sess.GetTotalElapsedTime()}s ({sess.GetEnhancedAvgSpeed():0.##} m/s)");
      }

      print($"  {laps.Count} {(laps.Count == 1 ? "lap" : "laps")}:");
      foreach (var lap in laps)
      {
        print($"    From {lap.Start()} to {lap.End()}: {lap.GetTotalDistance()} m in {lap.GetTotalElapsedTime()}s ({lap.GetEnhancedAvgSpeed():0.##} m/s)");

        var lapRecords = records.Where(rec => rec.Start() > lap.Start() && rec.Start() < lap.End())
                                .ToList();

        print($"      {lapRecords.Count} {(lapRecords.Count == 1 ? "record" : "records")}");

        if (!showRecords)
        {
          continue;
        }

        foreach (var rec in lapRecords)
        {
          var speed = new Speed { Unit = SpeedUnit.MetersPerSecond, Value = (double)rec.GetEnhancedSpeed() };
          var distance = new Distance { Unit = DistanceUnit.Meter, Value = (double)rec.GetDistance() };

          // Print the fractional part of the given number as
          // seconds of a minute e.g. 8.9557 => 8:57
          string pretty(double minPerMile)
          {
            if (minPerMile == double.PositiveInfinity || minPerMile == double.NegativeInfinity)
            {
              return "0:00";
            }

            int floor = (int)Math.Floor(minPerMile);
            return $"{floor}:{(int)((minPerMile - floor)*60):00}";
          }

          print($"        At {rec.Start():HH:mm:ss}: {distance.Miles():0.##} mi, {pretty(speed.MinutesPerMile())} min/mi, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
          //print($"        At {rec.Start():HH:mm:ss}: {rec.GetDistance():0.##} m, {rec.GetEnhancedSpeed():0.##} m/s, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
        }
      }

      return f;
    }

    /// <summary>
    /// Pretty-print everything in the given FIT file.
    /// </summary>
    public static string PrintAll(this FitFile f) => JsonConvert.SerializeObject(f, Formatting.Indented);

    /// <summary>
    /// Recalculate the workout as if each lap was run at the corresponding constant speed.
    /// Return the same modified FitFile.
    /// </summary>
    public static FitFile ApplySpeeds(this FitFile fitFile, List<Speed> speeds)
    {
      var laps = fitFile.Get<LapMesg>();
      var records = fitFile.Get<RecordMesg>();
      var sessions = fitFile.Get<SessionMesg>();

      if (laps.Count != speeds.Count)
      {
        throw new ArgumentException($"Found {laps.Count} laps but {speeds.Count} speeds");
      }

      if (!records.Any())
      {
        throw new ArgumentException($"Could not find any records");
      }

      if (!sessions.Any())
      {
        throw new ArgumentException($"Could not find any sessions");
      }

      foreach (int i in Enumerable.Range(0, laps.Count))
      {
        laps[i].Apply(speeds[i]);
      }

      var distance = new Distance { Unit = DistanceUnit.Meter };

      System.DateTime lastTimestamp = records.First().Start();

      foreach (RecordMesg record in records)
      {
        LapMesg lap = record.FindLap(laps);

        int j = laps.IndexOf(lap);

        double speed = speeds[j].MetersPerSecond();

        System.DateTime timestamp = record.Start();
        double elapsedSeconds = (timestamp - lastTimestamp).TotalSeconds;
        lastTimestamp = timestamp;

        distance.Value += speed * elapsedSeconds;

        lap.SetTotalDistance((float)distance.Meters());
        record.SetDistance((float)distance.Meters());
        record.SetEnhancedSpeed((float)speed);
      }

      SessionMesg session = sessions.FirstOrDefault();
      session?.Apply(distance, speeds.MaxBy(s => s.Value));

      return fitFile;
    }
  }
}