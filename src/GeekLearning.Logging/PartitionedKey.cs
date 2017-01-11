namespace GeekLearning.Logging
{
    public class PartitionedKey
    {
        public PartitionedKey(string partition, string key)
        {
            this.Partition = partition;
            this.Key = key;
            this.Full = $"{this.Partition}{this.Key}";
        }

        public string Partition { get; }

        public string Key { get; }

        public string Full { get; }

        public override string ToString()
        {
            return this.Full;
        }

        public override bool Equals(object obj)
        {
            return this.Full == obj.ToString();
        }

        public override int GetHashCode()
        {
            return this.Full.GetHashCode();
        }
    }
}
