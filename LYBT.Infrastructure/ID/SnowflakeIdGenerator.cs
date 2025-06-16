using System;

namespace LYBT.Infrastructure {
    /// <summary>
    /// 雪花算法实现唯一 ID 生成器
    /// </summary>
    public interface ISnowflakeIdGenerator {
        long GenerateId();
        string GenerateIdString();
    }

    public class SnowflakeIdGenerator : ISnowflakeIdGenerator {
        private static readonly object _lock = new();
        private long _lastTimestamp = -1L;
        private long _sequence = 0L;

        private const long Twepoch = 1288834974657L;
        private const int WorkerIdBits = 5;
        private const int DatacenterIdBits = 5;
        private const int SequenceBits = 12;
        private const long MaxSequence = -1L ^ (-1L << SequenceBits);

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

        private readonly long _workerId = 1L;
        private readonly long _datacenterId = 1L;

        public long GenerateId() {
            lock (_lock) {
                var timestamp = GetCurrentTimestamp();

                if (timestamp < _lastTimestamp)
                    throw new Exception("时间回拨异常");

                if (_lastTimestamp == timestamp) {
                    _sequence = (_sequence + 1) & MaxSequence;
                    if (_sequence == 0)
                        timestamp = WaitNextMillis(_lastTimestamp);
                } else {
                    _sequence = 0;
                }

                _lastTimestamp = timestamp;

                return ((timestamp - Twepoch) << TimestampLeftShift) |
                       (_datacenterId << DatacenterIdShift) |
                       (_workerId << WorkerIdShift) |
                       _sequence;
            }
        }

        public string GenerateIdString() => GenerateId().ToString();

        private long GetCurrentTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private long WaitNextMillis(long lastTimestamp) {
            var timestamp = GetCurrentTimestamp();
            while (timestamp <= lastTimestamp) {
                timestamp = GetCurrentTimestamp();
            }
            return timestamp;
        }
    }
}
