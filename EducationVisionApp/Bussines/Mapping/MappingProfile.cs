using AutoMapper;
using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Bussines.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserCreateUpdateDto, User>();
        CreateMap<User, UserListDto>();
    }
}