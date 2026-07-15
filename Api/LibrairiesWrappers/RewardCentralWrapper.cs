using TourGuide.LibrairiesWrappers.Interfaces;

namespace TourGuide.LibrairiesWrappers
{
    public class RewardCentralWrapper : IRewardCentral
    {
        private readonly RewardCentral.RewardCentral _rewardCentral;

        public RewardCentralWrapper()
        {
            _rewardCentral = new ();
        }

        public async Task<int> GetAttractionRewardPointsAsync(Guid attractionId, Guid userId)
        {
            return await Task.Run(() => _rewardCentral.GetAttractionRewardPoints(attractionId, userId));
        }
    }
}
