// See https://aka.ms/new-console-template for more information
using Movies.Api.Sdk;
using Movies.Contracts.Requests;
using Refit;
using System.Text.Json;

var moviesApi = RestService.For<IMoviesApi>("https://localhost:5001");

var movie = await moviesApi.GetMovieAsync("mark-of-zorro-the-1940");

Console.WriteLine(JsonSerializer.Serialize(movie));

var request = new GetAllMoviesRequest
{
    Title = null,
    Year = null,
    SortBy = null,
    Page = 1,
    PageSize = 3,
};


var movies = await moviesApi.GetMoviesAsync(request);

Console.WriteLine(JsonSerializer.Serialize(movies));