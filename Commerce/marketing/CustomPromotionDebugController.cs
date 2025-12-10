using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Marketing;
using EPiServer.Core;
using Mediachase.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.Custom.Commerce.Marketing
{
    [ApiController]
    [Route("util-api/custom-promotion-debug")]
    public class CustomPromotionDebugController : ControllerBase
    {
        private readonly ICurrentMarket _currentMarket;
        private readonly CampaignInfoExtractor _campaignInfoExtractor;
        private readonly IContentLoader _contentLoader;

        public CustomPromotionDebugController(
            ICurrentMarket currentMarket,
            CampaignInfoExtractor campaignInfoExtractor,
            IContentLoader contentLoader)
        {
            _currentMarket = currentMarket;
            _campaignInfoExtractor = campaignInfoExtractor;
            _contentLoader = contentLoader;
        }

        /// <summary>
        /// Sample: https://localhost:5000/util-api/custom-promotion-debug/active-campaigns-no-site
        /// Returns campaigns considered active when no site id is supplied (known to include expired ones).
        /// </summary>
        [HttpGet("active-campaigns-no-site")]
        public IActionResult GetActiveCampaignsWithoutSite()
        {
            try
            {
                var market = _currentMarket.GetCurrentMarket();
                var campaigns = GetActiveCampaignsBuggy(market, null)
                    .Select(c => new
                    {
                        c.ContentLink.ID,
                        c.Name,
                        c.IsActive,
                        Status = _campaignInfoExtractor.GetStatusFromDates(c.ValidFrom, c.ValidUntil).ToString(),
                        c.ValidFrom,
                        c.ValidUntil
                    })
                    .ToList();

                return Ok(new
                {
                    Market = market?.MarketId.Value,
                    Count = campaigns.Count,
                    Campaigns = campaigns
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Sample: https://localhost:5000/util-api/custom-promotion-debug/active-campaigns-by-site?siteId=FND
        /// Returns campaigns considered active when a site id is supplied (expected to filter expired ones).
        /// </summary>
        [HttpGet("active-campaigns-by-site")]
        public IActionResult GetActiveCampaignsBySite([FromQuery] string siteId)
        {
            try
            {
                var market = _currentMarket.GetCurrentMarket();
                var campaigns = GetActiveCampaignsBuggy(market, siteId)
                    .Select(c => new
                    {
                        c.ContentLink.ID,
                        c.Name,
                        c.IsActive,
                        Status = _campaignInfoExtractor.GetStatusFromDates(c.ValidFrom, c.ValidUntil).ToString(),
                        c.ValidFrom,
                        c.ValidUntil
                    })
                    .ToList();

                return Ok(new
                {
                    Market = market?.MarketId.Value,
                    SiteId = siteId,
                    Count = campaigns.Count,
                    Campaigns = campaigns
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Sample: https://localhost:5000/util-api/custom-promotion-debug/promotion-counts?siteId=FND
        /// Compares promotion counts per campaign using the site-aware active campaign list.
        /// </summary>
        [HttpGet("promotion-counts")]
        public IActionResult GetPromotionCounts([FromQuery] string siteId)
        {
            try
            {
                var market = _currentMarket.GetCurrentMarket();
                var campaigns = GetActiveCampaignsBuggy(market, siteId);

                var payload = campaigns
                    .Select(c => new
                    {
                        c.ContentLink.ID,
                        c.Name,
                        c.IsActive,
                        Status = _campaignInfoExtractor.GetStatusFromDates(c.ValidFrom, c.ValidUntil).ToString(),
                        c.ValidFrom,
                        c.ValidUntil,
                        PromotionCount = _contentLoader.GetChildren<PromotionData>(c.ContentLink).Count()
                    })
                    .OrderByDescending(x => x.PromotionCount)
                    .ToList();

                return Ok(new
                {
                    Market = market?.MarketId.Value,
                    SiteId = siteId,
                    CampaignCount = payload.Count,
                    TotalPromotions = payload.Sum(x => x.PromotionCount),
                    Campaigns = payload
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        // Replicates the current buggy behavior in PromotionEngineContentLoader.GetActiveCampaigns(IMarket, string)
        // where missing/empty siteId lets expired campaigns pass through.
        private IEnumerable<SalesCampaign> GetActiveCampaignsBuggy(IMarket market, string siteId)
        {
            var campaigns = _contentLoader.GetChildren<SalesCampaign>(SalesCampaignFolder.CampaignRoot);

            if (market != null)
            {
                campaigns = campaigns.Where(c => IsValidMarket(c, market));
            }

            return campaigns.Where(c => IsApplicableCampaignBuggy(c, siteId));
        }

        private bool IsValidMarket(SalesCampaign campaign, IMarket market)
        {
            if (market == null || !market.IsEnabled)
            {
                return market == null;
            }

            return campaign.TargetMarkets?.Contains(market.MarketId.Value) ?? false;
        }

        private bool IsApplicableCampaignBuggy(SalesCampaign campaign, string siteId)
        {
            if (campaign.Sites == null || campaign.Sites.Count == 0 || string.IsNullOrEmpty(siteId))
            {
                return true;
            }

            return campaign.Sites.Any(x => x.Contains(siteId, StringComparison.InvariantCultureIgnoreCase))
                && _campaignInfoExtractor.IsCampaignActive(campaign);
        }
    }
}

