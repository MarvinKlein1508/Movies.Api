﻿using FluentValidation;
using Movies.Application.Database;
using Movies.Application.Models;
using Movies.Application.Repositories;
using System.Net.Http.Headers;

namespace Movies.Application.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IValidator<Movie> _movieValidator;

    public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator)
    {
        _movieRepository = movieRepository;
        _movieValidator = movieValidator;
    }
    public async Task<bool> CreateAsync(Movie movie)
    {
        await _movieValidator.ValidateAndThrowAsync(movie);

        return await _movieRepository.CreateAsync(movie);
    }

    public Task<bool> DeleteByIdAsync(Guid id)
    {
        return _movieRepository.DeleteByIdAsync(id);
    }

    public Task<bool> ExistsByIdAsync(Guid id)
    {
        return _movieRepository.ExistsByIdAsync(id);
    }

    public Task<IEnumerable<Movie>> GetAllAsync()
    {
        return _movieRepository.GetAllAsync();
    }

    public Task<Movie?> GetByIdAsync(Guid id)
    {
        return _movieRepository.GetByIdAsync(id);
    }

    public Task<Movie?> GetBySlugAsync(string slug)
    {
        return _movieRepository.GetBySlugAsync(slug);
    }

    public async Task<Movie?> UpdateAsync(Movie movie)
    {
        await _movieValidator.ValidateAndThrowAsync(movie);

        var movieExists = await _movieRepository.ExistsByIdAsync(movie.Id);

        if(!movieExists)
        {
            return null;
        }

        await _movieRepository.UpdateAsync(movie);
        
        return movie;
    }
}
