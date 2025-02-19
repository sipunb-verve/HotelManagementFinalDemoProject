﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using HotelManagementCoreMvcFrontend.Models;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;

namespace HotelManagementCoreMvcFrontend.Controllers
{
    public class HotelController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:7119/api/";
        public HotelController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        // GET: Hotels/Index - Fetch all hotels
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SetAuthorizationHeader(_httpClient);
            var response = await _httpClient.GetAsync($"{_baseUrl}Hotel/All");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var hotels = JsonConvert.DeserializeObject<List<Hotel>>(jsonData);
                return View(hotels);
            }
            return View(new List<Hotel>());
        }
      

     //Create hotel
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        //Create Hotel Post method
        [HttpPost]
        public async Task<IActionResult> Create(IFormFile? image ,Hotel hotel)

        {

            if (ModelState.IsValid)
            {
               
                if (image != null && image.Length > 0)

                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");


                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    hotel.HotelImage = "/images/" + uniqueFileName;
                }
                var userIdString = HttpContext.Session.GetString("UserId");

                if (Guid.TryParse(userIdString, out var userId))
                {
                    // Assign the logged-in user's ID to the HotelDto
                    hotel.CreatedBy = userId;
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(hotel), Encoding.UTF8, "application/json");
                    SetAuthorizationHeader(_httpClient);
                    var response = await _httpClient.PostAsync($"{_baseUrl}Hotel", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["HotelSuccess"] = "Hotel Created Succesfully";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError("", "Error occurred while creating the hotel.");
                }
                else
                {
                    ModelState.AddModelError("", "Hotel have not created .Try Again.");
                }
            }

            return View(hotel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            SetAuthorizationHeader(_httpClient);
            // Call API to get user by ID for editing
            var response = await _httpClient.GetAsync($"{_baseUrl}Hotel/{id}");
            if (response.IsSuccessStatusCode)
            {
                var hotel = await response.Content.ReadFromJsonAsync<Hotel>();
                ViewBag.SelectedCountryId = hotel.CountryId;
                ViewBag.SelectedStateId = hotel.StateId;
                ViewBag.SelectedCityId = hotel.CityId;
                return View(hotel);
            }

            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> Edit(IFormFile? image, Hotel hotel,string deleteImageFlag)
        {
            if (ModelState.IsValid)
            {
                SetAuthorizationHeader(_httpClient);
                var existingHotelResponse = await _httpClient.GetAsync($"{_baseUrl}Hotel/{hotel.Id}");
                var existingHotel = await existingHotelResponse.Content.ReadFromJsonAsync<Hotel>();
                // Handle Profile Image Upload
                if (image != null && image.Length > 0)

                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");


                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    hotel.HotelImage = "/images/" + uniqueFileName;
                }
                else if (deleteImageFlag == "true")
                {
                    hotel.HotelImage = null;
                }
                // Set UpdatedBy to the current user's ID (replace with your method of fetching the logged-in user)
                // Assuming Identity is used
                SetAuthorizationHeader(_httpClient);
                var content = new StringContent(JsonConvert.SerializeObject(hotel), Encoding.UTF8, "application/json");
              

                var response = await _httpClient.PutAsync($"{_baseUrl}Hotel/{hotel.Id}", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["EidtHotel"] = "Hotel Update Succesfully";
                    return RedirectToAction(nameof(Index));

                }

                ModelState.AddModelError("", "Something Else User Have Not Updated.");
            }

            return View(hotel);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            SetAuthorizationHeader(_httpClient);
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Hotel/{id}");
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", "Error deleting user.");
            return RedirectToAction(nameof(Delete), new {id});
        }
        //check hotel is associated with rooms
        public async Task<IActionResult> DeleteConformation(Guid Id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");
            var response = await _httpClient.GetAsync($"{_baseUrl}Hotel/Exist-Room/{Id}");
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Delete", new { id = Id });
            }
            var message = await response.Content.ReadAsStringAsync();
            TempData["Warning"] = message;
            if (role == "Admin")
            {
                return RedirectToAction("Index");
            }
            return RedirectToAction("GetRoomByHotelAssociatedWithManager", new { userId });

        }
        //Show All Hotel For Booking
        [HttpGet]
        public async Task<IActionResult> ShowAllHotel()
        {
            SetAuthorizationHeader(_httpClient);
            var response = await _httpClient.GetAsync($"{_baseUrl}Hotel/All");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var hotels = JsonConvert.DeserializeObject<List<Hotel>>(jsonData);
                return View(hotels);
            }
            return View(new List<Hotel>());
        }
        private void SetAuthorizationHeader(HttpClient httpClient)
         {
            var token = HttpContext.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

    }
}
