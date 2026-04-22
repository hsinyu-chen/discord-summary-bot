using Hcs.LightI18n;

namespace SummaryAndCheck.Services
{
    class GeminiMonitor : IDisposable
    {

        readonly CancellationTokenSource cts = new();
        bool running = false;

        int counter = 0;
        public Task Task { get; }
        public bool IsCancellationRequested => cts.IsCancellationRequested;
        public GeminiMonitor(Func<Func<string, Task>> getUpdater, string locale, CancellationToken cancellationToken)
        {
            var messagingCt = cts.Token;
            Task = Task.Run(async () =>
            {
                using var _ = L.GetScope(locale, "GeminiService");
                try
                {
                    while (!messagingCt.IsCancellationRequested)
                    {
                        if (running)
                        {
                            await Task.Delay(1000, messagingCt);
                            var updater = getUpdater();
                            if (updater != null && running)
                            {
                                await updater("Processing".T(new { waiting = ++counter }));
                            }
                        }
                        else
                        {
                            await Task.Delay(50, messagingCt);
                        }
                    }
                }
                catch
                {
                    //ignore cancel message
                }
            }, cancellationToken);
        }
        public void Reset()
        {
            counter = 0;
        }
        public void ToggleRunning(bool running)
        {
            this.running = running;
        }
        public void Stop()
        {
            cts.Cancel();
        }
        public void Dispose()
        {
            cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
