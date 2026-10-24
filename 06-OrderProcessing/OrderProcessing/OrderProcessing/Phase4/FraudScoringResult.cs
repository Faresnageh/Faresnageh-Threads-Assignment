namespace OrderProcessing.Phase4
{
    public class FraudScoringResult
    {
        public int FlaggedCount { get; set; }
        public List<int> FlaggedIds { get; set; } = new();
        public long SequentialMs { get; set; }
        public long ParallelMs { get; set; }
        public double SpeedUp { get; set; }
        public int DistinctThreads { get; set; }
        public int ProcessorCount { get; set; }
    }
}