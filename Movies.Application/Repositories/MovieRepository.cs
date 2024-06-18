using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;
using System.Net.Http.Headers;
using System.Security.AccessControl;

namespace Movies.Application.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public MovieRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }
    public async Task<bool> CreateAsync(Movie movie)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        var command = new CommandDefinition("""
            insert into movies (id, slug, title, yearofrelease)
            values (@Id, @Slug, @Title, @YearOfRelease)
            """, movie);

        var result = await connection.ExecuteAsync(command);

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                command = new CommandDefinition("""
                insert into genres (movieId, name)
                values (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = genre });

                await connection.ExecuteAsync(command);
            }
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var transaction = connection.BeginTransaction();

        var command = new CommandDefinition("""
            delete from genres where movieId = @id
            """, new { id });

        await connection.ExecuteAsync(command);

        command = new CommandDefinition("""
            delete from movies where id = @id
            """, new { id });

        var result = await connection.ExecuteAsync(command);

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        var command = new CommandDefinition("""
            SELECT count(1) FROM movies where id = @id
            """, new { id });

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var command = new CommandDefinition("""
            select m.*, string_agg(g.name, ',') as genres 
            from movies m left join genres g on m.id = g.movieId
            group by id
            """);

        var result = await connection.QueryAsync(command);

        return result.Select(x => new Movie
        {
            Id = x.id,
            Title = x.title,
            YearOfRelease = x.yearofrelease,
            Genres = Enumerable.ToList(x.genres.Split(','))
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var command = new CommandDefinition("""
            select * from movies where id = @id
            """, new { id });

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(command);

        if (movie is null)
        {
            return null;
        }

        command = new CommandDefinition("""
            select name from genres where movieId = @id
            """, new { id });

        var genres = await connection.QueryAsync<string>(command);

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var command = new CommandDefinition("""
            select * from movies where slug = @slug
            """, new { slug });

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(command);

        if (movie is null)
        {
            return null;
        }

        command = new CommandDefinition("""
            select name from genres where movieId = @id
            """, new { id = movie.Id });

        var genres = await connection.QueryAsync<string>(command);

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        var transaction = connection.BeginTransaction();

        var command = new CommandDefinition("""
            delete from genres where movieId = @id
            """, new { id = movie.Id });

        await connection.ExecuteAsync(command);

        foreach (var genre in movie.Genres)
        {
            command = new CommandDefinition("""
                insert into genres (movieId, name)
                values (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = genre });

            await connection.ExecuteAsync(command);   
        }

        command = new CommandDefinition("""
            update movies set slug = @Slug, title = @Title, yearofrelease = @YearOfRelease
            where id = @Id
            """, movie);

        var result = await connection.ExecuteAsync(command);

        transaction.Commit();
        return result > 0;
    }
}