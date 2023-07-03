using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

var app = WebApplication.Create(args);
app.UseStaticFiles();

// Create a list to hold all image data
var allImageData = new List<Dictionary<string, string>>();

app.MapGet("/", async (HttpContext context) =>
{
    // Inline HTML content
    var html = @"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Image Uploader</title>
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <style>
                body {
                    font-family: Arial, sans-serif;
                    margin: 0;
                    padding: 20px;
                    background-color: #f4f4f4;
                    align-items: center;               

                    
                }
                h1 {
                    color: #333;
                    text-align: center;
                }
                form {
                    background-color: #fff;
                    padding: 20px;
                    border-radius: 4px;
                    box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                    height: 50vh;
                    width: 50vw;
                    display: flex;
                    flex-direction: column;
                    justify-content: center;
                    align-items: center;
                    margin: auto;            
                }
                label {
                    display: block;
                    margin-bottom: 10px;
                    font-weight: bold;
                }
                input[type=""text""]{ 
                    width: calc(50% - 10px);
                    padding: 10px;
                    border-radius: 4px;
                    border: 0.5px solid #c2d6d6;
                    margin-right: 10px;
                }
                input[type=""file""] {
                    width: calc(50% - 10px);
                    padding: 10px;
                    border-radius: 4px;
                    border: 0.5px solid #c2d6d6;
                    margin-right: 10px;
                    
                }
                input[type=""file""]:not(:disabled):not([readonly]) {
                    cursor: pointer;
                }
                input[type=""file""]::file-selector-button {
                    overflow: hidden;
                    font-weight: bold;
                    background-color: #f0f5f5;
                    padding: 10px;
                    border: 0.5px solid #c2d6d6;
                    border-radius: 1px;
                }
                input[type=""submit""] {
                    display: inline-block;
                    padding: 15px 25px;
                    font-size: 24px;
                    cursor: pointer;
                    text-align: center;
                    text-decoration: none;
                    outline: none;
                    color: #fff;
                    background-color: #4CAF50;
                    border: none;
                    border-radius: 15px;
                    box-shadow: 0 9px #C0C0C0;
                    margin-top: 20px;

                }
                input[type=""submit""]:hover {
                    background-color: #45a049;
                }
                input[type=""submit""]:active {
                    background-color: #3e8e41;
                    box-shadow: 0 5px #666;
                    transform: translateY(2px);
                }
                .error-message {
                    color: red;
                    margin-top: 10px;
                }
            </style>
        
        </head>
        <body>
            <h1>Image Uploader</h1>
            <form id=""uploadForm"" enctype=""multipart/form-data"">
                <label for=""title"">Enter title</label>
                <input type=""text"" name=""title"" id=""title"" required placeholder=""Enter title""><br>
                <label for=""image"">Browse image</label>
                <input type=""file"" name=""image"" id=""image"" accept="".jpeg, .jpg,.png, .gif"" required placeholder=""No file chosen""><br>
                <input type=""submit"" value=""Upload"">
            </form>

            <script>
                document.getElementById(""uploadForm"").addEventListener(""submit"", function (event) {
                    event.preventDefault(); // Prevent form submission

                    var titleInput = document.getElementById(""title"");
                    var imageInput = document.getElementById(""image"");

                    if (titleInput.value.trim() === """") {
                        alert(""Please enter a title."");
                        titleInput.focus();
                        return;
                    }

                    if (imageInput.files.length === 0) {
                        alert(""Please select an image."");
                        imageInput.focus();
                        return;
                    }           
           
                    var Extensions = ['jpeg', 'jpg' ,'png', 'gif'];
                    var fileExtension = document.getElementById('image').value.split('.').pop().toLowerCase();
                    if (!Extensions.includes(fileExtension)) {
                        alert(""Invalid file extension. Only JPEG, PNG, and GIF files are allowed."");
                        imageInput.focus();
                        return;
                    } 

                    var form = event.target;
                    var formData = new FormData(form);

                    // Send the form data to the server using AJAX
                    var xhr = new XMLHttpRequest();
                    xhr.open(""POST"", ""/upload"");
                    xhr.onload = function () {
                        if (xhr.status === 200) {
                            var imageUrl = xhr.responseURL;
                            // Redirect to the image details page
                            window.location.href = imageUrl;
                        } else {
                            alert(""Error uploading image: "" + xhr.statusText);
                        }
                    };
                    xhr.send(formData);
                });
            </script>
        </body>
        </html>
    ";
    await context.Response.WriteAsync(html);
});

app.MapPost("/upload", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var title = form["title"];
    var file = form.Files.GetFile("image");

    // Validate form inputs and file type
    if (string.IsNullOrEmpty(title) || file == null)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Please provide a title and select an image file.");
        return;
    }

    var allowedExtensions = new[] { ".jpeg", ".jpg", ".png", ".gif" };
    var fileExtension = Path.GetExtension(file.FileName);

    if (!allowedExtensions.Contains(fileExtension.ToLowerInvariant()))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Only JPEG, PNG, and GIF files are allowed.");
        return;
    }

    var uniqueId = Guid.NewGuid().ToString();
    var fileName = $"{uniqueId}{fileExtension}";

    // Save the uploaded image to the server
    var filePath = Path.Combine("wwwroot/Images", fileName);
    await using var stream = File.Create(filePath);
    await file.CopyToAsync(stream);

    // Create a dictionary to store the image details
    var newImage = new Dictionary<string, string>
    {
        { "Id", uniqueId },
        { "Title", title },
        { "ImagePath", filePath },
    };

    // Add the image data to the list
    allImageData.Add(newImage);

    // Serialize all image data to JSON and save it to a file
    var allImageDataJson = JsonSerializer.Serialize(allImageData);
    var allImageDataFilePath = Path.Combine("wwwroot/Images", "all_images.json");
    await File.WriteAllTextAsync(allImageDataFilePath, allImageDataJson);

    var imageUrl = $"/picture/{uniqueId}";

    // Redirect to the image details page with the unique ID
    context.Response.Redirect(imageUrl);
});

app.MapGet("/picture/{id}", async (HttpContext context) =>
{
    var id = context.Request.RouteValues["id"]?.ToString();
    var allImageDataFilePath = Path.Combine("wwwroot/Images", "all_images.json");

    if (File.Exists(allImageDataFilePath))
    {
        // Read all image data from the JSON file
        var allImageDataJson = await File.ReadAllTextAsync(allImageDataFilePath);
        var allImageData = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(allImageDataJson);

        // Find the specific image data by ID
        var imageData = allImageData.FirstOrDefault(img => img["Id"] == id);
        if (imageData != null)
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(imageData["ImagePath"]);
            string imageBase64Data = Convert.ToBase64String(imageBytes);

            // Create HTML response
            var html = $@"<!DOCTYPE html>
            <html>
                <head>
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Image Uploader</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        margin: 0;
                        padding: 20px;
                        background-color: #f4f4f4;
                    }}
                    .card h5{{
                        text-align: center;
                        font-size: 30px;
                    }}
                    .container {{
                        text-align: center;
                        margin-top: 20px;
                    }}

                    .container img {{
                        width: 60vw; /* Set the desired width */
                        height: auto; /* Automatically adjust height */
                    }}

                    .card {{
                        text-align: center;
                        margin-top: 20px;
                    }}

                    .card button {{
                        padding: 10px 20px;
                        font-size: 16px;
                        cursor: pointer;
                        text-align: center;
                        color: #fff;
                        background-color: #4CAF50;
                        border: none;
                        border-radius: 15px;
                        box-shadow: 0 9px #C0C0C0;
                        margin-bottom: 30px;

                    }}
                </style>
               
                </head>
                <body>
                    <div class=""container"">
                        <img src=""data:image/png;base64,{imageBase64Data}"" alt=""{imageData["Title"]}"">
                    </div>

                    <div class=""card"">
                        <h5 class=""card-title"" style=""text-align: center"">{imageData["Title"]}</h5>
                        <button onclick='redirectBack()'>Upload Again!</button>
                    </div>

                    <script>
                        function redirectBack() {{
                            window.location.href = '/';
                        }}
                </script
            </body>
            </html>";

          return Results.Content(html, "text/html", System.Text.Encoding.UTF8);
        }
        else
        {
            await context.Response.WriteAsync("Image not found.");
        }
    }

    // Return a default response when the file doesn't exist or other errors occur
    return Results.NotFound("No stored images");
});

app.Run();