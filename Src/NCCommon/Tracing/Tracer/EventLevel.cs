
namespace Alachisoft.NCache.Common.Tracing.Tracer
{
  
    public enum EventLevel {

        LogAlways = 0,    
        Critical = 1,   
        Error = 2, 
        Warning = 3,
        Informational = 4,
        Verbose = 5
    }


    public enum EventKeywords : long
    {
        All = -1,
        None = 0,
        MicrosoftTelemetry = 562949953421312,
        WdiContext = 562949953421312,
        WdiDiagnostic = 1125899906842624,
        Sqm = 2251799813685248,
        AuditFailure = 4503599627370496,
        CorrelationHint = 4503599627370496,
        AuditSuccess = 9007199254740992,
        EventLogClassic = 36028797018963968
    }
}