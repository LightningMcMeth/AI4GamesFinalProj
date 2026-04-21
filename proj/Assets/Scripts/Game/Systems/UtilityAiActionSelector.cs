using System.Collections.Generic;
using System.Linq;

namespace AI4GamesFinalProj.Gameplay
{
    public sealed class UtilityAiActionSelector
    {
        private readonly int offerCount;

        public UtilityAiActionSelector(int offerCount = 3)
        {
            this.offerCount = offerCount;
        }

        public IReadOnlyList<PlayerActionOffer> BuildTopOffers(Attempt attempt)
        {
            List<PlayerActionOffer> offers = new List<PlayerActionOffer>();
            foreach (PlayerAction action in attempt.AvailableActions)
            {
                PlayerActionContext context = attempt.CreateActionContext(new PlayerActionRequest(action.Id));
                if (!action.CanExecute(context))
                {
                    continue;
                }

                float score = action.Score(context);
                if (score <= 0f)
                {
                    continue;
                }

                offers.Add(new PlayerActionOffer(action, score));
            }

            return offers
                .OrderByDescending(offer => offer.UtilityScore)
                .ThenBy(offer => offer.Action.DisplayName)
                .Take(offerCount)
                .ToList();
        }
    }
}
