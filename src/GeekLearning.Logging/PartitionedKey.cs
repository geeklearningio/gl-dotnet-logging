namespace GeekLearning.Logging
{
    public class PartitionedKey
    {
        private string full;

        public PartitionedKey(string partition, string key)
        {
            this.Partition = partition;
            this.Key = key;
        }

        public string Partition { get; }

        public string Key { get; }

        public string Full
        {
            get
            {
                if (full == null)
                {
                    full = $"{Partition}{Key}";
                }

                return full;
            }
        }

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
