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
    public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        using var transaction = connection.BeginTransaction();

        var command = new CommandDefinition("""
            insert into movies (id, slug, title, yearofrelease)
            values (@Id, @Slug, @Title, @YearOfRelease)
            """, movie, cancellationToken: token);

        var result = await connection.ExecuteAsync(command);

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                command = new CommandDefinition("""
                insert into genres (movieId, name)
                values (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = genre }, cancellationToken: token);

                await connection.ExecuteAsync(command);
            }
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        var transaction = connection.BeginTransaction();

        var command = new CommandDefinition("""
            delete from genres where movieId = @id
            """, new { id }, cancellationToken: token);

        await connection.ExecuteAsync(command);

        command = new CommandDefinition("""
            delete from movies where id = @id
            """, new { id }, cancellationToken: token);

        var result = await connection.ExecuteAsync(command);

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

        var command = new CommandDefinition("""
            SELECT count(1) FROM movies where id = @id
            """, new { id }, cancellationToken: token);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        var command = new CommandDefinition("""
            select 
                m.*, 
                string_agg(distinct g.name, ',') as genres,
                round(avg(r.rating), 1) as rating,
                myr.rating as userrating
            from movies m 
            left join genres g on m.id = g.movieId
            left join ratings r on m.id = r.movieid
            left join ratings myr on (m.id = myr.movieid and myr.userid = @userId)
            group by id, myr.rating
            """, new { userId }, cancellationToken: token);

        var result = await connection.QueryAsync(command);

        return result.Select(x => new Movie
        {
            Id = x.id,
            Title = x.title,
            YearOfRelease = x.yearofrelease,
            Rating = (float?)x.rating,
            UserRating = (int?)x.userrating,
            Genres = Enumerable.ToList(x.genres.Split(','))
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        var command = new CommandDefinition("""
            select 
                m.*,
                round(avg(r.rating), 1) as rating,
                myr.rating as userrating
            from movies m
            left join ratings r on m.id = r.movieid
            left join ratings myr on (m.id = myr.movieid and myr.userid = @userId)
            where id = @id
            group by id, userrating
            """, new { id, userId }, cancellationToken: token);

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(command);

        if (movie is null)
        {
            return null;
        }

        command = new CommandDefinition("""
            select name from genres where movieId = @id
            """, new { id }, cancellationToken: token);

        var genres = await connection.QueryAsync<string>(command);

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug, Guid? userId = default, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        var command = new CommandDefinition("""
            select 
                m.*,
                round(avg(r.rating), 1) as rating,
                myr.rating as userrating
            from movies m
            left join ratings r on m.id = r.movieid
            left join ratings myr on (m.id = myr.movieid and myr.userid = @userId)
            where slug = @slug
            group by id, userrating
            """, new { slug, userId }, cancellationToken: token);

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(command);

        if (movie is null)
        {
            return null;
        }

        command = new CommandDefinition("""
            select name from genres where movieId = @id
            """, new { id = movie.Id }, cancellationToken: token);

        var genres = await connection.QueryAsync<string>(command);

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

        var transaction = connection.BeginTransaction();

        var command = new CommandDefinition("""
            delete from genres where movieId = @id
            """, new { id = movie.Id }, cancellationToken: token);

        await connection.ExecuteAsync(command);

        foreach (var genre in movie.Genres)
        {
            command = new CommandDefinition("""
                insert into genres (movieId, name)
                values (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = genre }, cancellationToken: token);

            await connection.ExecuteAsync(command);   
        }

        command = new CommandDefinition("""
            update movies set slug = @Slug, title = @Title, yearofrelease = @YearOfRelease
            where id = @Id
            """, movie, cancellationToken: token);

        var result = await connection.ExecuteAsync(command);

        transaction.Commit();
        return result > 0;
    }
}