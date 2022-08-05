//using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProductBase.Classes;
using ProductBase.Response;
using ProductBase.Response.Product;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace ProductSrv
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        public ProductResponse AddRating(ProductRequest request)
        {
            try
            {
                // Check if not valid request
                if (request.authenticateAddRating() != true)
                {
                    throw new Exception("Invalid parameters");
                }

                // Decalred to store updated products
                List<ProductDetails> updated_products = new List<ProductDetails>();

                // Declared valid products file path
                string filepath = System.AppDomain.CurrentDomain.BaseDirectory + "Data\\database.json";

                // Read file to fetch valid products
                string valid_products = File.ReadAllText(filepath);

                // Check if file is not empty
                if (valid_products != null && valid_products.Length > 0)
                {
                    // Store valid products into products instance
                    updated_products = JsonConvert.DeserializeObject<List<ProductDetails>>(valid_products);

                    // Check if requested product id exists in valid products list
                    ProductDetails existing_product = updated_products.Find(product => product.Id == request.id);

                    // Check if product already exist in file
                    if (existing_product != null)
                    {
                        foreach (ProductDetails beerDetails in updated_products)
                        {
                            // Skip if not request product
                            if (beerDetails.Id != existing_product.Id)
                            {
                                continue;
                            }
                            // Initialize User rating is not defined
                            if (beerDetails.UserRatings == null)
                            {
                                beerDetails.UserRatings = new List<UserRating>();
                            }

                            // Add user rating
                            beerDetails.UserRatings.Add(new UserRating
                            {
                                Username = request.username,
                                Rating = request.rating,
                                Comments = request.comments
                            });
                        }

                        // Write updated products into file
                        File.WriteAllText(filepath, JsonConvert.SerializeObject(updated_products));


                        // Send success response
                        return new ProductResponse()
                        {
                            Status = 200,
                            Message = "success",
                            Data = null
                        };
                    }
                }

                // Declared base url
                Uri baseUrl = new Uri(ConfigurationManager.AppSettings["PunkServiceUrl"]);

                // Instantiate web client
                RestClient client = new RestClient(baseUrl);

                // Prepare product endpoint by using product id
                RestRequest restRequest = new RestRequest(String.Format("beers/{0}", request.id), Method.Get);

                // Execute client request
                RestResponse restResponse = client.Execute(restRequest);

                // Check if request is successful
                if (restResponse.StatusCode == HttpStatusCode.OK)
                {
                    // Check if response is empty
                    if (restResponse == null || String.IsNullOrEmpty(restResponse.Content))
                    {
                        // Send error data response
                        return new ProductResponse()
                        {
                            Status = 400,
                            Message = "fail",
                            Error = "Invalid product id",
                            Data = null
                        };
                    }

                    // Parse Json data
                    JArray jArray = JArray.Parse(restResponse.Content);

                    // Convert parsed data into objects
                    List<ProductDetails> beers = JsonConvert.DeserializeObject<List<ProductDetails>>(jArray.ToString());

                    // Iterate requested product and add into updated file data
                    foreach (ProductDetails beerDetails in beers)
                    {
                        // Check if User rating is null
                        if (beerDetails.UserRatings == null)
                        {
                            beerDetails.UserRatings = new List<UserRating>();
                        }
                        // Add user rating
                        beerDetails.UserRatings.Add(new UserRating()
                        {
                            Username = request.username,
                            Rating = request.rating,
                            Comments = request.comments
                        });
                        
                        // Add requested product into updated product list
                        updated_products.Add(beerDetails);
                    }

                    // Write updated products into file
                    File.WriteAllText(filepath, JsonConvert.SerializeObject(updated_products));


                    // Send success response
                    return new ProductResponse()
                    {
                        Status = 200,
                        Message = "success",
                        Data = null
                    };
                }

                return new ProductResponse()
                {
                    Status = 400,
                    Message = "fail",
                    Error = "Invalid request"
                };
            }
            catch (HttpRequestException ex)
            {
                // Send empty response
                return new ProductResponse()
                {
                    Status = 400,
                    Message = "fail",
                    Error = ex.Message,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                // Send empty response
                return new ProductResponse()
                {
                    Status = 400,
                    Message = "fail",
                    Error = ex.Message,
                    Data = null
                };
            }
        }

        public ProductResponse GetBeers(ProductRequest request)
        {
            // Check if valid request
            if (request.authenticate() != true)
            {
                // Send success response back
                return new ProductResponse()
                {
                    Status = 400,
                    Message = "fail",
                    Error = "Invalid post",
                    Data = null
                };
            }

            try
            {
                // Declared base url
                Uri baseUrl = new Uri(ConfigurationManager.AppSettings["PunkServiceUrl"]);

                // Instantiate web client
                RestClient client = new RestClient(baseUrl);

                // Prepare request with endpoint
                RestRequest restRequest = new RestRequest("beers", Method.Get);

                // Add request parameter if product name filter is set
                if (!String.IsNullOrEmpty(request.name))
                {
                    restRequest.AddParameter("beer_name", request.name.Replace(" ", "_"));
                }

                // Execute client request
                RestResponse restResponse = client.Execute(restRequest);

                if (restResponse.StatusCode == HttpStatusCode.OK)
                {
                    // Check if response is not empty
                    if (restResponse != null && !String.IsNullOrEmpty(restResponse.Content))
                    {
                        // Declared valid products file path
                        string filepath = System.AppDomain.CurrentDomain.BaseDirectory + "Data\\database.json";

                        // Read file to fetch valid products
                        string valid_products = File.ReadAllText(filepath);

                        // Fetch products details from file if exists
                        List<ProductDetails> updated_products = (valid_products != null && valid_products.Length > 0) ? JsonConvert.DeserializeObject<List<ProductDetails>>(valid_products) : new List<ProductDetails>();
                        
                        // Parse requested products Json data
                        JArray jArray = JArray.Parse(restResponse.Content);

                        // Convert parsed data into objects
                        List<ProductDetails> beers = JsonConvert.DeserializeObject<List<ProductDetails>>(jArray.ToString());

                        // Iterate requested product and add into updated file data
                        foreach (ProductDetails beerDetails in beers)
                        {
                            // Skip product if already exists in valid products list to keep user ratings
                            if (updated_products.Count > 0 && updated_products.FindIndex(product => product.Id == beerDetails.Id) > 0)
                            {
                                continue;
                            }

                            // Add requested product into updated product list
                            updated_products.Add(beerDetails);
                        }

                        // Write updated products into file
                        File.WriteAllText(filepath, JsonConvert.SerializeObject(updated_products));

                        // Send success response back
                        return new ProductResponse()
                        {
                            Status = 200,
                            Message = "success",
                            Data = updated_products
                        };
                    }

                    // Send empty response
                    return new ProductResponse()
                    {
                        Status = 200,
                        Message = "success",
                        Data = null
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                // Send empty response
                return new ProductResponse()
                {
                    Status = 400,
                    Message = "fail",
                    Error = ex.Message,
                    Data = null
                };
            }
            catch (Exception ex)
            {
                // Send error response
                return new ProductResponse()
                {
                    Status = 400,
                    Message = "fail",
                    Error = ex.Message,
                    Data = null
                };
            }
            return new ProductResponse()
            {
                Status = 400,
                Message = "fail",
                Error = "Invalid request"
            };
        }
    }
}
