using PatientTracker.Domain.Entities;

namespace PatientTracker.Application.DTOs.Mapping;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.FullName : null));

        CreateMap<Profile, ProfileDto>();
    }
}
