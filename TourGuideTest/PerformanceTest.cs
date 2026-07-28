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

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var tasks = allUsers.Select(au => _fixture.TourGuideService.TrackUserLocationAsync(au));
            await Task.WhenAll(tasks);

            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeTrackLocation: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }

        [Fact]
        public async Task HighVolumeGetRewardsAsync()
        {
            //On peut ici augmenter le nombre d'utilisateurs pour tester les performances
            _fixture.Initialize(100);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
            Attraction attraction = attractions[0];

            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();

            allUsers.ForEach(u => u.AddToVisitedLocations(new VisitedLocation(u.UserId, attraction, DateTime.Now)));

            var tasks = allUsers.Select(u => _fixture.RewardsService.CalculateRewardsAsync(u));
            await Task.WhenAll(tasks);

            foreach (var user in allUsers)
            {
                Assert.True(user.UserRewards.Count > 0);
            }

            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeGetRewards: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }
    }
}
