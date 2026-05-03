using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Shared;

namespace LawyerApp.Shared.Abstractions
{
    public interface IJwtProvider
    {
        string Generate(User user); 
    }
}
