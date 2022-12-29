namespace ActiveCampaign {
    public class ConcurrentLimitedTimedAccessResourceGuard : IDisposable {
        public ConcurrentLimitedTimedAccessResourceGuard( int maxConcurrentAccesses, int accessLifeTimeMilliseconds ) {
            _semaphore = new SemaphoreSlim( maxConcurrentAccesses );
            _accessLifeTimeMilliseconds = accessLifeTimeMilliseconds;
        }

        public async Task WaitAsync( CancellationToken cancellationToken = default ) {
            await _semaphore.WaitAsync( cancellationToken );

            ScheduleSemaphoreRelease( );
        }

        async void ScheduleSemaphoreRelease( ) {
            var task = Task.Run( async ( ) => {
                await Task.Delay( _accessLifeTimeMilliseconds );
                _semaphore.Release( );
            } );
            
            _scheduledReleaseTasks.Add( task );

            await task;

            _scheduledReleaseTasks.Remove( task );
        }

        async void IDisposable.Dispose( ) {
            await Task.WhenAll( _scheduledReleaseTasks.ToArray( ) );

            _semaphore.Dispose( );
        }

        int _accessLifeTimeMilliseconds;
        SemaphoreSlim _semaphore;
        List< Task > _scheduledReleaseTasks = new List< Task >( );
    }
}