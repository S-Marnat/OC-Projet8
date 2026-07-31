using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TripPricer;

namespace TourGuide.Controllers;

[ApiController]
[Route("[controller]")]
public class TourGuideController : ControllerBase
{
    private readonly ITourGuideService _tourGuideService;

    public TourGuideController(ITourGuideService tourGuideService)
    {
        _tourGuideService = tourGuideService;
    }

    [HttpGet("getLocation")]
    public async Task<ActionResult<VisitedLocation>> GetLocationAsync([FromQuery] string userName)
    {
        var user = GetUser(userName);

        if (user == null)
        {
            return NotFound("Le nom renseigné ne correspond à aucun utilisateur.");
        }

        try
        {
            var location = await _tourGuideService.GetUserLocationAsync(user);
            return Ok(location);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Une erreur interne est survenue.");
        }
    }

    [HttpGet("getNearbyAttractions")]
    public async Task<ActionResult<List<Attraction>>> GetNearbyAttractionsAsync([FromQuery] string userName)
    {
        var user = GetUser(userName);

        if (user == null)
        {
            return NotFound("Le nom renseigné ne correspond à aucun utilisateur.");
        }

        try
        {
            var visitedLocation = await _tourGuideService.GetUserLocationAsync(user);
            var attractions = await _tourGuideService.GetNearByAttractionsAsync(visitedLocation);
            var json = await _tourGuideService.CreateAttractionJsonObjectAsync(attractions, visitedLocation);
            return Ok(json);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Une erreur interne est survenue.");
        }
    }

    [HttpGet("getRewards")]
    public ActionResult<List<UserReward>> GetRewards([FromQuery] string userName)
    {
        var user = GetUser(userName);

        if (user == null)
        {
            return NotFound("Le nom renseigné ne correspond à aucun utilisateur.");
        }

        try
        {
            var rewards = _tourGuideService.GetUserRewards(user);
            return Ok(rewards);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Une erreur interne est survenue.");
        }
    }

    [HttpGet("getTripDeals")]
    public ActionResult<List<Provider>> GetTripDeals([FromQuery] string userName)
    {
        var user = GetUser(userName);

        if (user == null)
        {
            return NotFound("Le nom renseigné ne correspond à aucun utilisateur.");
        }

        try
        {
            var deals = _tourGuideService.GetTripDeals(user);
            return Ok(deals);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Une erreur interne est survenue.");
        }
    }


    private User GetUser(string userName)
    {
        return _tourGuideService.GetUser(userName);
    }
}
