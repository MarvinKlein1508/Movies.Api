using Dapper;
using Movies.Application.Database;

namespace Movies.Application.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public RatingRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

        var command = new CommandDefinition("""
            delete from ratings
            where movieid = @movieId
            and userId = @userId
            """, new { movieId, userId }, cancellationToken: token);

        var result = await connection.ExecuteAsync(command);

        return result > 0;
    }

    public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

        var command = new CommandDefinition("""
            select round(avg(r.rating), 1) from ratings r
            where movieid = @movieId
            """, new { movieId }, cancellationToken: token);

        return await connection.QuerySingleOrDefaultAsync<float?>(command);
    }

    public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

        var command = new CommandDefinition("""
            select 
                round(avg(rating), 1),
                (select rating 
                 from ratings
                 where movieid = @movieId
                   and userid = @userId
                limit 1)
            from ratings
            where movieid = @movieId
            """, new { movieId, userId }, cancellationToken: token);

        return await connection.QuerySingleOrDefaultAsync<(float?, int?)>(command);
    }

    public async Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

        var command = new CommandDefinition("""
            insert into ratings(userid, movieid, rating)
            values (@userId, @movieId, @rating)
            on conflict (userid, movieid) do update
                set rating = @rating
            """, new { userId, movieId, rating }, cancellationToken: token);

        var result = await connection.ExecuteAsync(command);

        return result > 0;
    }
}
