namespace LeosAmazingAsynchrony
{
    public class AppSettings
    {
        public string FunctionsAppUrl { get; set; }
        public AppConnectionStrings ConnectionStrings { get; set; }

        public class AppConnectionStrings
        {
            public string StorageAccountConnectionString { get; set; }
        }
    }
}