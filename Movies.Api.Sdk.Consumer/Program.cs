// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Movies.Api.Sdk;
using Movies.Contracts.Requests;
using Refit;
using System.Text.Json;

//var moviesApi = RestService.For<IMoviesApi>("https://localhost:5001");

var services = new ServiceCollection();
services.AddRefitClient<IMoviesApi>()
    .ConfigureHttpClient(x =>
    {
        x.BaseAddress = new Uri("https://localhost:5001");
    })
    ;
var provider = services.BuildServiceProvider();

var moviesApi = provider.GetRequiredService<IMoviesApi>();  

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