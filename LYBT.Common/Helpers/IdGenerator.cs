namespace LYBT.Infrastructure.Utilities {

    /// <summary>
    /// 基于 Twitter Snowflake 算法的分布式唯一 ID 生成器
    /// </summary>
    public class SnowflakeIdGenerator {
        private readonly object _lock = new object();
        private readonly long _workerId;
        private readonly long _datacenterId;
        private long _sequence = 0L;

        private const long Twepoch = 1288834974657L;
        private const int WorkerIdBits = 5;
        private const int DatacenterIdBits = 5;
        private const int SequenceBits = 12;

        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        private long _lastTimestamp = -1L;

        public SnowflakeIdGenerator(long workerId, long datacenterId) {
            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException($"workerId must be between 0 and {MaxWorkerId}");

            if (datacenterId > MaxDatacenterId || datacenterId < 0)
                throw new ArgumentException($"datacenterId must be between 0 and {MaxDatacenterId}");

            _workerId = workerId;
            _datacenterId = datacenterId;
        }

        /// <summary>
        /// 获取下一个唯一 ID
        /// </summary>
        public long NextId() {
            lock (_lock) {
                long timestamp = TimeGen();

                if (timestamp < _lastTimestamp) {
                    throw new InvalidOperationException("系统时钟回拨，拒绝生成 ID");
                }

                if (_lastTimestamp == timestamp) {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0) {
                        timestamp = TilNextMillis(_lastTimestamp);
                    }
                } else {
                    _sequence = 0L;
                }

                _lastTimestamp = timestamp;

                return ((timestamp - Twepoch) << TimestampLeftShift)
                     | (_datacenterId << DatacenterIdShift)
                     | (_workerId << WorkerIdShift)
                     | _sequence;
            }
        }

        /// <summary>
        /// 等待到下一毫秒
        /// </summary>
        private long TilNextMillis(long lastTimestamp) {
            long timestamp = TimeGen();
            while (timestamp <= lastTimestamp) {
                timestamp = TimeGen();
            }
            return timestamp;
        }

        /// <summary>
        /// 获取当前时间戳（毫秒）
        /// </summary>
        private long TimeGen() {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}