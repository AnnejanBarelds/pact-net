namespace PactNet
{
    internal interface IPactConfig
    {
        string PactDir { get; set; }
        string LogDir { get; set; }
        string LoggerName { get; set; }
    }
}
