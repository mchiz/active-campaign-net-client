using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ActiveCampaign {
    public class ConcurrentLimitedTimedAccessResourceGuard : IDisposable {
        public ConcurrentLimitedTimedAccessResourceGuard( int maxConcurrentAccesses, int accessLifeTimeMilliseconds ) {
            _semaphore = new SemaphoreSlim( 1 );
            _accessLifeTimeMilliseconds = accessLifeTimeMilliseconds;

            _lastQueriesTimeStamps = new DateTime[ maxConcurrentAccesses ];
        }

        public async Task WaitAsync( CancellationToken cancellationToken = default ) {
            try {
                await _semaphore.WaitAsync( cancellationToken );

                if( _lastQueriesTimeStampsCount > 0 && _lastQueriesTimeStampsCount == _maxConcurrentAccesses ) {
                    var oldestTimeStamp = _lastQueriesTimeStamps[ 0 ];
                    var diff = DateTime.Now - oldestTimeStamp;
                    long milliseconds = diff.Ticks / TimeSpan.TicksPerMillisecond;

                    if( milliseconds < _accessLifeTimeMilliseconds ) {
                        int remaining = _accessLifeTimeMilliseconds - ( int )milliseconds;

                        await Task.Delay( remaining );
                    }

                    RemoveOldestTimeStamp( );
                }

                AddTimeStamp( );

            } finally {
                _semaphore.Release( );

            }
        }

        void AddTimeStamp( ) {
            _lastQueriesTimeStamps[ _lastQueriesTimeStampsCount ] = DateTime.Now;

            ++_lastQueriesTimeStampsCount;
        }

        void RemoveOldestTimeStamp( ) {
            System.Array.Copy( _lastQueriesTimeStamps, 1, _lastQueriesTimeStamps, 0, _lastQueriesTimeStamps.Length - 1 );

            --_lastQueriesTimeStampsCount;
        }

        void IDisposable.Dispose( ) {
            _semaphore.Dispose( );
        }

        int _maxConcurrentAccesses { get => _lastQueriesTimeStamps.Length; }

        readonly int _accessLifeTimeMilliseconds;
        SemaphoreSlim _semaphore;
        DateTime [ ]_lastQueriesTimeStamps;
        int _lastQueriesTimeStampsCount = 0;
    }
}