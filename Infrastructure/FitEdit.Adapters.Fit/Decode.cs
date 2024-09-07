#region Copyright
/////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2023 Garmin International, Inc.
// Licensed under the Flexible and Interoperable Data Transfer (FIT) Protocol License; you
// may not use this file except in compliance with the Flexible and Interoperable Data
// Transfer (FIT) Protocol License.
/////////////////////////////////////////////////////////////////////////////////////////////
// ****WARNING****  This file is auto-generated!  Do NOT edit this file.
// Profile Version = 21.115Release
// Tag = production/release/21.115.00-0-gfe0a7f8
/////////////////////////////////////////////////////////////////////////////////////////////

#endregion

using FitEdit.Adapters.Fit;
using FitEdit.Model;
using FitEdit.Model.Extensions;

namespace Dynastream.Fit
{

  /// <summary>
  /// This class will decode a .fit file reading the file header and any definition or data messages.
  /// </summary>
  public class Decode
  {
    private const long CRCSIZE = 2;
    private const uint INVALID_DATA_SIZE = 0;

    private readonly MesgDefinition[] localMesgDefs_ = new MesgDefinition[Fit.MaxLocalMesgs];
    private Header fileHeader_;
    private uint timestamp_ = 0;
    private int lastTimeOffset_ = 0;
    private int lastLatitude_ = 0;
    private int lastLongitude_ = 0;
    private readonly Accumulator accumulator_ = new();
    private readonly DeveloperDataLookup lookup_ = new();

    public bool InvalidDataSize { get; set; } = false;

    public event MesgEventHandler MesgEvent;
    public event MesgDefinitionEventHandler MesgDefinitionEvent;
    public event EventHandler<DeveloperFieldDescriptionEventArgs> DeveloperFieldDescriptionEvent;

    /// <summary>
    /// Reads the file header to check if the file is FIT.
    /// Does not check CRC.
    /// Returns true if file is FIT.
    /// </summary>
    /// <param name="fitStream"> Seekable (file)stream to parse</param>
    public static bool IsFIT(Stream fitStream)
    {
      long position = fitStream.Position;
      bool status = false;
      try
      {
        // Does the header contain the flag string ".FIT"?
        var header = new Header(fitStream);
        fitStream.Position = position;
        status = header.IsValid();
      }
      // If the header is malformed the ctor could throw an exception
      catch (FitException)
      {
      }

      fitStream.Position = position;
      return status;
    }

    /// <summary>
    /// Reads the FIT binary file header and crc to check compatibility and integrity.
    /// Also checks data reords size.
    /// Returns true if file is ok (not corrupt).
    ///</summary>
    /// <param name="fitStream">Seekable (file)stream to parse.</param>
    public bool CheckIntegrity(Stream fitStream)
    {
      bool isValid = true;
      long position = fitStream.Position;

      try
      {
        while ((fitStream.Position < fitStream.Length) && isValid)
        {
          // Is there a valid header?
          var header = new Header(fitStream);
          isValid = header.IsValid();

          // Get the file size from the header
          // When the data size is 0 set flags, don't calculate CRC
          if (header.DataSize > INVALID_DATA_SIZE)
          {
            long fileSize = header.Size + header.DataSize + CRCSIZE;

            // Is the file CRC ok?
            // Need to rewind the header size because the header is part of the CRC calculation.
            byte[] data = new byte[fileSize];
            fitStream.Position -= header.Size;
            fitStream.Read(data, 0, data.Length);
            isValid &= CRC.Calc16(data, data.Length) == 0x0000;
          }
          else
          {
            InvalidDataSize = true;
            isValid = false;
          }
        }
      }
      catch (FitException)
      {
        isValid = false;
      }

      fitStream.Position = position;
      return isValid;
    }

    /// <summary>
    /// Reads a FIT binary file.
    /// </summary>
    /// <param name="fitStream">Seekable (file)stream to parse.</param>
    /// <returns>
    /// Returns true if reading finishes successfully.
    /// </returns>
    public async Task<bool> ReadAsync(Stream fitStream)
    {
      bool status = true;
      long position = fitStream.Position;

      while ((fitStream.Position < fitStream.Length) && status)
      {
        status = await ReadAsync(fitStream, DecodeMode.Normal);
      }

      fitStream.Position = position;

      return status;
    }


    /// <summary>
    /// Reads a FIT binary File
    /// </summary>
    /// <param name="fitStream">Seekable (file)stream to parse.</param>
    /// <param name="mode">Decode Mode to use for reading the file</param>
    /// <param name="messageCount">Maximum number of messages to read</param>
    /// <returns>
    /// Returns true if reading finishes successfully.
    /// </returns>
    public async Task<bool> ReadAsync(Stream fitStream, DecodeMode mode, int messageCount = -1)
    {
      if (messageCount < 0) messageCount = int.MaxValue;

      long fileSize = fitStream.Length;
      long filePosition = fitStream.Position;

      try
      {
        bool fileOk = true;

        // Attempt to read header
        if (filePosition == 0 && (mode == DecodeMode.Normal || mode == DecodeMode.Partial))
        {
          fileHeader_ = new Header(fitStream);
          fileOk &= fileHeader_.IsValid();

          // Get the file size from the header
          // When the data size is invalid set the file size to the fitstream length
          if (!InvalidDataSize)
          {
            fileSize = fileHeader_.Size + fileHeader_.DataSize + CRCSIZE;
          }

          if (!fileOk)
          {
            throw new FitException("FIT decode error: File is not FIT format. Check file header data type. Error at stream position: " + fitStream.Position);
          }
          if ((fileHeader_.ProtocolVersion & Fit.ProtocolVersionMajorMask) > (Fit.ProtocolMajorVersion << Fit.ProtocolVersionMajorShift))
          {
            // The decoder does not support decode accross protocol major revisions
            throw new FitException($"FIT decode error: Protocol Version {(fileHeader_.ProtocolVersion & Fit.ProtocolVersionMajorMask) >> Fit.ProtocolVersionMajorShift}.X not supported by SDK Protocol Ver{Fit.ProtocolMajorVersion}.{Fit.ProtocolMinorVersion} ");
          }
        }
        else if (mode == DecodeMode.InvalidHeader)
        {
          // When skipping the header force the stream position to be at the beginning of the data
          // Also the fileSize is the length of the filestream.
          fitStream.Position += Fit.HeaderWithCRCSize;
          fileSize = fitStream.Length;
        }
        else if (mode == DecodeMode.DataOnly)
        {
          // When the stream is only data move the position of the stream
          // to the start. FileSize is the length of the stream
          fitStream.Position = 0;
          fileSize = fitStream.Length;
        }
        else if (mode == DecodeMode.Partial)
        {
        }
        else
        {
          throw new FitException("Invalid Decode Mode Provided to read");
        }

        long end = fileSize - CRCSIZE;

        // Read n data messages and definitions
        if (mode == DecodeMode.Partial)
        {
          int count = 0;
          while (fitStream.Position < end && count < messageCount)
          {
            DecodeNextMessage(fitStream);
            count++;
          }
          return fitStream.Position < end;
        }

        // Read all data messages and definitions
        while (fitStream.Position < end)
        {
          DecodeNextMessage(fitStream);
        }

        // Is the file CRC ok?
        if ((mode == DecodeMode.Normal) && !InvalidDataSize)
        {
          byte[] data = new byte[fileSize];
          fitStream.Position = filePosition;
          await fitStream.ReadAsync(data, 0, data.Length);
          fileOk &= (CRC.Calc16(data, data.Length) == 0x0000);
          fitStream.Position = filePosition + fileSize;
        }

        return fileOk;
      }
      catch (EndOfStreamException e)
      {
        Log.Error(e.Message);
        return false; // Done reading
      }
      catch (FitException e)
      {
        Log.Error(e.Message);
        return true; // Attempt to keep reading
      }
    }

    public void DecodeNextMessage(Stream fitStream)
    {
      var br = new BinaryReader(fitStream);
      long sourceIndex = fitStream.Position;

      byte nextByte = br.ReadByte();
      Log.Debug($"Message header: {nextByte:X2}");

      bool isCompressedHeader = (nextByte & Fit.CompressedHeaderMask) == Fit.CompressedHeaderMask;
      bool isMesgDefinition = (nextByte & Fit.MesgDefinitionMask) == Fit.MesgDefinitionMask;
      bool isDataMessage = (nextByte & Fit.MesgDefinitionMask) == Fit.MesgHeaderMask;
      bool isDevData = (nextByte & Fit.DevDataMask) == Fit.DevDataMask;

      Log.Debug($"  Compressed: {isCompressedHeader}");
      Log.Debug($"  Definition: {isMesgDefinition}");
      Log.Debug($"  Data: {isDataMessage}");
      Log.Debug($"  DevData: {isDevData}");

      // Is it a compressed timestamp mesg?
      if (isCompressedHeader)
      {
        var mesgBuffer = new MemoryStream();

        int timeOffset = nextByte & Fit.CompressedTimeMask;
        timestamp_ += (uint)((timeOffset - lastTimeOffset_) & Fit.CompressedTimeMask);
        lastTimeOffset_ = timeOffset;
        Field timestampField = new Field(Profile.GetMesg(MesgNum.Record).GetField("Timestamp"));
        timestampField.SetValue(timestamp_);

        byte localMesgNum = (byte)((nextByte & Fit.CompressedLocalMesgNumMask) >> 5);
        mesgBuffer.WriteByte(localMesgNum);
        if (localMesgDefs_[localMesgNum] == null)
        {
          throw new FitException($"FIT decode error: Missing message definition for local message number {localMesgNum} at stream position {fitStream.Position}");
        }
        int fieldsSize = localMesgDefs_[localMesgNum].GetMesgSize() - 1;
        try
        {
          byte[] read = br.ReadBytes(fieldsSize);
          if (read.Length < fieldsSize)
          {
            throw new FitException($"Field size mismatch, expected: {fieldsSize}, received: {read.Length}");
          }
          mesgBuffer.Write(read, 0, fieldsSize);
        }
        catch (Exception e)
        {
          throw new FitException($"Compressed Data Message unexpected end of file. Wanted {fieldsSize} bytes at stream position {fitStream.Position}", e);
        }

        var mesg = new Mesg(mesgBuffer, localMesgDefs_[localMesgNum])
        {
          SourceIndex = sourceIndex
        };
        mesg.InsertField(0, timestampField);

        if (FitConfig.Discard.DataMessages.UnexpectedFileIdMessage 
          && mesg.Num == MesgNum.FileId && fitStream.Position > 1024)
        {
          Log.Warn($"Discarding suspicious FileID compressed header message.");
          fitStream.Position = FindNextMessageByTimestamp(timestamp_, fitStream);
          return;
        }

        RaiseMesgEvent(mesg);
        return;
      }

      // Is it a mesg def?
      if (isMesgDefinition)
      {
        var mesgDefBuffer = new MemoryStream();
        const int bytesPerField = 3;

        // Read record content
        // Fixed content
        byte reserved = br.ReadByte();
        byte architecture = br.ReadByte();

        // Suspicious: architecture is not 0 or 1
        if (FitConfig.Discard.DefinitionMessages.WithUnknownArchitecture && architecture != 0 && architecture != 1)
        {
          throw new FitException("FIT decode error: unsupported architecture " + architecture);
        }

        byte[] globalMesgNum = br.ReadBytes(2);
        byte numFields = br.ReadByte();

        // Variable content
        int numBytes = numFields * bytesPerField;
        byte[] read = br.ReadBytes(numBytes);

        if (read.Length < numBytes)
        {
          throw new FitException($"Message Definition size mismatch, expected: {numBytes}, received: {read.Length}");
        }

        mesgDefBuffer.WriteByte(nextByte);
        mesgDefBuffer.WriteByte(reserved);
        mesgDefBuffer.WriteByte(architecture);
        mesgDefBuffer.Write(globalMesgNum, 0, 2);
        mesgDefBuffer.WriteByte(numFields);
        mesgDefBuffer.Write(read, 0, numBytes);

        if (isDevData)
        {
          // Definition Contains Dev Data
          byte numDevFields = br.ReadByte();
          mesgDefBuffer.WriteByte(numDevFields);

          numBytes = numDevFields * bytesPerField;
          read = br.ReadBytes(numBytes);
          if (read.Length < numBytes)
          {
            throw new FitException($"Message Definition size mismatch, expected: {numBytes}, received: {read.Length}");
          }

          // Read Dev Data
          mesgDefBuffer.Write(read, 0, numBytes);
        }

        var def = new MesgDefinition(mesgDefBuffer, lookup_)
        {
          SourceIndex = sourceIndex,
        };

        if (FitConfig.Discard.DefinitionMessages.RedefiningGlobalMesgNum 
          && localMesgDefs_[def.LocalMesgNum] != null
          && localMesgDefs_[def.LocalMesgNum].GlobalMesgNum != def.GlobalMesgNum)
        {
          Log.Warn($"Discarding suspicious redefinition of local mesg def with different global mesg num");
          Log.Debug($"Source data: {string.Join(" ", def.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}");
          return;
        }

        if (FitConfig.Discard.DefinitionMessages.ContainingUnknownType 
          && !def.GetFields().Any(field => FitTypes.TypeMap.ContainsKey(field.Type)))
        {
          Log.Warn($"Discarding suspicious definition containing unknown types");
          Log.Debug($"Source data: {string.Join(" ", def.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}");
          return;
        }

        if (FitConfig.Discard.DefinitionMessages.WithBigUnknownMessageNum 
          && def.GlobalMesgNum > 500)
        {
          Log.Warn("Discarding suspicious definition containing big unknown mesg_num");
          Log.Debug($"Source data: {string.Join(" ", def.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}");
          return;
        }

        localMesgDefs_[def.LocalMesgNum] = def;
        MesgDefinitionEvent?.Invoke(this, new MesgDefinitionEventArgs(def));
        return;
      }

      // Is it a data mesg?
      if (isDataMessage)
      {
        var mesgBuffer = new MemoryStream();
        byte localMesgNum = (byte)(nextByte & Fit.LocalMesgNumMask);

        mesgBuffer.WriteByte(localMesgNum);
        if (localMesgDefs_[localMesgNum] == null)
        {
          throw new FitException($"FIT decode error: Missing message definition for local message number {localMesgNum} at stream position {fitStream.Position}");
        }

        MesgDefinition def = localMesgDefs_[localMesgNum];
        Log.Debug($"  (global, local) message num: ({localMesgNum}, {def.GlobalMesgNum})");
        int fieldsSize = def.GetMesgSize() - 1;

        if (FitConfig.Discard.DataMessages.OfLargeSize 
          && fieldsSize > 1024)
        {
          Log.Debug($"Discarding suspicious large data message with field size {fieldsSize}");
          return;
        }

        try
        {
          byte[] read = br.ReadBytes(fieldsSize);
          if (read.Length < fieldsSize)
          {
            throw new FitException("Field size mismatch, expected: " + fieldsSize + "received: " + read.Length);
          }
          mesgBuffer.Write(read, 0, fieldsSize);
        }
        catch (Exception e)
        {
          throw new FitException($"Data Message unexpected end of file.  Wanted {fieldsSize} bytes at stream position {fitStream.Position}", e);
        }

        var mesg = new Mesg(mesgBuffer, def)
        {
          SourceIndex = sourceIndex,
        };

        // If the new message contains a timestamp field, record the value to use as
        // a reference for compressed timestamp headers
        Field timestampField = mesg.GetField("Timestamp");
        Field latitudeField = mesg.GetField("PositionLat");
        Field longitudeField = mesg.GetField("PositionLong");

        bool haveTimestamp = timestampField != null && timestampField.Count > 0;
        bool haveLatitude = latitudeField != null && latitudeField.Count > 0;
        bool haveLongitude = longitudeField != null && longitudeField.Count > 0;

        if (FitConfig.Discard.DataMessages.WithLargeLatitudeChange 
          && haveLatitude)
        {
          // Detect big jump in latitude
          var lat = (int)latitudeField.GetValue();
          var diff = Math.Abs(lat - lastLatitude_);
          bool suspicious = lastLatitude_ != 0 && diff > GeospatialExtensions.DegreeToSemicircles;

          if (suspicious)
          {
            Log.Warn($"Discarding suspicious message with large latitude change of {diff.ToDegrees()}deg");
            Log.Debug($"Source data: {string.Join(" ", mesg.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}");
            return;
          }

          lastLatitude_ = lat;
        }

        if (FitConfig.Discard.DataMessages.WithLargeLongitudeChange 
          && haveLongitude)
        {
          // Detect big jump in longitude
          var lon = (int)longitudeField.GetValue();
          var diff = Math.Abs(lon - lastLongitude_);
          bool suspicious = lastLongitude_ != 0 && diff > GeospatialExtensions.DegreeToSemicircles;

          if (suspicious)
          {
            Log.Warn($"Discarding suspicious message with large longitude change of {diff.ToDegrees()}");
            Log.Debug($"Source data: {string.Join(" ", mesg.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}");
            return;
          }

          lastLongitude_ = lon;
        }

        if (FitConfig.Discard.DataMessages.WithLargeTimestampChange 
          && haveTimestamp && haveLongitude && haveLongitude)
        {
          var ts = (uint)timestampField.GetValue();
          var diff = Math.Abs((int)ts - (int)timestamp_);
          bool timeTraveled = timestamp_ > 0 && diff > TimeSpan.FromDays(1).TotalSeconds;

          if (timeTraveled)
          {
            var dt = new RecordMesg(mesg).GetTimestamp().GetDateTime();
            Log.Warn($"Discarding suspicious message at {dt} with timestamp change of {diff}s");
            Log.Debug($"Source data: {string.Join(" ", mesg.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}");
            return;
          }

          timestamp_ = ts;
          lastTimeOffset_ = (int)timestamp_ & Fit.CompressedTimeMask;
        }

        if (FitConfig.Discard.DataMessages.UnexpectedFileIdMessage
          && mesg is FileIdMesg && fitStream.Position > 1024)
        {
          Log.Warn($"Discarding suspicious FileID message.");
          fitStream.Position = FindNextMessageByTimestamp(timestamp_, fitStream);
          return;
        }

        foreach (var kvp in mesg.Fields)
        {
          Field field = kvp.Value;

          if (field.IsAccumulated)
          {
            int i;
            for (i = 0; i < field.GetNumValues(); i++)
            {
              long value = Convert.ToInt64(field.GetRawValue(i));

              foreach (var kvp2 in mesg.Fields)
              {
                Field fieldIn = kvp2.Value;
                foreach (var kvp3 in fieldIn.Components)
                {
                  FieldComponent fc = kvp3.Value;
                  if ((fc.fieldNum == field.Num) && (fc.accumulate))
                  {
                    value = (long)((((value / field.Scale) - field.Offset) + fc.offset) * fc.scale);
                  }
                }
              }
              accumulator_.Set(mesg.Num, field.Num, value);
            }
          }
        }

        // Now that the entire message is decoded we can evaluate subfields and expand any components
        mesg.ExpandComponents(accumulator_);

        RaiseMesgEvent(mesg);
        return;
      }

      throw new FitException("Decode:Read - FIT decode error: Unexpected Record Header Byte 0x" + nextByte.ToString("X") + " at stream position: " + fitStream.Position);
    }

    /// <summary>
    /// Find next message after the given timestamp. Return the stream position of that message.
    /// Return the unchanged stream position if no message is found.
    /// 
    /// <para/>
    /// Algorithm:
    /// Find the next occurence of a timestamp similar to the given one in the file.
    /// We define timestamp similarity as matching the 2 most significant bytes, 
    /// i.e. the last 2 of the 4 sequential bytes in the file,
    /// i.e. the two bytes that change the least frequently in a timestamp.
    /// Then we return the stream position of the start of the message that the found timestamp is a part of.
    ///   position = timestamp third byte position (of 4) - 2 (to rewind to the start of timestamp) - 1 (to rewind to the message header byte)
    private long FindNextMessageByTimestamp(uint timestamp, Stream fitStream)
    {
      Log.Info($"Seeking to next timestamp");

      long position = fitStream.Position;

      var buf = new byte[256];
      int count = fitStream.Read(buf, 0, 256);

      if (count <= 0)
      {
        return position;
      }

      // Extract the last two bytes from the uint
      byte byte1 = (byte)((timestamp) & 0xFF);
      byte byte2 = (byte)((timestamp >> 8) & 0xFF);
      byte byte3 = (byte)((timestamp >> 16) & 0xFF);
      byte byte4 = (byte)((timestamp >> 24) & 0xFF);

      int nextTimestamp = buf.FindNextOccurrence(new[] { byte3, byte4 }, 0);
      return position + nextTimestamp - 2 - 1;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="mesg"></param>
    /// <exception cref="System.InvalidOperationException"></exception>
    private void RaiseMesgEvent(Mesg mesg)
    {
      if ((mesg.Num == MesgNum.DeveloperDataId) ||
          (mesg.Num == MesgNum.FieldDescription))
      {
        HandleMetaData(mesg);
      }

      MesgEvent?.Invoke(this, new MesgEventArgs(mesg));
    }

    private void HandleMetaData(Mesg newMesg)
    {
      if (newMesg.Num == MesgNum.DeveloperDataId)
      {
        var mesg = new DeveloperDataIdMesg(newMesg);
        lookup_.Add(mesg);
      }
      else if (newMesg.Num == MesgNum.FieldDescription)
      {
        var mesg = new FieldDescriptionMesg(newMesg);
        DeveloperFieldDescription desc = lookup_.Add(mesg);
        if (desc != null)
        {
          OnDeveloperFieldDescriptionEvent(
              new DeveloperFieldDescriptionEventArgs(desc));
        }
      }
    }

    protected virtual void OnDeveloperFieldDescriptionEvent(DeveloperFieldDescriptionEventArgs e) 
      => DeveloperFieldDescriptionEvent?.Invoke(this, e);
  } // class
} // namespace
