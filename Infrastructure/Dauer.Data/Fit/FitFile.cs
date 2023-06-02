using Dynastream.Fit;
using Dauer.Model.Extensions;

namespace Dauer.Data.Fit
{
  public class FitFile
  {
    public List<MesgDefinition> MessageDefinitions { get; set; } = new List<MesgDefinition>();
    public List<Mesg> Messages { get; set; } = new List<Mesg>();
    public List<EventArgs> Events { get; set; } = new List<EventArgs>();

    public List<SessionMesg> Sessions => Get<SessionMesg>();
    public List<LapMesg> Laps => Get<LapMesg>();

    private Lazy<List<RecordMesg>> records_;
    public List<RecordMesg> Records => records_.Value;

    public FitFile()
    {
      records_ = new(ComputeRecords);
    }

    /// <summary>
    /// Expensive; 2000 records take ~0.3s in WASM
    /// </summary>
    private List<RecordMesg> ComputeRecords() => Get<RecordMesg>().Sorted(MessageExtensions.Sort);

    public List<T> Get<T>() where T : Mesg => Messages
      .Where(message => message.Num == MessageFactory.MesgNums[typeof(T)])
      .Select(message => message as T)
      .ToList();

    public FitFile Clone() => new()
    {
      MessageDefinitions = MessageDefinitions.Select(x => new MesgDefinition(x)).ToList(),
      Messages = Messages.Select(MessageFactory.Create).ToList(),
      Events = Events.Select(x => x switch
      {
        _ when x is MesgEventArgs mea => (EventArgs)new MesgEventArgs(mea.mesg),
        _ when x is MesgDefinitionEventArgs mea => new MesgDefinitionEventArgs(mea.mesgDef),
        _ => null
      }).Where(x => x is not null).ToList(),
      records_ = records_,
    };
  }
}