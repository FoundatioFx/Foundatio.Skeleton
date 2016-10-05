using System;
using System.Threading.Tasks;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Models
{
    public class TokenHelper
    {
        private readonly ITokenRepository _tokenRepository;
        public TokenHelper(ITokenRepository tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        public async Task<bool> ValidateToken(string tokenId)
        {
            var token = await _tokenRepository.GetByIdAsync(tokenId).AnyContext();
            return (token != null);
        }
    }
}
