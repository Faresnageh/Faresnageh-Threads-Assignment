namespace OrderProcessing.Exceptions
{
    public class DataFileCorruptedException : Exception
    {
        public DataFileCorruptedException(string fileName) : base($"Corrupted JSON file: {fileName}")
        {
            
        }
    }
}
