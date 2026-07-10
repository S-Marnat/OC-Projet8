using GpsUtil.Location;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using RewardCentral;
using System.Diagnostics;
using System.Globalization;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TourGuide.Utilities;
using TripPricer;

namespace TourGuide.Services;

public class TourGuideService : ITourGuideService
{
    private readonly ILogger _logger;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardsService _rewardsService;
    private readonly IRewardCentral _rewardsCentral;
    private readonly TripPricer.TripPricer _tripPricer;
    public Tracker Tracker { get; private set; }
    private readonly Dictionary<string, User> _internalUserMap = new();
    private const string TripPricerApiKey = "test-server-api-key";
    private bool _testMode = true;

    public TourGuideService(ILogger<TourGuideService> logger, IGpsUtil gpsUtil, IRewardsService rewardsService, IRewardCentral rewardCentral, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _tripPricer = new();
        _gpsUtil = gpsUtil;
        _rewardsService = rewardsService;
        _rewardsCentral = rewardCentral;

        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        if (_testMode)
        {
            _logger.LogInformation("TestMode enabled");
            _logger.LogDebug("Initializing users");
            InitializeInternalUsers();
            _logger.LogDebug("Finished initializing users");
        }

        var trackerLogger = loggerFactory.CreateLogger<Tracker>();

        Tracker = new Tracker(this, trackerLogger);
        AddShutDownHook();
    }

    public List<UserReward> GetUserRewards(User user)
    {
        return user.UserRewards;
    }

    public VisitedLocation GetUserLocation(User user)
    {
        return user.VisitedLocations.Any() ? user.GetLastVisitedLocation() : TrackUserLocation(user);
    }

    public User GetUser(string userName)
    {
        return _internalUserMap.ContainsKey(userName) ? _internalUserMap[userName] : null;
    }

    public List<User> GetAllUsers()
    {
        return _internalUserMap.Values.ToList();
    }

    public void AddUser(User user)
    {
        if (!_internalUserMap.ContainsKey(user.UserName))
        {
            _internalUserMap.Add(user.UserName, user);
        }
    }

    public List<Provider> GetTripDeals(User user)
    {
        int cumulativeRewardPoints = user.UserRewards.Sum(i => i.RewardPoints);

        var providers = new List<Provider>();

        // Retourner 10 providers = TripPricer ne pouvant en retourner que 5, on duplique la récupération des providers pour en avoir 10
        for (var i = 0; i < 2; i++)
        {
            providers.AddRange(_tripPricer.GetPrice(TripPricerApiKey, user.UserId,
                user.UserPreferences.NumberOfAdults, user.UserPreferences.NumberOfChildren,
                user.UserPreferences.TripDuration, cumulativeRewardPoints));
        }

        user.TripDeals = providers;
        return providers;
    }

    public VisitedLocation TrackUserLocation(User user)
    {
        VisitedLocation visitedLocation = _gpsUtil.GetUserLocation(user.UserId);
        user.AddToVisitedLocations(visitedLocation);
        _rewardsService.CalculateRewards(user);
        return visitedLocation;
    }

    public List<Attraction> GetNearByAttractions(VisitedLocation visitedLocation)
    {
        // Récupérer la distance entre la position de l'utilisateur et les attractions
        var allAttractions = new List<Object[]>();

        foreach (var attraction in _gpsUtil.GetAttractions())
        {
            double distance = _rewardsService.GetDistance(visitedLocation.Location, attraction);

            allAttractions.Add(new object[]
            {
                attraction,
                distance
            });
        }

        // Trier les attractions par distance et prendre les 5 plus proches
        var tri = allAttractions.OrderBy(aa => (double)aa[1]).Take(5).ToList();

        // Renvoyer la liste des 5 attractions les plus proches
        List<Attraction> nearbyAttractions = new();
        foreach (var attraction in tri)
        {
            nearbyAttractions.Add((Attraction)attraction[0]);
        }

        return nearbyAttractions;
    }

    public object CreateAttractionJsonObject(List<Attraction> attractions, VisitedLocation visitedLocation)
    {
        var jsonObject = new List<Object[]>();

        foreach (var attraction in attractions)
        {
            double distance = _rewardsService.GetDistance(visitedLocation.Location, attraction);
            int pointsRecompense = _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, visitedLocation.UserId);

            jsonObject.Add(new object[]
            {
                $"Attraction : {attraction.AttractionName}",
                $"Coordonnées de l'attraction : {attraction.Latitude}/{attraction.Longitude}",
                $"Coordonnées de l'utilisateur : {visitedLocation.Location.Latitude}/{visitedLocation.Location.Longitude}",
                $"Distance entre l'emplacement de l'utilisateur et l'attraction : {distance} miles",
                $"Points de récompense : {pointsRecompense} points"
            });
        };

        return jsonObject;
    }

    private void AddShutDownHook()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Tracker.StopTracking();
    }

    /**********************************************************************************
    * 
    * Methods Below: For Internal Testing
    * 
    **********************************************************************************/

    private void InitializeInternalUsers()
    {
        for (int i = 0; i < InternalTestHelper.GetInternalUserNumber(); i++)
        {
            var userName = $"internalUser{i}";
            var user = new User(Guid.NewGuid(), userName, "000", $"{userName}@tourGuide.com");
            GenerateUserLocationHistory(user);
            _internalUserMap.Add(userName, user);
        }

        _logger.LogDebug($"Created {InternalTestHelper.GetInternalUserNumber()} internal test users.");
    }

    private void GenerateUserLocationHistory(User user)
    {
        for (int i = 0; i < 3; i++)
        {
            var visitedLocation = new VisitedLocation(user.UserId, new Locations(GenerateRandomLatitude(), GenerateRandomLongitude()), GetRandomTime());
            user.AddToVisitedLocations(visitedLocation);
        }
    }

    private static readonly Random random = new Random();

    private double GenerateRandomLongitude()
    {
        return new Random().NextDouble() * (180 - (-180)) + (-180);
    }

    private double GenerateRandomLatitude()
    {
        return new Random().NextDouble() * (90 - (-90)) + (-90);
    }

    private DateTime GetRandomTime()
    {
        return DateTime.UtcNow.AddDays(-new Random().Next(30));
    }
}
