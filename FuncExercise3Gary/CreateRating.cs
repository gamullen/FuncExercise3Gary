using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FuncExercise3Gary
{
    public static class CreateRating
    {
        [FunctionName("CreateRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            bool result = false;
            string reason = "No reason found for failure";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string userId = data?.userId;
            string productId = data?.productId;
            string locationName = data?.locationName;
            string rating = data?.rating;
            string userNotes = data?.userNotes;

            if (null != userId)
            {
                string uri = "https://serverlessohuser.trafficmanager.net/api/GetUser?userId=" + userId;
                dynamic content = await MyHttpClientAsync(uri);

                string userName = content?.userName;
                string fullName = content?.fullName;
                if (null != userName && null != fullName)
                {
                    result = true;
                }
                else
                {
                    result = false;
                    reason = "No user found for ID " + userId;
                }
            }
            else
            {
                reason = "No user ID passed in";
            }

            if (null != productId && result)
            {
                string uri = "https://serverlessohproduct.trafficmanager.net/api/GetProduct?productId=" + productId;
                dynamic content = await MyHttpClientAsync(uri);

                string productName = content?.productName;
                string productDescription = content?.productDescription;
                if (null == productName || null == productDescription)
                {
                    result = false;
                    reason = "No product found for ID " + productId;
                }
            }

            if (null != rating && result)
            {
                bool isNumeric = int.TryParse(rating, out int ratingInt);
                if (isNumeric)
                {
                    if (0 >= ratingInt && 5 <= ratingInt)
                    {
                        result = false;
                        reason = "Invalid rating: " + rating;
                    }
                }
                else
                {
                    result = false;
                    reason = "Invalid rating: " + rating;
                }
            }

            //Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(data);
            data["id"] = Guid.NewGuid();
            data["timestamp"] = DateTime.UtcNow;
            //data = obj.ToString();

            ObjectResult returnValue;

            if (result)
            {
                returnValue = new OkObjectResult(data);
            }
            else
            {
                returnValue = new BadRequestObjectResult(reason);
            }

            return returnValue;
        }

        static internal async Task<Object> MyHttpClientAsync(string uri)
        {
            ObjectResult result = new BadRequestObjectResult("Unknown error occurred.");
            Object content = null;

            HttpClient client = new HttpClient();

            // Update port # in the following line.
            client.BaseAddress = new Uri(uri);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    string requestBody = await response.Content.ReadAsStringAsync();
                    content = JsonConvert.DeserializeObject(requestBody);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return content;
        }
    }
}
