﻿using Microsoft.AspNetCore.Mvc;
using static api.Handler;
using Newtonsoft.Json;
using WebApplication1.Models;
using System.Linq;
using static WebApplication1.Models.ResturantsModel;

namespace WebApplication1.Controllers
{
    public class ApiController : Controller
    {
        // 
        // GET: /resturants/search
        [AllowCrossSiteJson]
        [Route("/resturants/search")]
        public async Task<IActionResult> GetResturants(
            string sort, bool onlyOpenNow,
            int maxPrice = 5, int minPrice = 0,
            string search = "resturant", string radius = "2000", 
            string lat = "57.78029486070066", string lon = "14.178692680912373"
        )
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            if (
                string.IsNullOrEmpty(search) == false && string.IsNullOrEmpty(radius) == false && 
                (sort == null || (sort == "rating" || sort == "alphabetical" || sort == "expensive" || sort == "cheap" || sort == "distance" || sort == "opennow" || sort == "closednow")) && 
                minPrice >= 0 && minPrice <= 5 && maxPrice >= 0 && maxPrice <= 5 && minPrice <= maxPrice
            )
            {
                string baseUrl = "https://maps.googleapis.com/maps/api/place/nearbysearch/json";
                string urlParams = $"?key={config["GOOGLE_API_KEY"]}&keyword={search}&location={lat}%2C{lon}&type=resturant&maxprice={maxPrice}&minprice={minPrice}";

                if (sort == "distance")
                {
                    urlParams += "&rankby=distance";
                } else
                {
                    urlParams += $"&radius={radius}";
                }

                Console.WriteLine(onlyOpenNow);

                if (onlyOpenNow == true)
                {
                    urlParams += "&opennow";
                }

                string? apiResp = await APICallAsync(baseUrl, urlParams, "application/json");

                if (apiResp != null)
                {
                    UnsortedResults? apiObj = JsonConvert.DeserializeObject<UnsortedResults>(apiResp);

                    if (apiObj != null && (sort != null && sort != "distance") && apiObj.status != null)
                    {
                        IOrderedEnumerable<PlaceObj> sortedResturants = null;

                        switch (sort)
                        {
                            case "alphabetical":
                                sortedResturants = apiObj.results.OrderBy(el => el.name);
                                break;
                            case "rating":
                                sortedResturants = apiObj.results.OrderByDescending(el => el.rating);
                                break;
                            case "expensive":
                                sortedResturants = apiObj.results.OrderByDescending(el => el.price_level);
                                break;
                            case "cheap":
                                sortedResturants = apiObj.results.OrderBy(el => el.price_level);
                                break;
                            case "opennow":
                                sortedResturants = apiObj.results.OrderByDescending(el => el.opening_hours.open_now);
                                break;
                            case "closednow":
                                sortedResturants = apiObj.results.OrderBy(el => el.opening_hours.open_now);
                                break;
                        }

                        if (sortedResturants != null)
                        {
                            return Ok(new SortedResults
                            {
                                results = sortedResturants,
                                status = apiObj.status
                            });
                        }
                        else
                        {
                            return NoContent();
                        }
                    }

                    return Ok(apiObj);
                }
                else
                {
                    return NoContent();
                }
            }

            return BadRequest();
        }
    }
}
