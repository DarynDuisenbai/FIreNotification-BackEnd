namespace Application.Service.BackService
{
    public class CoordinationService : BackgroundService
    {
        private readonly NasaFirmsService _nasaFirmsService;
        private readonly FireDataCleanupService _cleanupService;
        private readonly SatelliteImageService _imageService;
        private readonly ILogger<CoordinationService> _logger;

        public CoordinationService(
            NasaFirmsService nasaFirmsService,
            FireDataCleanupService cleanupService,
            SatelliteImageService imageService,
            ILogger<CoordinationService> logger)
        {
            _nasaFirmsService = nasaFirmsService;
            _cleanupService = cleanupService;
            _imageService = imageService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _nasaFirmsService.ProcessFireData();
                    await _cleanupService.RemoveDuplicates();
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в CoordinationService");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}
