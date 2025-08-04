using AutoMapper;
using EducationVisionApp.Application.DTOs.Class;
using EducationVisionApp.Application.DTOs.Lesson;
using EducationVisionApp.Application.DTOs.Student;
using EducationVisionApp.Domain.Entities;

namespace EducationVisionApp.Bussines.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserCreateUpdateDto, User>();
        CreateMap<User, UserListDto>();

        CreateMap<ClassCreateUpdateDto, Class>();
        CreateMap<Class, ClassListDto>();
        CreateMap<Lesson, LessonListDto>();

        CreateMap<Lesson, CreateLessonDto>().ReverseMap();

    }
}