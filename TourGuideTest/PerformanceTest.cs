using GpsUtil.Location;
using System.Diagnostics;
using TourGuide.Users;
using Xunit.Abstractions;

namespace TourGuideTest
{
    public class PerformanceTest : IClassFixture<DependencyFixture>
    {
        private readonly DependencyFixture _fixture;

        private readonly ITestOutputHelper _output;

        public PerformanceTest(DependencyFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public async Task HighVolumeTrackLocationAsync()
        {
            //On peut ici augmenter le nombre d'utilisateurs pour tester les performances
            _fixture.Initialize(100);

            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();

            // Démarrer le chronomètre
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Paralléliser le suivi de la localisation pour chaque user
            var tasks = allUsers.Select(au => _fixture.TourGuideService.TrackUserLocationAsync(au));
            await Task.WhenAll(tasks);

            // Arrêter le chronomètre et le suivi de la localisation
            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            // Afficher le temps écoulé dans la sortie de test
            _output.WriteLine($"highVolumeTrackLocation: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            // Vérifier que le temps écoulé est inférieur ou égal à 15 minutes
            Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }

        [Fact]
        public async Task HighVolumeGetRewardsAsync()
        {
            //On peut ici augmenter le nombre d'utilisateurs pour tester les performances
            _fixture.Initialize(100);

            // Démarrer le chronomètre
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Récupérer une attraction
            var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
            Attraction attraction = attractions[0];

            // Récupérer les users
            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();

            // Ajouter une visite pour chaque user
            allUsers.ForEach(u => u.AddToVisitedLocations(new VisitedLocation(u.UserId, attraction, DateTime.Now)));

            // Paralléliser le calcul des récompenses pour chaque user
            var tasks = allUsers.Select(u => _fixture.RewardsService.CalculateRewardsAsync(u));
            await Task.WhenAll(tasks);

            // Vérifier que chaque user a au moins une récompense
            foreach (var user in allUsers)
            {
                Assert.True(user.UserRewards.Count > 0);
            }

            // Arrêter le chronomètre et le suivi de la localisation
            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            // Afficher le temps écoulé dans la sortie de test
            _output.WriteLine($"highVolumeGetRewards: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            // Vérifier que le temps écoulé est inférieur ou égal à 20 minutes
            Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }
    }
}
