namespace ServiceBusLibrary.Services;

public sealed class ProcessErrorEventArgs : EventArgs
{
    public required Exception Exception { get; set; }
    public required string EntityPath { get; set; }
    public required string ErrorSource { get; set; }
    public required string FullyQualifiedNamespace { get; set; }
}
