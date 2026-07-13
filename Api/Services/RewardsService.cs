using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;
    private static int count = 0;

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral =rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void CalculateRewards(User user)
    {
        count++;

        // Copier les collections pour éviter les modifications concurrentes
        // Liste des positions où l'utilisateur a été
        List<VisitedLocation> userLocations = user.VisitedLocations.ToList();
        // Liste des attractions disponibles
        List<Attraction> attractions = _gpsUtil.GetAttractions().ToList();

        // Comparer chaque position visitée avec chaque attraction
        foreach (var visitedLocation in userLocations)
        {
            foreach (var attraction in attractions)
            {
                // Vérifier si l'utilisateur n'a pas déjà reçu de récompense pour cette attraction
                if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                {
                    // Vérifier si l'utilisateur est proche de l'attraction
                    if (NearAttraction(visitedLocation, attraction))
                    {
                        // Ajouter une récompense pour l'utilisateur
                        user.AddUserReward(new UserReward(visitedLocation, attraction, GetRewardPoints(attraction, user)));
                    }
                }
            }
        }
    }

    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
        Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    private int GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
    }

    public double GetDistance(Locations loc1, Locations loc2)
    {
        // Conversion des degrés aux radians
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        // Formule du cosinus sphérique pour calculer la distance entre deux points sur une sphère
        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        // Conversion de l'angle en milles nautiques, puis en milles terrestres
        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        return StatuteMilesPerNauticalMile * nauticalMiles;
    }
}
