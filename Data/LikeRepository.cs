using System;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class LikeRepository(DataContext context, IMapper mapper) : ILikeRepository
{
    public void AddLike(UserLike like)
    {
        context.Likes.Add(like);
    }

    public void DeleteLike(UserLike like)
    {
        context.Likes.Remove(like);
    }

    // Get the users that are liked by the current user
    public async Task<IEnumerable<int>> GetCurrentUserLikeIds(int currentUserId)
    {
        return await context.Likes
            .Where(like => like.SourceUserId == currentUserId)  
            .Select(like => like.TargetUserId)
            .ToListAsync();
    }

    // Get a like of the current user
    public async Task<UserLike?> GetUserLike(int sourceUserId, int targetUserId)
    {
        return await context.Likes.FindAsync(sourceUserId, targetUserId);
    }

    public async Task<PagedList<MemberDto>> GetUserLikes(LikeParams likeParams)
    {
        var likes = context.Likes.AsQueryable();
        IQueryable<MemberDto> query;

        switch(likeParams.Predicate) 
        {
            case "liked":
                query = likes 
                    .Where(x => x.SourceUserId == likeParams.UserId)
                    .Select(x => x.TargetUser)    
                    .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                break;
            case "likedBy":
                query = likes 
                        .Where(x => x.TargetUserId == likeParams.UserId)
                    .Select(x => x.SourceUser)    
                    .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                break;
            default:
                var likeIds = await GetCurrentUserLikeIds(likeParams.UserId);

                query = likes
                    .Where(x => x.TargetUserId == likeParams.UserId && likeIds.Contains(x.SourceUserId))
                    .Select(x => x.SourceUser)
                    .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                break;    
        }
        return await PagedList<MemberDto>.CreateAsync(query, likeParams.PageNumber, likeParams.PageSize);
    }

    public async Task<bool> SaveChanges()
    {
        return await context.SaveChangesAsync() > 0;
    }
}
