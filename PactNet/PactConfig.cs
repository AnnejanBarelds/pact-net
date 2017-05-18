namespace PactNet
{
    public class PactConfig: IPactConfig
    {
        public string PactDir { get; set; }
        public string LogDir { get; set; }

        string IPactConfig.LoggerName { get; set; }

        public PactConfig()
        {
            PactDir = Constants.DefaultPactDir;
            LogDir = Constants.DefaultLogDir;
        }
    }
}